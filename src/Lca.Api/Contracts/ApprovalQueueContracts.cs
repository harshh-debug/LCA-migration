using System.ComponentModel.DataAnnotations;

using Lca.Core.Governance;

namespace Lca.Api.Contracts;

public sealed class ApprovalQueueSearchRequest
{
    public ApprovalStatus? Status { get; init; }

    public ApprovalEntityType? EntityType { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}

public sealed class ApproveProductRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public required string ItemCode { get; init; }
}

public sealed record ApprovalQueueItemResponse(
    Guid Id,
    string EntityType,
    string EntityId,
    string DraftPayloadJson,
    string Status,
    string CreatedByAgent,
    DateTime CreatedAt,
    string? ReviewedBy,
    DateTime? ReviewedAt);
