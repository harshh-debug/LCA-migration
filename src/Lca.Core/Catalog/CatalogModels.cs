using Lca.Core.Governance;

namespace Lca.Core.Catalog;

public sealed record ProductSearch(
    string TenantId,
    string? Search,
    decimal? CategoryId,
    bool IncludeDrafts,
    int Page,
    int PageSize);

public sealed record ProductDraft(
    string TenantId,
    string Name,
    string? Description,
    string? Specification,
    decimal? CategoryId,
    string CreatedByAgent);

public sealed record ProductApproval(
    string TenantId,
    Guid ApprovalId,
    string ItemCode,
    string ReviewedBy);

public sealed record ApprovalQueueSearch(
    string TenantId,
    ApprovalStatus? Status,
    ApprovalEntityType? EntityType,
    int Page,
    int PageSize);

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public interface ICatalogService
{
    Task<PagedResult<Product>> SearchProductsAsync(ProductSearch search, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(string tenantId, CancellationToken cancellationToken);

    Task<ApprovalQueueItem> CreateProductDraftAsync(ProductDraft draft, CancellationToken cancellationToken);
}

public interface IApprovalQueueService
{
    Task<PagedResult<ApprovalQueueItem>> SearchAsync(ApprovalQueueSearch search, CancellationToken cancellationToken);

    Task<ApprovalQueueItem?> ApproveProductAsync(ProductApproval approval, CancellationToken cancellationToken);
}
