using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Lca.Core.Customers;

namespace Lca.Api.Contracts;

public sealed class CustomerSearchRequest
{
    [MaxLength(200)] public string? Search { get; init; }
    public CustomerStatusFilter Status { get; init; } = CustomerStatusFilter.Active;
    public CustomerPriceBand? PriceBand { get; init; }
    [MaxLength(200)] public string? Area { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
}

public sealed class CustomerCreateRequest : CustomerWriteRequest
{
    [Required, StringLength(50, MinimumLength = 1)] public required string AccountNumber { get; init; }
}

public class CustomerWriteRequest : IValidatableObject
{
    [Required, StringLength(500, MinimumLength = 1)] public required string CompanyName { get; init; }
    [MaxLength(200)] public string? ContactPerson { get; init; }
    [MaxLength(200)] public string? MobileNumber { get; init; }
    [MaxLength(200)] public string? OfficePhone { get; init; }
    [MaxLength(200)] public string? ResidentialPhone { get; init; }
    [MaxLength(200)] public string? Email { get; init; }
    [MaxLength(200)] public string? ManagementContactPerson { get; init; }
    [MaxLength(200)] public string? ManagementMobileNumber { get; init; }
    [MaxLength(200)] public string? ManagementEmail { get; init; }
    [MaxLength(1000)] public string? AddressLine1 { get; init; }
    [MaxLength(1000)] public string? AddressLine2 { get; init; }
    [MaxLength(1000)] public string? AddressLine3 { get; init; }
    [MaxLength(200)] public string? City { get; init; }
    [MaxLength(200)] public string? District { get; init; }
    [MaxLength(200)] public string? State { get; init; }
    [MaxLength(200)] public string? PostalCode { get; init; }
    [MaxLength(1000)] public string? DefaultShippingAddressLine1 { get; init; }
    [MaxLength(1000)] public string? DefaultShippingAddressLine2 { get; init; }
    [MaxLength(1000)] public string? DefaultShippingAddressLine3 { get; init; }
    [MaxLength(200)] public string? Area { get; init; }
    [MaxLength(200)] public string? Gstin { get; init; }
    [MaxLength(200)] public string? TinNumber { get; init; }
    [MaxLength(200)] public string? CstNumber { get; init; }
    public CustomerPriceBand PriceBand { get; init; } = CustomerPriceBand.Other;
    [Range(0, int.MaxValue)] public int? CreditDays { get; init; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")] public decimal? CreditLimit { get; init; }
    [MaxLength(200)] public string? DefaultTransportName { get; init; }
    [MaxLength(200)] public string? SalespersonReference { get; init; }
    public bool IsDisabled { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(CompanyName))
            yield return new ValidationResult("CompanyName cannot be blank.", [nameof(CompanyName)]);
    }
}

public sealed class CustomerContactWriteRequest : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)] public required string Name { get; init; }
    [MaxLength(200)] public string? Designation { get; init; }
    [MaxLength(100)] public string? Mobile { get; init; }
    [MaxLength(200)] public string? Email { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name)) yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);
    }
}

public sealed class ContactChangeSearchRequest
{
    public ContactChangeReviewStatus? ReviewStatus { get; init; }
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int PageSize { get; init; } = 25;
}

public sealed record CustomerSummaryResponse(
    long Id, string AccountNumber, string CompanyName, string? ContactPerson, string? MobileNumber,
    string? ManagementMobileNumber, string? Area, CustomerPriceBand PriceBand, bool IsDisabled,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CustomerResponse(
    long Id, string AccountNumber, string CompanyName,
    string? ContactPerson, string? MobileNumber, string? OfficePhone, string? ResidentialPhone, string? Email,
    string? ManagementContactPerson, string? ManagementMobileNumber, string? ManagementEmail,
    string? AddressLine1, string? AddressLine2, string? AddressLine3,
    string? City, string? District, string? State, string? PostalCode,
    string? DefaultShippingAddressLine1, string? DefaultShippingAddressLine2, string? DefaultShippingAddressLine3,
    string? Area, string? Gstin, string? TinNumber, string? CstNumber,
    CustomerPriceBand PriceBand, int? CreditDays, decimal? CreditLimit,
    string? DefaultTransportName, string? SalespersonReference, bool IsDisabled,
    IReadOnlyCollection<CustomerContactResponse> Contacts,
    LegacyCustomerAccountingSnapshotResponse? LegacyAccountingSnapshot,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record CustomerContactResponse(
    long Id, long CustomerId, string Name, string? Designation, string? Mobile, string? Email,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record LegacyCustomerAccountingSnapshotResponse(
    decimal? DueBalance, decimal? OpeningBalance, string? BlockLevel, string? PreviousBlockLevel,
    DateTime CapturedAtUtc, DateTime? LegacyUpdatedAtUtc, bool IsAuthoritative = false);

public sealed record CustomerContactSnapshotResponse(string Name, string? Designation, string? Mobile, string? Email);

public sealed record CustomerContactChangeResponse(
    long Id, long CustomerId, long? CustomerContactId, ContactChangeOperation Operation,
    CustomerContactSnapshotResponse? Before, CustomerContactSnapshotResponse? After,
    bool IsMobileChanged, ContactChangeReviewStatus ReviewStatus,
    string? ChangedByUserId, DateTime ChangedAtUtc, string? ReviewedByUserId, DateTime? ReviewedAtUtc, string? Source)
{
    public static CustomerContactChangeResponse From(CustomerContactChange value) => new(
        value.Id, value.CustomerId, value.CustomerContactId, value.Operation,
        Parse(value.BeforeJson), Parse(value.AfterJson), value.IsMobileChanged, value.ReviewStatus,
        value.ChangedByUserId, value.ChangedAtUtc, value.ReviewedByUserId, value.ReviewedAtUtc, value.Source);

    private static CustomerContactSnapshotResponse? Parse(string? json)
    {
        if (json is null) return null;
        CustomerContactSnapshot? value = JsonSerializer.Deserialize<CustomerContactSnapshot>(json);
        return value is null ? null : new(value.Name, value.Designation, value.Mobile, value.Email);
    }
}
