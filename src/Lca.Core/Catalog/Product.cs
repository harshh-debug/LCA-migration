using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class Product : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public required string ItemCode { get; set; }
    public required string Name { get; set; }
    public string? Unit { get; set; }
    public string? AlternateItemCode { get; set; }
    public string? GujaratiName { get; set; }
    public decimal? UnitKilograms { get; set; }
    public string? Group1 { get; set; }
    public string? Group2 { get; set; }
    public string? ChapterNumber { get; set; }
    public string? HsnNumber { get; set; }
    public string? ItemType { get; set; }
    public string? Packing { get; set; }
    public string? ManufacturerName { get; set; }
    public string? Location { get; set; }
    public string? AlternateLocation { get; set; }
    public int? WarrantyYears { get; set; }
    public int? WarrantyMonths { get; set; }
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public decimal? SalesmanCommission { get; set; }
    public long? CategoryId { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Category? Category { get; set; }
    public ProductPricing? Pricing { get; set; }
    public ProductInventory? Inventory { get; set; }
    public ICollection<ProductMedia> Media { get; set; } = [];
}
