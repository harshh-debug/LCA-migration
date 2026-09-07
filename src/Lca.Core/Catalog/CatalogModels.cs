namespace Lca.Core.Catalog;

public enum ProductStatusFilter { Active, Inactive, All }

public sealed record ProductSearch(string? Search, long? CategoryId, ProductStatusFilter Status, int Page, int PageSize);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ProductWriteModel(
    string ItemCode, string Name, string? Unit, string? AlternateItemCode, string? GujaratiName,
    decimal? UnitKilograms, string? Group1, string? Group2, string? ChapterNumber, string? HsnNumber,
    string? ItemType, string? Packing, string? ManufacturerName, string? Location, string? AlternateLocation,
    int? WarrantyYears, int? WarrantyMonths, string? Description, string? Remark,
    decimal? SalesmanCommission, long? CategoryId, bool IsDisabled);

public sealed record PricingWriteModel(
    decimal PurchaseRate, decimal DealerRate, decimal WholesaleRate, decimal RetailRate, decimal OtherRate,
    decimal? VatRate, decimal? AdditionalVatRate, decimal? CstRate,
    decimal IgstRate, decimal SgstRate, decimal CgstRate);

public sealed record InventoryWriteModel(
    decimal? Balance, decimal? CurrentStock, decimal? MaximumStock, decimal? MinimumStock,
    decimal? Godown1Stock, decimal? Godown2Stock);

public interface ICatalogService
{
    Task<PagedResult<Product>> SearchProductsAsync(ProductSearch search, CancellationToken cancellationToken);
    Task<Product?> GetProductAsync(long id, CancellationToken cancellationToken);
    Task<Product> CreateProductAsync(ProductWriteModel model, CancellationToken cancellationToken);
    Task<Product?> UpdateProductAsync(long id, ProductWriteModel model, CancellationToken cancellationToken);
    Task<ProductPricing?> UpdatePricingAsync(long id, PricingWriteModel model, CancellationToken cancellationToken);
    Task<ProductInventory?> UpdateInventoryAsync(long id, InventoryWriteModel model, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<Category> CreateCategoryAsync(string name, long? parentCategoryId, bool displaySubCategory, CancellationToken cancellationToken);
    Task<Category?> UpdateCategoryAsync(long id, string name, long? parentCategoryId, bool displaySubCategory, CancellationToken cancellationToken);
    Task<bool> DeleteCategoryAsync(long id, CancellationToken cancellationToken);
}

public sealed class CatalogConflictException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
