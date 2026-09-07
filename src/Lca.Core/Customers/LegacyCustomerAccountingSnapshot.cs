using Lca.Core.Tenancy;

namespace Lca.Core.Customers;

public sealed class LegacyCustomerAccountingSnapshot : ITenantOwned
{
    public long CustomerId { get; set; }
    public long TenantId { get; set; }
    public decimal? DueBalance { get; set; }
    public decimal? OpeningBalance { get; set; }
    public string? BlockLevel { get; set; }
    public string? PreviousBlockLevel { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime? LegacyUpdatedAtUtc { get; set; }

    public Customer Customer { get; set; } = null!;
}
