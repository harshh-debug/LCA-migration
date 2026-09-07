namespace Lca.Core.Tenancy;

public sealed class TenantMembership
{
    public required string UserId { get; set; }

    public long TenantId { get; set; }

    public TenantMembershipStatus Status { get; set; } = TenantMembershipStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Tenant? Tenant { get; set; }
}

public enum TenantMembershipStatus
{
    Active,
    Inactive,
}
