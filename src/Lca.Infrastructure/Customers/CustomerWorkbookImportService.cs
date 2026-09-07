using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Security;
using Lca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Customers;

public sealed class CustomerWorkbookImportService(
    LcaDbContext context,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICustomerWorkbookImportService
{
    private const string SheetName = "CUSTOMER";

    static CustomerWorkbookImportService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public Task<CustomerWorkbookImportPreview> PreviewAsync(Stream workbook, CancellationToken cancellationToken)
    {
        ParsedWorkbook parsed = ParseSafe(workbook, cancellationToken);
        return Task.FromResult(new CustomerWorkbookImportPreview(parsed.RowsRead, parsed.Rows.Count, parsed.Issues));
    }

    public async Task<CustomerWorkbookImportResult> ApplySnapshotAsync(
        Stream workbook, bool confirmSnapshot, CancellationToken cancellationToken)
    {
        if (!confirmSnapshot) throw new CustomerConflictException("Customer snapshot replacement must be explicitly confirmed.");
        ParsedWorkbook parsed = ParseSafe(workbook, cancellationToken);
        if (parsed.Issues.Count > 0) return new(parsed.RowsRead, 0, 0, 0, 0, 0, 0, parsed.Issues);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        Customer[] existing = await context.Customers
            .Include(value => value.Contacts)
            .Include(value => value.LegacyAccountingSnapshot)
            .ToArrayAsync(cancellationToken);
        LegacyCustomerContactMap[] existingContactMaps = await context.LegacyCustomerContactMaps.ToArrayAsync(cancellationToken);
        Dictionary<string, Customer> byAccount = existing.ToDictionary(value => value.AccountNumber, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, LegacyCustomerContactMap> contactMaps = existingContactMaps.ToDictionary(
            value => value.SourceFingerprint, StringComparer.OrdinalIgnoreCase);
        Dictionary<long, CustomerContact> contactsById = existing.SelectMany(value => value.Contacts).ToDictionary(value => value.Id);
        HashSet<string> importedAccounts = new(StringComparer.OrdinalIgnoreCase);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int created = 0, updated = 0, reactivated = 0, disabled = 0, contactsCreated = 0, contactsUpdated = 0;

        foreach (ParsedCustomer row in parsed.Rows)
        {
            importedAccounts.Add(row.AccountNumber);
            if (!byAccount.TryGetValue(row.AccountNumber, out Customer? customer))
            {
                customer = new Customer
                {
                    AccountNumber = row.AccountNumber,
                    CompanyName = row.CompanyName,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };
                context.Customers.Add(customer);
                byAccount.Add(row.AccountNumber, customer);
                created++;
            }
            else
            {
                updated++;
                if (customer.IsDisabled) reactivated++;
            }

            Apply(customer, row, now);
            customer.LegacyAccountingSnapshot ??= new LegacyCustomerAccountingSnapshot { CapturedAtUtc = now };
            customer.LegacyAccountingSnapshot.DueBalance = row.DueBalance;
            customer.LegacyAccountingSnapshot.OpeningBalance = row.OpeningBalance;
            customer.LegacyAccountingSnapshot.CapturedAtUtc = now;

            await UpsertAdditionalContact(customer, row.AccountNumber, "additional", row.AdditionalContactName,
                row.AdditionalMobile, row.AdditionalEmail, now);
            await UpsertAdditionalContact(customer, row.AccountNumber, "purchase", row.PurchaseContactName,
                row.PurchaseMobile, row.PurchaseEmail, now);
        }

        foreach (Customer customer in existing.Where(value => !importedAccounts.Contains(value.AccountNumber) && !value.IsDisabled))
        {
            customer.IsDisabled = true;
            customer.UpdatedAtUtc = now;
            disabled++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(parsed.RowsRead, created, updated, reactivated, disabled, contactsCreated, contactsUpdated, []);

        async Task UpsertAdditionalContact(
            Customer customer, string account, string slot, string? name, string? mobile, string? email, DateTime timestamp)
        {
            if (name is null && mobile is null && email is null) return;
            string fingerprint = Fingerprint($"customer-workbook|{account.ToUpperInvariant()}|{slot}");
            CustomerContact? contact = null;
            if (contactMaps.TryGetValue(fingerprint, out LegacyCustomerContactMap? map))
                contactsById.TryGetValue(map.CustomerContactId, out contact);

            if (contact is null)
            {
                contact = new CustomerContact
                {
                    Customer = customer,
                    Name = name ?? slot,
                    Mobile = mobile,
                    Email = email,
                    CreatedAtUtc = timestamp,
                    UpdatedAtUtc = timestamp,
                };
                context.CustomerContacts.Add(contact);
                await context.SaveChangesAsync(cancellationToken);
                map = new LegacyCustomerContactMap { SourceFingerprint = fingerprint, CustomerContactId = contact.Id };
                context.LegacyCustomerContactMaps.Add(map);
                context.CustomerContactChanges.Add(ContactChange(
                    contact, ContactChangeOperation.Created, null, Snapshot(contact), timestamp));
                contactMaps[fingerprint] = map;
                contactsById[contact.Id] = contact;
                contactsCreated++;
            }
            else
            {
                CustomerContactSnapshot before = Snapshot(contact);
                contact.Name = name ?? contact.Name;
                contact.Mobile = mobile;
                contact.Email = email;
                CustomerContactSnapshot after = Snapshot(contact);
                if (before == after) return;
                contact.UpdatedAtUtc = timestamp;
                context.CustomerContactChanges.Add(ContactChange(
                    contact, ContactChangeOperation.Updated, before, after, timestamp));
                contactsUpdated++;
            }
        }

        CustomerContactChange ContactChange(
            CustomerContact contact, ContactChangeOperation operation,
            CustomerContactSnapshot? before, CustomerContactSnapshot after, DateTime timestamp) => new()
            {
                CustomerId = contact.CustomerId,
                CustomerContactId = contact.Id,
                Operation = operation,
                BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
                AfterJson = JsonSerializer.Serialize(after),
                IsMobileChanged = before is not null && !string.Equals(before.Mobile, after.Mobile, StringComparison.Ordinal),
                ChangedByUserId = currentUser.UserId,
                ChangedAtUtc = timestamp,
                Source = "CustomerWorkbook",
            };
    }

    private static ParsedWorkbook Parse(Stream workbook, CancellationToken cancellationToken)
    {
        using IExcelDataReader reader = ExcelReaderFactory.CreateReader(workbook);
        do
        {
            if (!string.Equals(reader.Name?.Trim(), SheetName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!reader.Read()) return new(0, [], [new(1, SheetName, $"The {SheetName} sheet has no header row.")]);
            Dictionary<string, int> headers = Enumerable.Range(0, reader.FieldCount)
                .Select(index => (Name: Text(reader.GetValue(index)), Index: index))
                .Where(value => value.Name is not null)
                .ToDictionary(value => value.Name!, value => value.Index, StringComparer.OrdinalIgnoreCase);
            List<WorkbookImportIssue> issues = [];
            foreach (string required in new[] { "Account_no", "Name" })
                if (!headers.ContainsKey(required)) issues.Add(new(1, required, "Required column is missing."));
            if (issues.Count > 0) return new(0, [], issues);

            List<ParsedCustomer> rows = [];
            HashSet<string> accounts = new(StringComparer.OrdinalIgnoreCase);
            int rowNumber = 1;
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowNumber++;
                string? account = Cell("Account_no");
                string? company = Cell("Name");
                if (account is null && company is null) continue;
                int before = issues.Count;
                if (account is null) issues.Add(new(rowNumber, "Account_no", "Account number is required."));
                else if (account.Length > 50) issues.Add(new(rowNumber, "Account_no", "Account number exceeds 50 characters."));
                else if (!accounts.Add(account)) issues.Add(new(rowNumber, "Account_no", "Duplicate Account number in workbook."));
                if (company is null) issues.Add(new(rowNumber, "Name", "Customer name is required."));
                else CheckLength("Name", company, 500);

                decimal? due = Number("Dueamount", allowNegative: true);
                decimal? opening = Number("Opbalance", allowNegative: true);
                decimal? limit = Number("Crlimitrs", allowNegative: false);
                int? days = Whole("Crlimitdays");
                CustomerPriceBand? band = PriceBand(Cell("Pricelist"));
                if (!band.HasValue) issues.Add(new(rowNumber, "Pricelist", "Expected p, d, w, r, o, or blank."));

                CheckAllLengths();
                if (issues.Count == before && account is not null && company is not null && band.HasValue)
                {
                    rows.Add(new(account, company, Cell("Contect"), Cell("Mobile"), Cell("Phone_o"), Cell("Phone_r"), Cell("Email"),
                        Cell("M_contect"), Cell("M_mobile"), Cell("M_email"), Cell("Add1"), Cell("Add2"), Cell("Add3"),
                        Cell("City"), Cell("State"), Cell("Pin"), Cell("Area"), Cell("Gsttinno"), Cell("Tinno"), Cell("Cstno"),
                        band.Value, days, limit, Cell("Transport"), Cell("Salesman"), due, opening,
                        Cell("A_contect"), Cell("A_mobile"), Cell("A_email"), Cell("P_contect"), Cell("P_mobile"), Cell("P_email")));
                }

                decimal? Number(string column, bool allowNegative)
                {
                    string? raw = Cell(column);
                    if (raw is null) return null;
                    if (!(decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal value)
                        || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)))
                    { issues.Add(new(rowNumber, column, $"'{raw}' is not a valid number.")); return null; }
                    if (!allowNegative && value < 0) { issues.Add(new(rowNumber, column, "Value cannot be negative.")); return null; }
                    return value;
                }

                int? Whole(string column)
                {
                    decimal? value = Number(column, allowNegative: false);
                    if (!value.HasValue) return null;
                    if (value.Value != decimal.Truncate(value.Value) || value > int.MaxValue)
                    { issues.Add(new(rowNumber, column, "Expected a nonnegative whole number.")); return null; }
                    return (int)value.Value;
                }

                void CheckAllLengths()
                {
                    foreach (string column in new[] { "Contect", "Mobile", "Phone_o", "Phone_r", "Email", "M_contect", "M_mobile", "M_email",
                                 "City", "State", "Pin", "Area", "Gsttinno", "Tinno", "Cstno", "Transport", "Salesman",
                                 "A_contect", "A_mobile", "A_email", "P_contect", "P_mobile", "P_email" }) CheckLength(column, Cell(column), 200);
                    foreach (string column in new[] { "Add1", "Add2", "Add3" }) CheckLength(column, Cell(column), 1000);
                }

                void CheckLength(string field, string? value, int maximum)
                {
                    if (value?.Length > maximum) issues.Add(new(rowNumber, field, $"Value exceeds {maximum} characters."));
                }
            }
            return new(rowNumber - 1, rows, issues);

            string? Cell(string name) => headers.TryGetValue(name, out int index) ? Text(reader.GetValue(index)) : null;
        } while (reader.NextResult());
        return new(0, [], [new(0, SheetName, $"Required {SheetName} sheet was not found.")]);
    }

    private static ParsedWorkbook ParseSafe(Stream workbook, CancellationToken cancellationToken)
    {
        try { return Parse(workbook, cancellationToken); }
        catch (HeaderException exception) { return InvalidWorkbook(exception); }
        catch (InvalidDataException exception) { return InvalidWorkbook(exception); }
    }

    private static ParsedWorkbook InvalidWorkbook(Exception exception) =>
        new(0, [], [new(0, "Workbook", $"The file is not a readable Excel workbook: {exception.Message}")]);

    private static CustomerPriceBand? PriceBand(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        null or "" => CustomerPriceBand.Other,
        "p" => CustomerPriceBand.Purchase,
        "d" => CustomerPriceBand.Dealer,
        "w" => CustomerPriceBand.Wholesale,
        "r" => CustomerPriceBand.Retail,
        "o" => CustomerPriceBand.Other,
        _ => null,
    };

    private static void Apply(Customer customer, ParsedCustomer row, DateTime now)
    {
        customer.CompanyName = row.CompanyName;
        customer.ContactPerson = row.ContactPerson; customer.MobileNumber = row.MobileNumber;
        customer.OfficePhone = row.OfficePhone; customer.ResidentialPhone = row.ResidentialPhone; customer.Email = row.Email;
        customer.ManagementContactPerson = row.ManagementContactPerson; customer.ManagementMobileNumber = row.ManagementMobileNumber;
        customer.ManagementEmail = row.ManagementEmail; customer.AddressLine1 = row.AddressLine1; customer.AddressLine2 = row.AddressLine2;
        customer.AddressLine3 = row.AddressLine3; customer.City = row.City; customer.State = row.State; customer.PostalCode = row.PostalCode;
        customer.Area = row.Area; customer.Gstin = row.Gstin; customer.TinNumber = row.TinNumber; customer.CstNumber = row.CstNumber;
        customer.PriceBand = row.PriceBand; customer.CreditDays = row.CreditDays; customer.CreditLimit = row.CreditLimit;
        customer.DefaultTransportName = row.Transport; customer.SalespersonReference = row.Salesperson;
        customer.IsDisabled = false; customer.UpdatedAtUtc = now;
    }

    private static string? Text(object? value)
    {
        string? text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static CustomerContactSnapshot Snapshot(CustomerContact contact) =>
        new(contact.Name, contact.Designation, contact.Mobile, contact.Email);

    private sealed record ParsedWorkbook(int RowsRead, List<ParsedCustomer> Rows, List<WorkbookImportIssue> Issues);
    private sealed record ParsedCustomer(
        string AccountNumber, string CompanyName, string? ContactPerson, string? MobileNumber, string? OfficePhone,
        string? ResidentialPhone, string? Email, string? ManagementContactPerson, string? ManagementMobileNumber,
        string? ManagementEmail, string? AddressLine1, string? AddressLine2, string? AddressLine3, string? City,
        string? State, string? PostalCode, string? Area, string? Gstin, string? TinNumber, string? CstNumber,
        CustomerPriceBand PriceBand, int? CreditDays, decimal? CreditLimit, string? Transport, string? Salesperson,
        decimal? DueBalance, decimal? OpeningBalance, string? AdditionalContactName, string? AdditionalMobile,
        string? AdditionalEmail, string? PurchaseContactName, string? PurchaseMobile, string? PurchaseEmail);
}
