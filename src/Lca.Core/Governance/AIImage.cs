using Lca.Core.Catalog;

namespace Lca.Core.Governance;

public sealed class AIImage
{
    public int Id { get; set; }

    public required string ProductId { get; set; }

    public required string ImageUrl { get; set; }

    public int SlotPosition { get; set; }

    public AIImageStatus Status { get; set; } = AIImageStatus.Draft;

    public required string AgentId { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? AuditLogId { get; set; }

    public Product? Product { get; set; }
}

public enum AIImageStatus
{
    Draft,
    Approved,
    Rejected,
}
