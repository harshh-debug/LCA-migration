using System.Data;
using System.Globalization;
using System.Text.Json;
using Lca.Core.Catalog;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

const long TenantOneId = 1;
if (args.Any(argument => !string.Equals(argument, "--apply", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Only --apply is supported. This importer is permanently fixed to Tenant 1.");
bool apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
string legacyConnection = Environment.GetEnvironmentVariable("LEGACY_LCA_CONNECTION")
    ?? throw new InvalidOperationException("LEGACY_LCA_CONNECTION is required.");
string targetConnection = Environment.GetEnvironmentVariable("LCA_MIGRATION_CONNECTION")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__LcaDatabase")
    ?? throw new InvalidOperationException("LCA_MIGRATION_CONNECTION is required.");

await using SqlConnection legacy = new(legacyConnection);
await legacy.OpenAsync();
List<LegacyCategory> sourceCategories = await ReadCategoriesAsync(legacy);
List<LegacyProduct> sourceProducts = await ReadProductsAsync(legacy);

List<RejectedRow> rejected = [];
HashSet<string> codes = new(StringComparer.OrdinalIgnoreCase);
HashSet<decimal> categoryIds = sourceCategories.Select(value => value.Id).ToHashSet();
foreach (LegacyProduct product in sourceProducts)
{
    if (string.IsNullOrWhiteSpace(product.ItemCode)) rejected.Add(new("Product", "(blank)", "ItemCode is required."));
    else if (!codes.Add(product.ItemCode)) rejected.Add(new("Product", product.ItemCode, "Duplicate ItemCode."));
    if (string.IsNullOrWhiteSpace(product.Name)) rejected.Add(new("Product", product.ItemCode, "Name is required."));
    if (product.CategoryId.HasValue && !categoryIds.Contains(product.CategoryId.Value))
        rejected.Add(new("Product", product.ItemCode, $"Orphan CategoryID {product.CategoryId}."));
    foreach ((string field, string? value) in product.NumericTextFields())
        if (value is not null && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            rejected.Add(new("Product", product.ItemCode, $"{field} is not numeric: '{value}'."));
    foreach ((string field, decimal? value) in product.PriceAndTaxFields())
        if (value < 0) rejected.Add(new("Product", product.ItemCode, $"{field} cannot be negative."));
}
foreach (LegacyCategory category in sourceCategories.Where(value => string.IsNullOrWhiteSpace(value.Name)))
    rejected.Add(new("Category", category.Id.ToString(CultureInfo.InvariantCulture), "Name is required."));
foreach (LegacyCategory category in sourceCategories.Where(value => value.ParentId.HasValue && !categoryIds.Contains(value.ParentId.Value)))
    rejected.Add(new("Category", category.Id.ToString(CultureInfo.InvariantCulture), $"Orphan parent {category.ParentId}."));
Dictionary<decimal, decimal?> parents = sourceCategories.ToDictionary(value => value.Id, value => value.ParentId);
foreach (LegacyCategory category in sourceCategories)
{
    HashSet<decimal> visited = [];
    decimal? cursor = category.Id;
    while (cursor.HasValue && parents.TryGetValue(cursor.Value, out decimal? parent))
    {
        if (!visited.Add(cursor.Value))
        {
            rejected.Add(new("Category", category.Id.ToString(CultureInfo.InvariantCulture), "Parent relationship contains a cycle."));
            break;
        }
        cursor = parent;
    }
}

DbContextOptions<LcaDbContext> options = new DbContextOptionsBuilder<LcaDbContext>().UseSqlServer(targetConnection).Options;
await using LcaDbContext target = new(options, new TenantOneContext());
Tenant? tenantOne = await target.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(value => value.Id == TenantOneId);
if (tenantOne is null || tenantOne.Status != TenantStatus.Active)
    throw new InvalidOperationException("Canonical active Tenant 1 does not exist; legacy import is refused.");
int existingTargetCategories = await target.Categories.CountAsync();
int existingTargetProducts = await target.Products.CountAsync();

ImportReport report;
if (!apply || rejected.Count > 0)
{
    report = new(false, sourceCategories.Count, sourceProducts.Count, existingTargetCategories, existingTargetProducts, 0, 0, 0, rejected);
}
else
{
    await using var transaction = await target.Database.BeginTransactionAsync();
    DateTime now = DateTime.UtcNow;
    Dictionary<decimal, LegacyCategoryMap> maps = await target.LegacyCategoryMaps.Include(value => value.Category)
        .ToDictionaryAsync(value => value.LegacyCategoryId);
    int categoriesCreated = 0, productsCreated = 0, productsUpdated = 0;
    foreach (LegacyCategory source in sourceCategories)
    {
        if (!maps.TryGetValue(source.Id, out LegacyCategoryMap? map))
        {
            Category category = new() { Name = source.Name!, DisplaySubCategory = source.DisplaySubCategory, LegacyIconPath = source.Icon, LegacyNotificationImagePath = source.NotificationImage, CreatedAtUtc = now, UpdatedAtUtc = now };
            map = new() { LegacyCategoryId = source.Id, Category = category };
            target.LegacyCategoryMaps.Add(map); maps.Add(source.Id, map); categoriesCreated++;
        }
        else
        {
            map.Category.Name = source.Name!; map.Category.DisplaySubCategory = source.DisplaySubCategory;
            map.Category.LegacyIconPath = source.Icon; map.Category.LegacyNotificationImagePath = source.NotificationImage; map.Category.UpdatedAtUtc = now;
        }
    }
    await target.SaveChangesAsync();
    foreach (LegacyCategory source in sourceCategories)
        maps[source.Id].Category.ParentCategoryId = source.ParentId.HasValue ? maps[source.ParentId.Value].CategoryId : null;
    await target.SaveChangesAsync();

    Product[] existing = await target.Products.Include(value => value.Pricing).Include(value => value.Inventory).Include(value => value.Media).ToArrayAsync();
    Dictionary<string, Product> byCode = existing.ToDictionary(value => value.ItemCode, StringComparer.OrdinalIgnoreCase);
    foreach (LegacyProduct source in sourceProducts)
    {
        if (!byCode.TryGetValue(source.ItemCode, out Product? product))
        {
            product = new() { ItemCode = source.ItemCode, Name = source.Name, CreatedAtUtc = now };
            target.Products.Add(product); byCode.Add(source.ItemCode, product); productsCreated++;
        }
        else productsUpdated++;
        product.Name = source.Name; product.Unit = source.Unit; product.AlternateItemCode = source.AlternateItemCode;
        product.GujaratiName = source.GujaratiName; product.UnitKilograms = ParseDecimal(source.UnitKilograms);
        product.Group1 = source.Group1; product.Group2 = source.Group2; product.ChapterNumber = source.ChapterNumber;
        product.HsnNumber = source.HsnNumber; product.ItemType = source.ItemType; product.Packing = source.Packing;
        product.ManufacturerName = source.ManufacturerName; product.Location = source.Location; product.AlternateLocation = source.AlternateLocation;
        product.WarrantyYears = ParseInt(source.WarrantyYears); product.WarrantyMonths = ParseInt(source.WarrantyMonths);
        product.Description = source.Description; product.Remark = source.Remark; product.SalesmanCommission = source.SalesmanCommission;
        product.CategoryId = source.CategoryId.HasValue ? maps[source.CategoryId.Value].CategoryId : null;
        product.IsDisabled = source.IsDisabled; product.UpdatedAtUtc = now;
        product.Pricing ??= new();
        product.Pricing.PurchaseRate = source.PurchaseRate ?? 0; product.Pricing.DealerRate = source.DealerRate ?? 0;
        product.Pricing.WholesaleRate = source.WholesaleRate ?? 0; product.Pricing.RetailRate = source.RetailRate ?? 0; product.Pricing.OtherRate = source.OtherRate ?? 0;
        product.Pricing.VatRate = source.VatRate; product.Pricing.AdditionalVatRate = source.AdditionalVatRate; product.Pricing.CstRate = source.CstRate;
        product.Pricing.IgstRate = source.IgstRate ?? 0; product.Pricing.SgstRate = source.SgstRate ?? 0; product.Pricing.CgstRate = source.CgstRate ?? 0; product.Pricing.UpdatedAtUtc = now;
        product.Inventory ??= new();
        product.Inventory.Balance = source.Balance; product.Inventory.CurrentStock = ParseDecimal(source.CurrentStock);
        product.Inventory.MaximumStock = ParseDecimal(source.MaximumStock); product.Inventory.MinimumStock = ParseDecimal(source.MinimumStock);
        product.Inventory.Godown1Stock = ParseDecimal(source.Godown1Stock); product.Inventory.Godown2Stock = ParseDecimal(source.Godown2Stock); product.Inventory.UpdatedAtUtc = now;
        product.Media.Clear();
        foreach ((string path, int order, bool thumbnail) in source.Media()) product.Media.Add(new() { LegacyPath = path, SortOrder = order, IsThumbnail = thumbnail });
    }
    await target.SaveChangesAsync();
    await transaction.CommitAsync();
    int targetCategories = await target.Categories.CountAsync();
    int targetProducts = await target.Products.CountAsync();
    report = new(true, sourceCategories.Count, sourceProducts.Count, targetCategories, targetProducts, categoriesCreated, productsCreated, productsUpdated, []);
}

Console.WriteLine(JsonSerializer.Serialize(report, Serialization.Options));
Environment.ExitCode = rejected.Count == 0 ? 0 : 2;

static async Task<List<LegacyCategory>> ReadCategoriesAsync(SqlConnection connection)
{
    const string sql = "SELECT CategoryID, CategoryName, ParentCategoryID, DisplaySubCategory, CategoryIcon, NotificationImage FROM dbo.CategoryMastertbl ORDER BY CategoryID";
    await using SqlCommand command = new(sql, connection); await using SqlDataReader reader = await command.ExecuteReaderAsync();
    List<LegacyCategory> rows = [];
    while (await reader.ReadAsync()) rows.Add(new(reader.GetDecimal(0), Text(reader, 1), Decimal(reader, 2), Bool(reader, 3), Text(reader, 4), Text(reader, 5)));
    return rows;
}

static async Task<List<LegacyProduct>> ReadProductsAsync(SqlConnection connection)
{
    const string sql = """
        SELECT ItemCode, Item, Unit, Altitemcode, Gujitem, Unit_kgs, Group1, Group2, Chapter_no, Hsnno, Itemtype, Packing,
               Mfg_name, Location, Location2, Warrantyyear, Warrantymonth, COALESCE(Itemdisc, Description), COALESCE(Itremark, Specification),
               Salesmancommission, CategoryID, Disable, Purrate, Delearrt, Wholesaler, Rretailrt, Otherrt, Vatrt, Addivatrt, Cstrt,
               Igstrt, Sgstrt, Cgstrt, Balance, Curstock, Max_stock, Min_stock, God01stock, God02stock,
               Image1, Image2, Image3, Image4, Image5, Image6, Image7, Image8, Image9, ThumbImage
        FROM dbo.Mobile_ItemMaster ORDER BY ItemCode
        """;
    await using SqlCommand command = new(sql, connection); await using SqlDataReader reader = await command.ExecuteReaderAsync();
    List<LegacyProduct> rows = [];
    while (await reader.ReadAsync())
    {
        rows.Add(new(Text(reader, 0) ?? "", Text(reader, 1) ?? "", Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5), Text(reader, 6), Text(reader, 7), Text(reader, 8), Text(reader, 9), Text(reader, 10), Text(reader, 11), Text(reader, 12), Text(reader, 13), Text(reader, 14), Text(reader, 15), Text(reader, 16), Text(reader, 17), Text(reader, 18), Decimal(reader, 19), Decimal(reader, 20), Bool(reader, 21), Decimal(reader, 22), Decimal(reader, 23), Decimal(reader, 24), Decimal(reader, 25), Decimal(reader, 26), Decimal(reader, 27), Decimal(reader, 28), Decimal(reader, 29), Decimal(reader, 30), Decimal(reader, 31), Decimal(reader, 32), Decimal(reader, 33), Text(reader, 34), Text(reader, 35), Text(reader, 36), Text(reader, 37), Text(reader, 38), Enumerable.Range(39, 9).Select(index => Text(reader, index)).ToArray(), Text(reader, 48)));
    }
    return rows;
}

static string? Text(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() is { Length: > 0 } value ? value : null;
static decimal? Decimal(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
static bool Bool(SqlDataReader reader, int ordinal) => !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
static decimal? ParseDecimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;
static int? ParseInt(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;

sealed class TenantOneContext : ITenantContext { public bool IsAvailable => true; public TenantId? TenantId { get; } = new(1); }
static class Serialization { public static readonly JsonSerializerOptions Options = new() { WriteIndented = true }; }
sealed record RejectedRow(string Entity, string Key, string Reason);
sealed record ImportReport(bool Applied, int SourceCategories, int SourceProducts, int TargetCategories, int TargetProducts, int CategoriesCreated, int ProductsCreated, int ProductsUpdated, IReadOnlyCollection<RejectedRow> Rejected);
sealed record LegacyCategory(decimal Id, string? Name, decimal? ParentId, bool DisplaySubCategory, string? Icon, string? NotificationImage);
sealed record LegacyProduct(string ItemCode, string Name, string? Unit, string? AlternateItemCode, string? GujaratiName, string? UnitKilograms,
    string? Group1, string? Group2, string? ChapterNumber, string? HsnNumber, string? ItemType, string? Packing, string? ManufacturerName,
    string? Location, string? AlternateLocation, string? WarrantyYears, string? WarrantyMonths, string? Description, string? Remark,
    decimal? SalesmanCommission, decimal? CategoryId, bool IsDisabled, decimal? PurchaseRate, decimal? DealerRate, decimal? WholesaleRate,
    decimal? RetailRate, decimal? OtherRate, decimal? VatRate, decimal? AdditionalVatRate, decimal? CstRate, decimal? IgstRate, decimal? SgstRate,
    decimal? CgstRate, decimal? Balance, string? CurrentStock, string? MaximumStock, string? MinimumStock, string? Godown1Stock, string? Godown2Stock,
    string?[] Images, string? Thumbnail)
{
    public IEnumerable<(string Path, int Order, bool Thumbnail)> Media()
    {
        for (int index = 0; index < Images.Length; index++) if (Images[index] is { } path) yield return (path, index + 1, false);
        if (Thumbnail is { } thumbnail) yield return (thumbnail, 0, true);
    }
    public IEnumerable<(string Field, string? Value)> NumericTextFields()
    {
        yield return (nameof(UnitKilograms), UnitKilograms); yield return (nameof(WarrantyYears), WarrantyYears); yield return (nameof(WarrantyMonths), WarrantyMonths);
        yield return (nameof(CurrentStock), CurrentStock); yield return (nameof(MaximumStock), MaximumStock); yield return (nameof(MinimumStock), MinimumStock);
        yield return (nameof(Godown1Stock), Godown1Stock); yield return (nameof(Godown2Stock), Godown2Stock);
    }
    public IEnumerable<(string Field, decimal? Value)> PriceAndTaxFields()
    {
        yield return (nameof(PurchaseRate), PurchaseRate); yield return (nameof(DealerRate), DealerRate); yield return (nameof(WholesaleRate), WholesaleRate);
        yield return (nameof(RetailRate), RetailRate); yield return (nameof(OtherRate), OtherRate); yield return (nameof(VatRate), VatRate);
        yield return (nameof(AdditionalVatRate), AdditionalVatRate); yield return (nameof(CstRate), CstRate); yield return (nameof(IgstRate), IgstRate);
        yield return (nameof(SgstRate), SgstRate); yield return (nameof(CgstRate), CgstRate);
    }
}
