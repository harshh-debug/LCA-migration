using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Governance;
using Lca.Core.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController]
[Route("api/v1/approval-queue")]
public sealed class ApprovalQueueController(
    IApprovalQueueService approvalQueueService,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.ApprovalQueueRead)]
    [ProducesResponseType<PagedResponse<ApprovalQueueItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ApprovalQueueItemResponse>>> GetQueue(
        [FromQuery] ApprovalQueueSearchRequest request,
        CancellationToken cancellationToken)
    {
        PagedResult<ApprovalQueueItem> result = await approvalQueueService.SearchAsync(
            new ApprovalQueueSearch(
                tenantContext.TenantId!.Value.Value,
                request.Status,
                request.EntityType,
                request.Page,
                request.PageSize),
            cancellationToken);
        ApprovalQueueItemResponse[] items = result.Items.Select(Map).ToArray();
        return Ok(new PagedResponse<ApprovalQueueItemResponse>(items, result.Page, result.PageSize, result.TotalCount));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.ApprovalQueueApprove)]
    [ProducesResponseType<ApprovalQueueItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApprovalQueueItemResponse>> Approve(
        Guid id,
        [FromBody] ApproveProductRequest request,
        CancellationToken cancellationToken)
    {
        ApprovalQueueItem? item = await approvalQueueService.ApproveProductAsync(
            new ProductApproval(
                tenantContext.TenantId!.Value.Value,
                id,
                request.ItemCode.Trim(),
                currentUser.UserId!),
            cancellationToken);
        return item is null ? NotFound() : Ok(Map(item));
    }

    private static ApprovalQueueItemResponse Map(ApprovalQueueItem item) => new(
        item.Id,
        item.EntityType.ToString(),
        item.EntityId,
        item.DraftPayloadJson,
        item.Status.ToString(),
        item.CreatedByAgent,
        item.CreatedAt,
        item.ReviewedBy,
        item.ReviewedAt);
}
