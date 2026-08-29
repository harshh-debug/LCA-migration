using System.Text.Json;

using Lca.Core.Catalog;
using Lca.Core.Governance;
using Lca.Infrastructure.Catalog;
using Lca.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Governance;

internal sealed class ApprovalQueueService(ITenantDbContextFactory contextFactory, TimeProvider timeProvider)
    : IApprovalQueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResult<ApprovalQueueItem>> SearchAsync(
        ApprovalQueueSearch search,
        CancellationToken cancellationToken)
    {
        await using LcaDbContext context = contextFactory.Create(search.TenantId);
        IQueryable<ApprovalQueueItem> query = context.ApprovalQueue
            .AsNoTracking()
            .Where(item => item.TenantId == search.TenantId);

        if (search.Status.HasValue)
        {
            query = query.Where(item => item.Status == search.Status.Value);
        }

        if (search.EntityType.HasValue)
        {
            query = query.Where(item => item.EntityType == search.EntityType.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        ApprovalQueueItem[] items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<ApprovalQueueItem>(items, search.Page, search.PageSize, totalCount);
    }

    public async Task<ApprovalQueueItem?> ApproveProductAsync(
        ProductApproval approval,
        CancellationToken cancellationToken)
    {
        await using LcaDbContext context = contextFactory.Create(approval.TenantId);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        ApprovalQueueItem? queueItem = await context.ApprovalQueue.SingleOrDefaultAsync(
            item => item.Id == approval.ApprovalId && item.TenantId == approval.TenantId,
            cancellationToken);

        if (queueItem is null)
        {
            return null;
        }

        if (queueItem.Status != ApprovalStatus.Pending || queueItem.EntityType != ApprovalEntityType.Product)
        {
            throw new ApprovalConflictException("Only pending Product approvals can use this operation.");
        }

        if (await context.Products.AnyAsync(product => product.ItemCode == approval.ItemCode, cancellationToken))
        {
            throw new ApprovalConflictException("The reviewer-supplied item code already exists.");
        }

        ProductDraftPayload payload = JsonSerializer.Deserialize<ProductDraftPayload>(queueItem.DraftPayloadJson, JsonOptions)
            ?? throw new ApprovalConflictException("The stored draft payload is invalid.");

        context.Products.Add(new Product
        {
            ItemCode = approval.ItemCode,
            Name = payload.Name,
            Description = payload.Description,
            Specification = payload.Specification,
            CategoryId = payload.CategoryId,
            IsDraft = false,
            CreatedSource = "AIBot",
        });

        queueItem.Status = ApprovalStatus.Approved;
        queueItem.ReviewedBy = approval.ReviewedBy;
        queueItem.ReviewedAt = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return queueItem;
    }
}

public sealed class ApprovalConflictException(string message) : InvalidOperationException(message);
