using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController, Route("api/v1/customers"), Authorize(Policy = Policies.TenantAccess)]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerSummaryResponse>>> GetCustomers(
        [FromQuery] CustomerSearchRequest request, CancellationToken cancellationToken)
    {
        PagedResult<Customer> result = await customerService.SearchAsync(new(
            request.Search, request.Status, request.PriceBand, request.Area, request.Page, request.PageSize), cancellationToken);
        return Ok(new PagedResponse<CustomerSummaryResponse>(
            result.Items.Select(MapSummary).ToArray(), result.Page, result.PageSize, result.TotalCount));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(long id, CancellationToken cancellationToken)
    {
        Customer? customer = await customerService.GetAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(Map(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        Customer customer = await customerService.CreateAsync(new(request.AccountNumber, ToModel(request)), cancellationToken);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, Map(customer));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(long id, CustomerWriteRequest request, CancellationToken cancellationToken)
    {
        Customer? customer = await customerService.UpdateAsync(id, ToModel(request), cancellationToken);
        return customer is null ? NotFound() : Ok(Map(customer));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCustomer(long id, CancellationToken cancellationToken) =>
        await customerService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("{customerId:long}/contacts")]
    public async Task<ActionResult<CustomerContactResponse>> CreateContact(
        long customerId, CustomerContactWriteRequest request, CancellationToken cancellationToken)
    {
        CustomerContact? contact = await customerService.CreateContactAsync(customerId, ToModel(request), cancellationToken);
        return contact is null ? NotFound() : Created($"/api/v1/customers/{customerId}/contacts/{contact.Id}", Map(contact));
    }

    [HttpPut("{customerId:long}/contacts/{contactId:long}")]
    public async Task<ActionResult<CustomerContactResponse>> UpdateContact(
        long customerId, long contactId, CustomerContactWriteRequest request, CancellationToken cancellationToken)
    {
        CustomerContact? contact = await customerService.UpdateContactAsync(customerId, contactId, ToModel(request), cancellationToken);
        return contact is null ? NotFound() : Ok(Map(contact));
    }

    [HttpDelete("{customerId:long}/contacts/{contactId:long}")]
    public async Task<IActionResult> DeleteContact(long customerId, long contactId, CancellationToken cancellationToken) =>
        await customerService.DeleteContactAsync(customerId, contactId, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{customerId:long}/contact-changes")]
    public async Task<ActionResult<PagedResponse<CustomerContactChangeResponse>>> GetContactChanges(
        long customerId, [FromQuery] ContactChangeSearchRequest request, CancellationToken cancellationToken)
    {
        if (await customerService.GetAsync(customerId, cancellationToken) is null) return NotFound();
        PagedResult<CustomerContactChange> result = await customerService.SearchContactChangesAsync(
            new(request.ReviewStatus, request.Page, request.PageSize), customerId, cancellationToken);
        return Ok(new PagedResponse<CustomerContactChangeResponse>(
            result.Items.Select(CustomerContactChangeResponse.From).ToArray(), result.Page, result.PageSize, result.TotalCount));
    }

    internal static CustomerResponse Map(Customer value) => new(
        value.Id, value.AccountNumber, value.CompanyName, value.ContactPerson, value.MobileNumber,
        value.OfficePhone, value.ResidentialPhone, value.Email, value.ManagementContactPerson,
        value.ManagementMobileNumber, value.ManagementEmail, value.AddressLine1, value.AddressLine2,
        value.AddressLine3, value.City, value.District, value.State, value.PostalCode,
        value.DefaultShippingAddressLine1, value.DefaultShippingAddressLine2, value.DefaultShippingAddressLine3,
        value.Area, value.Gstin, value.TinNumber, value.CstNumber, value.PriceBand, value.CreditDays,
        value.CreditLimit, value.DefaultTransportName, value.SalespersonReference, value.IsDisabled,
        value.Contacts.OrderBy(contact => contact.Name).Select(Map).ToArray(),
        value.LegacyAccountingSnapshot is null ? null : new(
            value.LegacyAccountingSnapshot.DueBalance, value.LegacyAccountingSnapshot.OpeningBalance,
            value.LegacyAccountingSnapshot.BlockLevel, value.LegacyAccountingSnapshot.PreviousBlockLevel,
            value.LegacyAccountingSnapshot.CapturedAtUtc, value.LegacyAccountingSnapshot.LegacyUpdatedAtUtc),
        value.CreatedAtUtc, value.UpdatedAtUtc);

    private static CustomerSummaryResponse MapSummary(Customer value) => new(
        value.Id, value.AccountNumber, value.CompanyName, value.ContactPerson, value.MobileNumber,
        value.ManagementMobileNumber, value.Area, value.PriceBand, value.IsDisabled,
        value.CreatedAtUtc, value.UpdatedAtUtc);
    private static CustomerContactResponse Map(CustomerContact value) => new(
        value.Id, value.CustomerId, value.Name, value.Designation, value.Mobile, value.Email,
        value.CreatedAtUtc, value.UpdatedAtUtc);
    private static CustomerContactWriteModel ToModel(CustomerContactWriteRequest value) =>
        new(value.Name, value.Designation, value.Mobile, value.Email);
    private static CustomerWriteModel ToModel(CustomerWriteRequest value) => new(
        value.CompanyName, value.ContactPerson, value.MobileNumber, value.OfficePhone, value.ResidentialPhone,
        value.Email, value.ManagementContactPerson, value.ManagementMobileNumber, value.ManagementEmail,
        value.AddressLine1, value.AddressLine2, value.AddressLine3, value.City, value.District, value.State,
        value.PostalCode, value.DefaultShippingAddressLine1, value.DefaultShippingAddressLine2,
        value.DefaultShippingAddressLine3, value.Area, value.Gstin, value.TinNumber, value.CstNumber,
        value.PriceBand, value.CreditDays, value.CreditLimit, value.DefaultTransportName,
        value.SalespersonReference, value.IsDisabled);
}
