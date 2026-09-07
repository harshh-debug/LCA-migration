using Lca.Core.Tenancy;

namespace Lca.Core.Customers;

public sealed class LegacyCustomerMap : ITenantOwned
{
    public long TenantId { get; set; }
    public decimal LegacyCustomerId { get; set; }
    public long CustomerId { get; set; }
    public required string AccountNumber { get; set; }
    public Customer Customer { get; set; } = null!;
}

public sealed class LegacyCustomerContactMap : ITenantOwned
{
    public long TenantId { get; set; }
    public required string SourceFingerprint { get; set; }
    public long CustomerContactId { get; set; }
    public CustomerContact CustomerContact { get; set; } = null!;
}

public sealed class LegacyCustomerContactChangeMap : ITenantOwned
{
    public long TenantId { get; set; }
    public required string SourceFingerprint { get; set; }
    public long CustomerContactChangeId { get; set; }
    public CustomerContactChange CustomerContactChange { get; set; } = null!;
}
