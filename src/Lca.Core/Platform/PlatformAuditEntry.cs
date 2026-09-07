namespace Lca.Core.Platform;

public sealed class PlatformAuditEntry
{
    public long Id { get; set; }
    public required string ActorUserId { get; set; }
    public required string Action { get; set; }
    public required string TargetType { get; set; }
    public required string TargetId { get; set; }
    public long? TenantId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public required string CorrelationId { get; set; }
    public string? Details { get; set; }
}
