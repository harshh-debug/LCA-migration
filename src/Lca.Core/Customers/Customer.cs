using Lca.Core.Tenancy;

namespace Lca.Core.Customers;

public sealed class Customer : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public required string AccountNumber { get; set; }
    public required string CompanyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? MobileNumber { get; set; }
    public string? OfficePhone { get; set; }
    public string? ResidentialPhone { get; set; }
    public string? Email { get; set; }
    public string? ManagementContactPerson { get; set; }
    public string? ManagementMobileNumber { get; set; }
    public string? ManagementEmail { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? DefaultShippingAddressLine1 { get; set; }
    public string? DefaultShippingAddressLine2 { get; set; }
    public string? DefaultShippingAddressLine3 { get; set; }
    public string? Area { get; set; }
    public string? Gstin { get; set; }
    public string? TinNumber { get; set; }
    public string? CstNumber { get; set; }
    public CustomerPriceBand PriceBand { get; set; } = CustomerPriceBand.Other;
    public int? CreditDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? DefaultTransportName { get; set; }
    public string? SalespersonReference { get; set; }
    public bool IsDisabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<CustomerContact> Contacts { get; set; } = [];
    public ICollection<CustomerContactChange> ContactChanges { get; set; } = [];
    public LegacyCustomerAccountingSnapshot? LegacyAccountingSnapshot { get; set; }
}
