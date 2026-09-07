namespace Lca.Core.Tenancy;

public sealed class Tenant
{
    public long Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public enum TenantStatus
{
    Active,
    Inactive,
}
