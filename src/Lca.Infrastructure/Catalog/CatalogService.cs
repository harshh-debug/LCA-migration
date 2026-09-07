using Lca.Core.Catalog;
using Lca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Catalog;

public sealed class CatalogService(LcaDbContext context, TimeProvider timeProvider) : ICatalogService
{
    public async Task<PagedResult<Product>> SearchProductsAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = context.Products.AsNoTracking()
            .Include(value => value.Category).Include(value => value.Pricing)
            .Include(value => value.Inventory).Include(value => value.Media);
        query = search.Status switch
        {
            ProductStatusFilter.Active => query.Where(value => !value.IsDisabled),
            ProductStatusFilter.Inactive => query.Where(value => value.IsDisabled),
            _ => query,
        };
        if (search.CategoryId.HasValue) query = query.Where(value => value.CategoryId == search.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            string pattern = $"%{EscapeLikePattern(search.Search.Trim())}%";
            query = query.Where(value => EF.Functions.Like(value.ItemCode, pattern, "\\")
                || EF.Functions.Like(value.Name, pattern, "\\")
                || (value.Group1 != null && EF.Functions.Like(value.Group1, pattern, "\\"))
                || (value.Group2 != null && EF.Functions.Like(value.Group2, pattern, "\\")));
        }
        int totalCount = await query.CountAsync(cancellationToken);
        Product[] items = await query.OrderBy(value => value.ItemCode)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).ToArrayAsync(cancellationToken);
        return new(items, search.Page, search.PageSize, totalCount);
    }

    public Task<Product?> GetProductAsync(long id, CancellationToken cancellationToken) => context.Products.AsNoTracking()
        .Include(value => value.Category).Include(value => value.Pricing)
        .Include(value => value.Inventory).Include(value => value.Media)
        .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

    public async Task<Product> CreateProductAsync(ProductWriteModel model, CancellationToken cancellationToken)
    {
        await ValidateCategoryAsync(model.CategoryId, null, cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Product product = new() { ItemCode = model.ItemCode.Trim(), Name = model.Name.Trim(), CreatedAtUtc = now, UpdatedAtUtc = now };
        Apply(product, model);
        context.Products.Add(product);
        await SaveCatalogChangesAsync("A product with this ItemCode already exists.", cancellationToken);
        return (await GetProductAsync(product.Id, cancellationToken))!;
    }

    public async Task<Product?> UpdateProductAsync(long id, ProductWriteModel model, CancellationToken cancellationToken)
    {
        Product? product = await context.Products.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return null;
        await ValidateCategoryAsync(model.CategoryId, null, cancellationToken);
        Apply(product, model);
        product.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await SaveCatalogChangesAsync("A product with this ItemCode already exists.", cancellationToken);
        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<ProductPricing?> UpdatePricingAsync(long id, PricingWriteModel model, CancellationToken cancellationToken)
    {
        Product? product = await context.Products.Include(value => value.Pricing).SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return null;
        ProductPricing pricing = product.Pricing ?? new ProductPricing { ProductId = product.Id };
        pricing.PurchaseRate = model.PurchaseRate; pricing.DealerRate = model.DealerRate;
        pricing.WholesaleRate = model.WholesaleRate; pricing.RetailRate = model.RetailRate; pricing.OtherRate = model.OtherRate;
        pricing.VatRate = model.VatRate; pricing.AdditionalVatRate = model.AdditionalVatRate; pricing.CstRate = model.CstRate;
        pricing.IgstRate = model.IgstRate; pricing.SgstRate = model.SgstRate; pricing.CgstRate = model.CgstRate;
        pricing.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (product.Pricing is null) context.ProductPricing.Add(pricing);
        await context.SaveChangesAsync(cancellationToken);
        return pricing;
    }

    public async Task<ProductInventory?> UpdateInventoryAsync(long id, InventoryWriteModel model, CancellationToken cancellationToken)
    {
        Product? product = await context.Products.Include(value => value.Inventory).SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return null;
        ProductInventory inventory = product.Inventory ?? new ProductInventory { ProductId = product.Id };
        inventory.Balance = model.Balance; inventory.CurrentStock = model.CurrentStock;
        inventory.MaximumStock = model.MaximumStock; inventory.MinimumStock = model.MinimumStock;
        inventory.Godown1Stock = model.Godown1Stock; inventory.Godown2Stock = model.Godown2Stock;
        inventory.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (product.Inventory is null) context.ProductInventory.Add(inventory);
        await context.SaveChangesAsync(cancellationToken);
        return inventory;
    }

    public async Task<bool> DeleteProductAsync(long id, CancellationToken cancellationToken)
    {
        Product? product = await context.Products.Include(value => value.Pricing).Include(value => value.Inventory)
            .Include(value => value.Media).SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (product is null) return false;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Products.Remove(product);
        await SaveCatalogChangesAsync("This product is referenced by protected business history and cannot be deleted.", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await context.Categories.AsNoTracking().OrderBy(value => value.Name).ToArrayAsync(cancellationToken);

    public async Task<Category> CreateCategoryAsync(string name, long? parentCategoryId, bool displaySubCategory, CancellationToken cancellationToken)
    {
        await ValidateCategoryAsync(parentCategoryId, null, cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Category category = new() { Name = name.Trim(), ParentCategoryId = parentCategoryId, DisplaySubCategory = displaySubCategory, CreatedAtUtc = now, UpdatedAtUtc = now };
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Category?> UpdateCategoryAsync(long id, string name, long? parentCategoryId, bool displaySubCategory, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (category is null) return null;
        await ValidateCategoryAsync(parentCategoryId, id, cancellationToken);
        category.Name = name.Trim(); category.ParentCategoryId = parentCategoryId; category.DisplaySubCategory = displaySubCategory;
        category.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(long id, CancellationToken cancellationToken)
    {
        Category? category = await context.Categories.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (category is null) return false;
        bool isReferenced = await context.Products.AnyAsync(value => value.CategoryId == id, cancellationToken)
            || await context.Categories.AnyAsync(value => value.ParentCategoryId == id, cancellationToken);
        if (isReferenced)
            throw new CatalogConflictException("This category is used by products or child categories and cannot be deleted.");
        context.Categories.Remove(category);
        await SaveCatalogChangesAsync("This category is used by products or child categories and cannot be deleted.", cancellationToken);
        return true;
    }

    private async Task ValidateCategoryAsync(long? categoryId, long? editedCategoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue) return;
        if (categoryId == editedCategoryId) throw new CatalogConflictException("A category cannot be its own parent.");
        if (!await context.Categories.AnyAsync(value => value.Id == categoryId, cancellationToken))
            throw new CatalogConflictException("The selected category does not exist for this tenant.");
        if (!editedCategoryId.HasValue) return;
        long? cursor = categoryId;
        while (cursor.HasValue)
        {
            if (cursor == editedCategoryId) throw new CatalogConflictException("The parent selection would create a category cycle.");
            cursor = await context.Categories.Where(value => value.Id == cursor).Select(value => value.ParentCategoryId).SingleOrDefaultAsync(cancellationToken);
        }
    }

    private async Task SaveCatalogChangesAsync(string conflictMessage, CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) { throw new CatalogConflictException(conflictMessage, exception); }
    }

    private static void Apply(Product product, ProductWriteModel model)
    {
        product.ItemCode = model.ItemCode.Trim(); product.Name = model.Name.Trim(); product.Unit = Clean(model.Unit);
        product.AlternateItemCode = Clean(model.AlternateItemCode); product.GujaratiName = Clean(model.GujaratiName); product.UnitKilograms = model.UnitKilograms;
        product.Group1 = Clean(model.Group1); product.Group2 = Clean(model.Group2); product.ChapterNumber = Clean(model.ChapterNumber);
        product.HsnNumber = Clean(model.HsnNumber); product.ItemType = Clean(model.ItemType); product.Packing = Clean(model.Packing);
        product.ManufacturerName = Clean(model.ManufacturerName); product.Location = Clean(model.Location); product.AlternateLocation = Clean(model.AlternateLocation);
        product.WarrantyYears = model.WarrantyYears; product.WarrantyMonths = model.WarrantyMonths; product.Description = Clean(model.Description);
        product.Remark = Clean(model.Remark); product.SalesmanCommission = model.SalesmanCommission; product.CategoryId = model.CategoryId;
        product.IsDisabled = model.IsDisabled;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal).Replace("[", "\\[", StringComparison.Ordinal);
}
