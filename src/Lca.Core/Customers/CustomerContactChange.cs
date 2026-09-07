using Lca.Core.Tenancy;

namespace Lca.Core.Customers;

public enum ContactChangeOperation { Created, Updated, Deleted, LegacyImported }
public enum ContactChangeReviewStatus { Pending, Acknowledged, ContactDeleted }

public sealed class CustomerContactChange : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long CustomerId { get; set; }
    public long? CustomerContactId { get; set; }
    public ContactChangeOperation Operation { get; set; }
    public required string? BeforeJson { get; set; }
    public required string? AfterJson { get; set; }
    public bool IsMobileChanged { get; set; }
    public ContactChangeReviewStatus ReviewStatus { get; set; } = ContactChangeReviewStatus.Pending;
    public string? ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? Source { get; set; }

    public Customer Customer { get; set; } = null!;
}
