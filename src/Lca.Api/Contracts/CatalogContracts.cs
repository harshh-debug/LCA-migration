using System.ComponentModel.DataAnnotations;
using Lca.Core.Catalog;

namespace Lca.Api.Contracts;

public sealed class ProductSearchRequest
{
    [MaxLength(200)] public string? Search { get; init; }
    public long? CategoryId { get; init; }
    public ProductStatusFilter Status { get; init; } = ProductStatusFilter.Active;
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
}

public sealed class ProductWriteRequest : IValidatableObject
{
    [Required, StringLength(20, MinimumLength = 1)] public required string ItemCode { get; init; }
    [Required, StringLength(500, MinimumLength = 1)] public required string Name { get; init; }
    [MaxLength(50)] public string? Unit { get; init; }
    [MaxLength(100)] public string? AlternateItemCode { get; init; }
    [MaxLength(500)] public string? GujaratiName { get; init; }
    public decimal? UnitKilograms { get; init; }
    [MaxLength(200)] public string? Group1 { get; init; }
    [MaxLength(200)] public string? Group2 { get; init; }
    [MaxLength(100)] public string? ChapterNumber { get; init; }
    [MaxLength(100)] public string? HsnNumber { get; init; }
    [MaxLength(100)] public string? ItemType { get; init; }
    [MaxLength(100)] public string? Packing { get; init; }
    [MaxLength(500)] public string? ManufacturerName { get; init; }
    [MaxLength(500)] public string? Location { get; init; }
    [MaxLength(500)] public string? AlternateLocation { get; init; }
    [Range(0, 100)] public int? WarrantyYears { get; init; }
    [Range(0, 11)] public int? WarrantyMonths { get; init; }
    public string? Description { get; init; }
    public string? Remark { get; init; }
    public decimal? SalesmanCommission { get; init; }
    public long? CategoryId { get; init; }
    public bool IsDisabled { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ItemCode)) yield return new ValidationResult("ItemCode cannot be blank.", [nameof(ItemCode)]);
        if (string.IsNullOrWhiteSpace(Name)) yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);
    }
}

public sealed class PricingWriteRequest
{
    [Range(0, double.MaxValue)] public decimal PurchaseRate { get; init; }
    [Range(0, double.MaxValue)] public decimal DealerRate { get; init; }
    [Range(0, double.MaxValue)] public decimal WholesaleRate { get; init; }
    [Range(0, double.MaxValue)] public decimal RetailRate { get; init; }
    [Range(0, double.MaxValue)] public decimal OtherRate { get; init; }
    [Range(0, 100)] public decimal? VatRate { get; init; }
    [Range(0, 100)] public decimal? AdditionalVatRate { get; init; }
    [Range(0, 100)] public decimal? CstRate { get; init; }
    [Range(0, 100)] public decimal IgstRate { get; init; }
    [Range(0, 100)] public decimal SgstRate { get; init; }
    [Range(0, 100)] public decimal CgstRate { get; init; }
}

public sealed class InventoryWriteRequest
{
    public decimal? Balance { get; init; }
    public decimal? CurrentStock { get; init; }
    public decimal? MaximumStock { get; init; }
    public decimal? MinimumStock { get; init; }
    public decimal? Godown1Stock { get; init; }
    public decimal? Godown2Stock { get; init; }
}

public sealed class CategoryWriteRequest : IValidatableObject
{
    [Required, StringLength(500, MinimumLength = 1)] public required string Name { get; init; }
    public long? ParentCategoryId { get; init; }
    public bool DisplaySubCategory { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name)) yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);
    }
}

public sealed record ProductResponse(
    long Id, string ItemCode, string Name, string? Unit, string? AlternateItemCode, string? GujaratiName,
    decimal? UnitKilograms, string? Group1, string? Group2, string? ChapterNumber, string? HsnNumber,
    string? ItemType, string? Packing, string? ManufacturerName, string? Location, string? AlternateLocation,
    int? WarrantyYears, int? WarrantyMonths, string? Description, string? Remark, decimal? SalesmanCommission,
    CategoryReferenceResponse? Category, ProductPricingResponse? Pricing, ProductInventoryResponse? Inventory,
    bool IsDisabled, IReadOnlyCollection<ProductMediaResponse> Media, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record ProductPricingResponse(
    decimal PurchaseRate, decimal DealerRate, decimal WholesaleRate, decimal RetailRate, decimal OtherRate,
    decimal? VatRate, decimal? AdditionalVatRate, decimal? CstRate, decimal IgstRate, decimal SgstRate, decimal CgstRate);
public sealed record ProductInventoryResponse(decimal? Balance, decimal? CurrentStock, decimal? MaximumStock, decimal? MinimumStock, decimal? Godown1Stock, decimal? Godown2Stock);
public sealed record ProductMediaResponse(long Id, string LegacyPath, int SortOrder, bool IsThumbnail);
public sealed record CategoryReferenceResponse(long Id, string Name);
public sealed record CategoryResponse(long Id, string Name, long? ParentCategoryId, bool DisplaySubCategory, string? LegacyIconPath, string? LegacyNotificationImagePath);
