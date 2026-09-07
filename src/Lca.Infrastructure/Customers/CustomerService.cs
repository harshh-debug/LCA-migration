using System.Text.Json;
using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Security;
using Lca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Customers;

public sealed class CustomerService(
    LcaDbContext context,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICustomerService
{
    public async Task<PagedResult<Customer>> SearchAsync(CustomerSearch search, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = context.Customers.AsNoTracking();
        query = search.Status switch
        {
            CustomerStatusFilter.Active => query.Where(value => !value.IsDisabled),
            CustomerStatusFilter.Inactive => query.Where(value => value.IsDisabled),
            _ => query,
        };
        if (search.PriceBand.HasValue) query = query.Where(value => value.PriceBand == search.PriceBand);
        if (!string.IsNullOrWhiteSpace(search.Area)) query = query.Where(value => value.Area == search.Area.Trim());
        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            string pattern = $"%{EscapeLikePattern(search.Search.Trim())}%";
            query = query.Where(value =>
                EF.Functions.Like(value.AccountNumber, pattern, "\\")
                || EF.Functions.Like(value.CompanyName, pattern, "\\")
                || (value.ContactPerson != null && EF.Functions.Like(value.ContactPerson, pattern, "\\"))
                || (value.MobileNumber != null && EF.Functions.Like(value.MobileNumber, pattern, "\\"))
                || (value.ManagementMobileNumber != null && EF.Functions.Like(value.ManagementMobileNumber, pattern, "\\"))
                // Useful target enhancement: legacy Customer search did not query Mobile_Contact.
                || value.Contacts.Any(contact => EF.Functions.Like(contact.Name, pattern, "\\")
                    || (contact.Mobile != null && EF.Functions.Like(contact.Mobile, pattern, "\\"))));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        Customer[] customers = await query.OrderBy(value => value.AccountNumber)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .ToArrayAsync(cancellationToken);
        return new(customers, search.Page, search.PageSize, totalCount);
    }

    public Task<Customer?> GetAsync(long id, CancellationToken cancellationToken) => context.Customers.AsNoTracking()
        .Include(value => value.Contacts)
        .Include(value => value.LegacyAccountingSnapshot)
        .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);

    public async Task<Customer> CreateAsync(CustomerCreateModel model, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        Customer customer = new()
        {
            AccountNumber = model.AccountNumber.Trim(),
            CompanyName = model.Customer.CompanyName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        Apply(customer, model.Customer);
        context.Customers.Add(customer);
        await SaveAsync("A Customer with this Account number already exists.", cancellationToken);
        return (await GetAsync(customer.Id, cancellationToken))!;
    }

    public async Task<Customer?> UpdateAsync(long id, CustomerWriteModel model, CancellationToken cancellationToken)
    {
        Customer? customer = await context.Customers.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (customer is null) return null;
        Apply(customer, model);
        customer.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await SaveAsync("The Customer update conflicts with existing data.", cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        Customer? customer = await context.Customers
            .Include(value => value.Contacts)
            .Include(value => value.ContactChanges)
            .Include(value => value.LegacyAccountingSnapshot)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (customer is null) return false;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Customers.Remove(customer);
        await SaveAsync("This Customer is referenced by protected business history and cannot be deleted.", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<CustomerContact?> CreateContactAsync(long customerId, CustomerContactWriteModel model, CancellationToken cancellationToken)
    {
        if (!await context.Customers.AnyAsync(value => value.Id == customerId, cancellationToken)) return null;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        CustomerContact contact = new() { CustomerId = customerId, Name = model.Name.Trim(), CreatedAtUtc = now, UpdatedAtUtc = now };
        Apply(contact, model);
        context.CustomerContacts.Add(contact);
        await context.SaveChangesAsync(cancellationToken);
        context.CustomerContactChanges.Add(Change(contact, ContactChangeOperation.Created, null, Snapshot(contact), now));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return contact;
    }

    public async Task<CustomerContact?> UpdateContactAsync(long customerId, long contactId, CustomerContactWriteModel model, CancellationToken cancellationToken)
    {
        CustomerContact? contact = await context.CustomerContacts.SingleOrDefaultAsync(
            value => value.Id == contactId && value.CustomerId == customerId, cancellationToken);
        if (contact is null) return null;
        CustomerContactSnapshot before = Snapshot(contact);
        Apply(contact, model);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        contact.UpdatedAtUtc = now;
        context.CustomerContactChanges.Add(Change(contact, ContactChangeOperation.Updated, before, Snapshot(contact), now));
        await context.SaveChangesAsync(cancellationToken);
        return contact;
    }

    public async Task<bool> DeleteContactAsync(long customerId, long contactId, CancellationToken cancellationToken)
    {
        CustomerContact? contact = await context.CustomerContacts.SingleOrDefaultAsync(
            value => value.Id == contactId && value.CustomerId == customerId, cancellationToken);
        if (contact is null) return false;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        CustomerContactChange deleted = Change(contact, ContactChangeOperation.Deleted, Snapshot(contact), null, now);
        deleted.ReviewStatus = ContactChangeReviewStatus.ContactDeleted;
        CustomerContactChange[] pending = await context.CustomerContactChanges
            .Where(value => value.CustomerContactId == contactId && value.ReviewStatus == ContactChangeReviewStatus.Pending)
            .ToArrayAsync(cancellationToken);
        foreach (CustomerContactChange change in pending)
        {
            change.ReviewStatus = ContactChangeReviewStatus.ContactDeleted;
            change.ReviewedAtUtc = now;
            change.ReviewedByUserId = currentUser.UserId;
        }
        context.CustomerContactChanges.Add(deleted);
        context.CustomerContacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PagedResult<CustomerContactChange>> SearchContactChangesAsync(
        ContactChangeSearch search, long? customerId, CancellationToken cancellationToken)
    {
        IQueryable<CustomerContactChange> query = context.CustomerContactChanges.AsNoTracking();
        if (customerId.HasValue)
        {
            if (!await context.Customers.AnyAsync(value => value.Id == customerId, cancellationToken))
                return new([], search.Page, search.PageSize, 0);
            query = query.Where(value => value.CustomerId == customerId.Value);
        }
        if (search.ReviewStatus.HasValue) query = query.Where(value => value.ReviewStatus == search.ReviewStatus);
        int totalCount = await query.CountAsync(cancellationToken);
        CustomerContactChange[] rows = await query.OrderByDescending(value => value.ChangedAtUtc)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).ToArrayAsync(cancellationToken);
        return new(rows, search.Page, search.PageSize, totalCount);
    }

    public async Task<CustomerContactChange?> AcknowledgeContactChangeAsync(long id, CancellationToken cancellationToken)
    {
        CustomerContactChange? change = await context.CustomerContactChanges.SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (change is null) return null;
        if (change.ReviewStatus == ContactChangeReviewStatus.Pending)
        {
            change.ReviewStatus = ContactChangeReviewStatus.Acknowledged;
            change.ReviewedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            change.ReviewedByUserId = currentUser.UserId;
            await context.SaveChangesAsync(cancellationToken);
        }
        return change;
    }

    private CustomerContactChange Change(
        CustomerContact contact, ContactChangeOperation operation,
        CustomerContactSnapshot? before, CustomerContactSnapshot? after, DateTime now) => new()
        {
            CustomerId = contact.CustomerId,
            CustomerContactId = contact.Id,
            Operation = operation,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            IsMobileChanged = before is not null && after is not null
                && !string.Equals(before.Mobile, after.Mobile, StringComparison.Ordinal),
            ChangedByUserId = currentUser.UserId,
            ChangedAtUtc = now,
            Source = "User",
        };

    private async Task SaveAsync(string message, CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) { throw new CustomerConflictException(message, exception); }
    }

    private static void Apply(Customer customer, CustomerWriteModel model)
    {
        customer.CompanyName = model.CompanyName.Trim();
        customer.ContactPerson = Clean(model.ContactPerson); customer.MobileNumber = Clean(model.MobileNumber);
        customer.OfficePhone = Clean(model.OfficePhone); customer.ResidentialPhone = Clean(model.ResidentialPhone); customer.Email = Clean(model.Email);
        customer.ManagementContactPerson = Clean(model.ManagementContactPerson); customer.ManagementMobileNumber = Clean(model.ManagementMobileNumber);
        customer.ManagementEmail = Clean(model.ManagementEmail); customer.AddressLine1 = Clean(model.AddressLine1);
        customer.AddressLine2 = Clean(model.AddressLine2); customer.AddressLine3 = Clean(model.AddressLine3); customer.City = Clean(model.City);
        customer.District = Clean(model.District); customer.State = Clean(model.State); customer.PostalCode = Clean(model.PostalCode);
        customer.DefaultShippingAddressLine1 = Clean(model.DefaultShippingAddressLine1);
        customer.DefaultShippingAddressLine2 = Clean(model.DefaultShippingAddressLine2);
        customer.DefaultShippingAddressLine3 = Clean(model.DefaultShippingAddressLine3); customer.Area = Clean(model.Area);
        customer.Gstin = Clean(model.Gstin); customer.TinNumber = Clean(model.TinNumber); customer.CstNumber = Clean(model.CstNumber);
        customer.PriceBand = model.PriceBand; customer.CreditDays = model.CreditDays; customer.CreditLimit = model.CreditLimit;
        customer.DefaultTransportName = Clean(model.DefaultTransportName); customer.SalespersonReference = Clean(model.SalespersonReference);
        customer.IsDisabled = model.IsDisabled;
    }

    private static void Apply(CustomerContact contact, CustomerContactWriteModel model)
    {
        contact.Name = model.Name.Trim(); contact.Designation = Clean(model.Designation);
        contact.Mobile = Clean(model.Mobile); contact.Email = Clean(model.Email);
    }

    private static CustomerContactSnapshot Snapshot(CustomerContact contact) =>
        new(contact.Name, contact.Designation, contact.Mobile, contact.Email);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal).Replace("[", "\\[", StringComparison.Ordinal);
}
