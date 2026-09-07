namespace Lca.Core.Customers;

public enum CustomerPriceBand { Purchase, Dealer, Wholesale, Retail, Other }
public enum CustomerStatusFilter { Active, Inactive, All }

public sealed record CustomerSearch(
    string? Search, CustomerStatusFilter Status, CustomerPriceBand? PriceBand,
    string? Area, int Page, int PageSize);

public sealed record CustomerCreateModel(string AccountNumber, CustomerWriteModel Customer);

public sealed record CustomerWriteModel(
    string CompanyName,
    string? ContactPerson, string? MobileNumber, string? OfficePhone, string? ResidentialPhone, string? Email,
    string? ManagementContactPerson, string? ManagementMobileNumber, string? ManagementEmail,
    string? AddressLine1, string? AddressLine2, string? AddressLine3,
    string? City, string? District, string? State, string? PostalCode,
    string? DefaultShippingAddressLine1, string? DefaultShippingAddressLine2, string? DefaultShippingAddressLine3,
    string? Area, string? Gstin, string? TinNumber, string? CstNumber,
    CustomerPriceBand PriceBand, int? CreditDays, decimal? CreditLimit,
    string? DefaultTransportName, string? SalespersonReference, bool IsDisabled);

public sealed record CustomerContactWriteModel(string Name, string? Designation, string? Mobile, string? Email);
public sealed record CustomerContactSnapshot(string Name, string? Designation, string? Mobile, string? Email);

public sealed record ContactChangeSearch(ContactChangeReviewStatus? ReviewStatus, int Page, int PageSize);

public interface ICustomerService
{
    Task<Lca.Core.Catalog.PagedResult<Customer>> SearchAsync(CustomerSearch search, CancellationToken cancellationToken);
    Task<Customer?> GetAsync(long id, CancellationToken cancellationToken);
    Task<Customer> CreateAsync(CustomerCreateModel model, CancellationToken cancellationToken);
    Task<Customer?> UpdateAsync(long id, CustomerWriteModel model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    Task<CustomerContact?> CreateContactAsync(long customerId, CustomerContactWriteModel model, CancellationToken cancellationToken);
    Task<CustomerContact?> UpdateContactAsync(long customerId, long contactId, CustomerContactWriteModel model, CancellationToken cancellationToken);
    Task<bool> DeleteContactAsync(long customerId, long contactId, CancellationToken cancellationToken);
    Task<Lca.Core.Catalog.PagedResult<CustomerContactChange>> SearchContactChangesAsync(ContactChangeSearch search, long? customerId, CancellationToken cancellationToken);
    Task<CustomerContactChange?> AcknowledgeContactChangeAsync(long id, CancellationToken cancellationToken);
}

public sealed class CustomerConflictException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
