namespace Lca.Core.Governance;

public sealed class ApprovalQueueItem
{
    public Guid Id { get; set; }

    public required string TenantId { get; set; }

    public ApprovalEntityType EntityType { get; set; }

    public required string EntityId { get; set; }

    public required string DraftPayloadJson { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public required string CreatedByAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
}

public enum ApprovalEntityType
{
    Product,
    Media,
    MarketingPost,
    LogisticsBooking,
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
}
