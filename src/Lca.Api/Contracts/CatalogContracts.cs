using System.ComponentModel.DataAnnotations;

namespace Lca.Api.Contracts;

public sealed class ProductSearchRequest
{
    [MaxLength(200)]
    public string? Search { get; init; }

    public decimal? CategoryId { get; init; }

    public bool IncludeDrafts { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}

public sealed class CreateProductDraftRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Name { get; init; }

    [MaxLength(20_000)]
    public string? Description { get; init; }

    [MaxLength(20_000)]
    public string? Specification { get; init; }

    public decimal? CategoryId { get; init; }
}

public sealed record ProductResponse(
    string ItemCode,
    string? Name,
    string? Description,
    string? Specification,
    CategoryReferenceResponse? Category,
    ProductPriceResponse Prices,
    bool IsDisabled,
    bool IsDraft,
    string CreatedSource,
    IReadOnlyCollection<string> Images,
    string? ThumbnailImage);

public sealed record ProductPriceResponse(decimal? Retail, decimal? Wholesale, decimal? Dealer);

public sealed record CategoryReferenceResponse(decimal Id, string? Name);

public sealed record CategoryResponse(
    decimal Id,
    string? Name,
    decimal? ParentCategoryId,
    bool DisplaySubCategory,
    string? Icon,
    string? NotificationImage);

public sealed record ProductDraftCreatedResponse(Guid ApprovalId, string DraftId, string Status, DateTime CreatedAt);
