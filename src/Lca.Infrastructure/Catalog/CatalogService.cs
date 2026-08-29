using System.Text.Json;

using Lca.Core.Catalog;
using Lca.Core.Governance;
using Lca.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Catalog;

internal sealed class CatalogService(ITenantDbContextFactory contextFactory, TimeProvider timeProvider) : ICatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<Product>> SearchProductsAsync(ProductSearch search, CancellationToken cancellationToken)
    {
        await using LcaDbContext context = contextFactory.Create(search.TenantId);
        IQueryable<Product> query = context.Products.AsNoTracking();

        if (!search.IncludeDrafts)
        {
            query = query.Where(product => !product.IsDraft);
        }

        if (search.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == search.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            string pattern = $"%{EscapeLikePattern(search.Search.Trim())}%";
            query = query.Where(product =>
                EF.Functions.Like(product.ItemCode, pattern, "\\")
                || (product.Name != null && EF.Functions.Like(product.Name, pattern, "\\"))
                || (product.Description != null && EF.Functions.Like(product.Description, pattern, "\\")));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        Product[] items = await query
            .OrderBy(product => product.ItemCode)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Product>(items, search.Page, search.PageSize, totalCount);
    }

    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(
        string tenantId,
        CancellationToken cancellationToken)
    {
        await using LcaDbContext context = contextFactory.Create(tenantId);
        return await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ApprovalQueueItem> CreateProductDraftAsync(
        ProductDraft draft,
        CancellationToken cancellationToken)
    {
        await using LcaDbContext context = contextFactory.Create(draft.TenantId);

        if (draft.CategoryId.HasValue
            && !await context.Categories.AnyAsync(category => category.Id == draft.CategoryId.Value, cancellationToken))
        {
            throw new InvalidProductDraftException("The selected category does not exist in the tenant database.");
        }

        Guid draftId = Guid.NewGuid();
        ProductDraftPayload payload = new(draft.Name, draft.Description, draft.Specification, draft.CategoryId);
        ApprovalQueueItem queueItem = new()
        {
            Id = Guid.NewGuid(),
            TenantId = draft.TenantId,
            EntityType = ApprovalEntityType.Product,
            EntityId = draftId.ToString("N"),
            DraftPayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            Status = ApprovalStatus.Pending,
            CreatedByAgent = draft.CreatedByAgent,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        context.ApprovalQueue.Add(queueItem);
        await context.SaveChangesAsync(cancellationToken);
        return queueItem;
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
}

public sealed class InvalidProductDraftException(string message) : InvalidOperationException(message);

internal sealed record ProductDraftPayload(
    string Name,
    string? Description,
    string? Specification,
    decimal? CategoryId);
