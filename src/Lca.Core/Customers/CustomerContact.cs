using Lca.Core.Tenancy;

namespace Lca.Core.Customers;

public sealed class CustomerContact : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CustomerId { get; set; }
    public required string Name { get; set; }
    public string? Designation { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;
}
