using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lca.Core.Customers;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

const long TenantOneId = 1;
if (args.Any(argument => !string.Equals(argument, "--apply", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Only --apply is supported. This importer is permanently fixed to Tenant 1.");
bool apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
string legacyConnection = Environment.GetEnvironmentVariable("LEGACY_LCA_CONNECTION")
    ?? throw new InvalidOperationException("LEGACY_LCA_CONNECTION is required.");
string targetConnection = Environment.GetEnvironmentVariable("LCA_MIGRATION_CONNECTION")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__LcaDatabase")
    ?? throw new InvalidOperationException("LCA_MIGRATION_CONNECTION is required.");

await using SqlConnection legacy = new(legacyConnection);
await legacy.OpenAsync();
List<LegacyCustomer> sourceCustomers = await ReadCustomersAsync(legacy);
List<LegacyContact> sourceContacts = await ReadContactsAsync(legacy);
List<LegacyContactChange> sourceChanges = await ReadContactChangesAsync(legacy);

List<RejectedRow> rejected = [];
HashSet<decimal> invalidCustomerIds = [];
foreach (IGrouping<string, LegacyCustomer> duplicate in sourceCustomers
             .Where(value => !string.IsNullOrWhiteSpace(value.AccountNumber))
             .GroupBy(value => value.AccountNumber!, StringComparer.OrdinalIgnoreCase).Where(value => value.Count() > 1))
{
    foreach (LegacyCustomer customer in duplicate) invalidCustomerIds.Add(customer.Id);
    rejected.Add(new("Customer", duplicate.Key, "Duplicate Account number in legacy source."));
}
foreach (LegacyCustomer customer in sourceCustomers)
{
    if (string.IsNullOrWhiteSpace(customer.AccountNumber)) Reject(customer, "Account number is required.");
    if (string.IsNullOrWhiteSpace(customer.CompanyName)) Reject(customer, "Company name is required.");
    if (customer.AccountNumber?.Length > 50) Reject(customer, "Account number exceeds 50 characters.");
    if (customer.CompanyName?.Length > 500) Reject(customer, "Company name exceeds 500 characters.");
    if (!TryPriceBand(customer.PriceList, out _)) Reject(customer, $"Unknown PriceList code '{customer.PriceList}'.");
    if (!TryWhole(customer.CreditDays, out _, nonnegative: true)) Reject(customer, $"CreditDays is invalid: '{customer.CreditDays}'.");
    if (!TryNumber(customer.CreditLimit, out _, nonnegative: true)) Reject(customer, $"CreditLimit is invalid: '{customer.CreditLimit}'.");
    if (!TryNumber(customer.DueAmount, out _, nonnegative: false)) Reject(customer, $"DueAmount is invalid: '{customer.DueAmount}'.");
    if (!TryNumber(customer.OpeningBalance, out _, nonnegative: false)) Reject(customer, $"OpeningBalance is invalid: '{customer.OpeningBalance}'.");
    CheckLength(customer.ContactPerson, 200, "ContactPerson"); CheckLength(customer.Address, 1000, "Address");
    CheckLength(customer.City, 200, "City"); CheckLength(customer.District, 200, "District"); CheckLength(customer.State, 200, "State");
    CheckLength(customer.ShippingAddress1, 1000, "Transport1"); CheckLength(customer.ShippingAddress2, 1000, "Transport2");
    CheckLength(customer.ShippingAddress3, 1000, "Transport3"); CheckLength(customer.Address2, 1000, "Add2");
    CheckLength(customer.Address3, 1000, "Add3"); CheckLength(customer.TinNumberAlternate, 200, "Tinno");
    CheckLength(customer.BlockLevel, 20, "IsBlock"); CheckLength(customer.PreviousBlockLevel, 20, "PreviousStatus");

    void CheckLength(string? text, int maximum, string field)
    {
        if (text?.Length > maximum) Reject(customer, $"{field} exceeds {maximum} characters.");
    }

    void Reject(LegacyCustomer value, string reason)
    {
        invalidCustomerIds.Add(value.Id);
        rejected.Add(new("Customer", value.Id.ToString(CultureInfo.InvariantCulture), reason));
    }
}

Dictionary<string, LegacyCustomer> sourceByAccount = sourceCustomers
    .Where(value => !invalidCustomerIds.Contains(value.Id) && value.AccountNumber is not null)
    .ToDictionary(value => value.AccountNumber!, StringComparer.OrdinalIgnoreCase);
foreach (LegacyContact contact in sourceContacts.Where(value => value.AccountNumber is null || !sourceByAccount.ContainsKey(value.AccountNumber)))
    rejected.Add(new("Contact", contact.AccountNumber ?? "(blank)", "Orphan contact: Account number has no valid legacy Customer."));
foreach (LegacyContactChange change in sourceChanges.Where(value => value.AccountNumber is null || !sourceByAccount.ContainsKey(value.AccountNumber)))
    rejected.Add(new("ContactChange", change.AccountNumber ?? "(blank)", "Orphan review row: Account number has no valid legacy Customer."));

DbContextOptions<LcaDbContext> options = new DbContextOptionsBuilder<LcaDbContext>().UseSqlServer(targetConnection).Options;
await using LcaDbContext target = new(options, new TenantOneContext());
Tenant? tenantOne = await target.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(value => value.Id == TenantOneId);
if (tenantOne is null || tenantOne.Status != TenantStatus.Active)
    throw new InvalidOperationException("Canonical active Tenant 1 does not exist; legacy Customer import is refused.");

Dictionary<decimal, LegacyCustomerMap> existingMaps = await target.LegacyCustomerMaps
    .Include(value => value.Customer).ThenInclude(value => value.LegacyAccountingSnapshot)
    .ToDictionaryAsync(value => value.LegacyCustomerId);
Dictionary<string, Customer> targetByAccount = (await target.Customers.ToArrayAsync())
    .ToDictionary(value => value.AccountNumber, StringComparer.OrdinalIgnoreCase);
foreach (LegacyCustomer source in sourceCustomers.Where(value => !invalidCustomerIds.Contains(value.Id)))
{
    if (existingMaps.TryGetValue(source.Id, out LegacyCustomerMap? map))
    {
        if (!string.Equals(map.AccountNumber, source.AccountNumber, StringComparison.OrdinalIgnoreCase))
        {
            invalidCustomerIds.Add(source.Id);
            rejected.Add(new("Customer", source.Id.ToString(CultureInfo.InvariantCulture),
                $"Mapped immutable Account number is '{map.AccountNumber}', source now contains '{source.AccountNumber}'."));
        }
    }
    else if (source.AccountNumber is not null && targetByAccount.ContainsKey(source.AccountNumber))
    {
        invalidCustomerIds.Add(source.Id);
        rejected.Add(new("Customer", source.AccountNumber,
            "An unmapped target Customer already uses this Account number; automatic adoption is refused."));
    }
}

int created = 0, updated = 0, contactsCreated = 0, changesCreated = 0;
if (apply)
{
    await using var transaction = await target.Database.BeginTransactionAsync();
    DateTime now = DateTime.UtcNow;
    foreach (LegacyCustomer source in sourceCustomers.Where(value => !invalidCustomerIds.Contains(value.Id)))
    {
        Customer customer;
        if (!existingMaps.TryGetValue(source.Id, out LegacyCustomerMap? map))
        {
            customer = new Customer
            {
                AccountNumber = source.AccountNumber!, CompanyName = source.CompanyName!, CreatedAtUtc = now, UpdatedAtUtc = now,
            };
            map = new LegacyCustomerMap { LegacyCustomerId = source.Id, AccountNumber = source.AccountNumber!, Customer = customer };
            target.LegacyCustomerMaps.Add(map);
            existingMaps.Add(source.Id, map);
            created++;
        }
        else { customer = map.Customer; updated++; }
        ApplyCustomer(customer, source, now);
        customer.LegacyAccountingSnapshot ??= new LegacyCustomerAccountingSnapshot();
        customer.LegacyAccountingSnapshot.DueBalance = Number(source.DueAmount);
        customer.LegacyAccountingSnapshot.OpeningBalance = Number(source.OpeningBalance);
        customer.LegacyAccountingSnapshot.BlockLevel = source.BlockLevel;
        customer.LegacyAccountingSnapshot.PreviousBlockLevel = source.PreviousBlockLevel;
        customer.LegacyAccountingSnapshot.CapturedAtUtc = now;
        customer.LegacyAccountingSnapshot.LegacyUpdatedAtUtc = source.LastUpdate;
    }
    await target.SaveChangesAsync();

    Dictionary<string, Customer> customers = existingMaps.Values.Where(value => !invalidCustomerIds.Contains(value.LegacyCustomerId))
        .ToDictionary(value => value.AccountNumber, value => value.Customer, StringComparer.OrdinalIgnoreCase);
    Dictionary<string, LegacyCustomerContactMap> contactMaps = await target.LegacyCustomerContactMaps
        .ToDictionaryAsync(value => value.SourceFingerprint);
    Dictionary<long, string> accountByCustomerId = (await target.Customers.ToArrayAsync())
        .ToDictionary(value => value.Id, value => value.AccountNumber);
    Dictionary<string, CustomerContact> contactsByNaturalKey = (await target.CustomerContacts.ToArrayAsync())
        .GroupBy(value => ContactKey(accountByCustomerId[value.CustomerId], value.Name, value.Designation, value.Mobile, value.Email),
            StringComparer.OrdinalIgnoreCase)
        .ToDictionary(value => value.Key, value => value.First(), StringComparer.OrdinalIgnoreCase);

    List<LegacyContact> allContacts = [];
    allContacts.AddRange(sourceCustomers.Where(value => !invalidCustomerIds.Contains(value.Id)).SelectMany(FixedContacts));
    allContacts.AddRange(sourceContacts.Where(value => value.AccountNumber is not null && customers.ContainsKey(value.AccountNumber)));
    foreach (LegacyContact source in allContacts)
    {
        string fingerprint = Fingerprint(source.SourceIdentity);
        if (contactMaps.ContainsKey(fingerprint)) continue;
        Customer customer = customers[source.AccountNumber!];
        string naturalKey = ContactKey(source.AccountNumber!, source.Name, source.Designation, source.Mobile, source.Email);
        if (!contactsByNaturalKey.TryGetValue(naturalKey, out CustomerContact? contact))
        {
            contact = new CustomerContact
            {
                Customer = customer,
                Name = source.Name ?? "Unnamed contact",
                Designation = source.Designation,
                Mobile = source.Mobile,
                Email = source.Email,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            target.CustomerContacts.Add(contact);
            await target.SaveChangesAsync();
            contactsByNaturalKey[naturalKey] = contact;
            contactsCreated++;
        }
        target.LegacyCustomerContactMaps.Add(new LegacyCustomerContactMap
        {
            SourceFingerprint = fingerprint,
            CustomerContactId = contact.Id,
        });
    }
    await target.SaveChangesAsync();

    Dictionary<string, LegacyCustomerContactChangeMap> changeMaps = await target.LegacyCustomerContactChangeMaps
        .ToDictionaryAsync(value => value.SourceFingerprint);
    foreach (LegacyContactChange source in sourceChanges.Where(value => value.AccountNumber is not null && customers.ContainsKey(value.AccountNumber)))
    {
        string fingerprint = Fingerprint(source.SourceIdentity);
        if (changeMaps.ContainsKey(fingerprint)) continue;
        Customer customer = customers[source.AccountNumber!];
        string naturalKey = ContactKey(source.AccountNumber!, source.Name, source.Designation, source.Mobile, source.Email);
        contactsByNaturalKey.TryGetValue(naturalKey, out CustomerContact? contact);
        CustomerContactSnapshot snapshot = new(source.Name ?? "Unnamed contact", source.Designation, source.Mobile, source.Email);
        CustomerContactChange change = new()
        {
            Customer = customer,
            CustomerContactId = contact?.Id,
            Operation = ContactChangeOperation.LegacyImported,
            BeforeJson = null,
            AfterJson = JsonSerializer.Serialize(snapshot),
            IsMobileChanged = source.IsMobileChanged,
            ReviewStatus = source.IsUpdated ? ContactChangeReviewStatus.Pending : ContactChangeReviewStatus.Acknowledged,
            ChangedByUserId = source.UserName,
            ChangedAtUtc = source.CreateDate ?? source.UpdateDate?.ToDateTime(TimeOnly.MinValue) ?? now,
            ReviewedAtUtc = source.IsUpdated ? null : now,
            Source = "LegacyMobileContactUpdate",
        };
        target.CustomerContactChanges.Add(change);
        await target.SaveChangesAsync();
        target.LegacyCustomerContactChangeMaps.Add(new LegacyCustomerContactChangeMap
        {
            SourceFingerprint = fingerprint,
            CustomerContactChangeId = change.Id,
        });
        changesCreated++;
    }
    await target.SaveChangesAsync();
    await transaction.CommitAsync();
}

int targetCustomers = await target.Customers.CountAsync();
int mappedCustomers = await target.LegacyCustomerMaps.CountAsync();
int targetContacts = await target.CustomerContacts.CountAsync();
int pendingChanges = await target.CustomerContactChanges.CountAsync(value => value.ReviewStatus == ContactChangeReviewStatus.Pending);
ImportReport report = new(apply, sourceCustomers.Count, sourceContacts.Count, sourceChanges.Count,
    targetCustomers, mappedCustomers, targetContacts, pendingChanges, created, updated, contactsCreated, changesCreated, rejected);
Console.WriteLine(JsonSerializer.Serialize(report, Serialization.Options));
Environment.ExitCode = rejected.Count == 0 ? 0 : 2;

static void ApplyCustomer(Customer target, LegacyCustomer source, DateTime now)
{
    target.CompanyName = source.CompanyName!; target.ContactPerson = source.ContactPerson; target.MobileNumber = source.MobileNumber;
    target.OfficePhone = source.LandlineNumber; target.ResidentialPhone = source.ResidentialPhone; target.Email = source.Email;
    target.ManagementContactPerson = source.ManagementContactName; target.ManagementMobileNumber = source.ManagementMobile;
    target.ManagementEmail = source.ManagementEmail; target.AddressLine1 = source.Address; target.AddressLine2 = source.Address2;
    target.AddressLine3 = source.Address3; target.City = source.City; target.District = source.District; target.State = source.State;
    target.PostalCode = source.PostalCode; target.DefaultShippingAddressLine1 = source.ShippingAddress1;
    target.DefaultShippingAddressLine2 = source.ShippingAddress2; target.DefaultShippingAddressLine3 = source.ShippingAddress3;
    target.Area = source.Area; target.Gstin = source.Gstin; target.TinNumber = source.TinNumber ?? source.TinNumberAlternate;
    target.CstNumber = source.CstNumber; TryPriceBand(source.PriceList, out CustomerPriceBand band); target.PriceBand = band;
    TryWhole(source.CreditDays, out int? days, true); target.CreditDays = days;
    TryNumber(source.CreditLimit, out decimal? limit, true); target.CreditLimit = limit;
    target.DefaultTransportName = source.Transport; target.SalespersonReference = source.Salesperson;
    target.IsDisabled = source.IsDisabled; target.UpdatedAtUtc = now;
}

static IEnumerable<LegacyContact> FixedContacts(LegacyCustomer value)
{
    (string Slot, string? Name, string? Email, string? Mobile, string? Designation)[] slots =
    [
        ("A", value.AdditionalContactName, value.AdditionalEmail, value.AdditionalMobile, value.AdditionalDesignation),
        ("P", value.PurchaseContactName, value.PurchaseEmail, value.PurchaseMobile, value.PurchaseDesignation),
        ("1", value.Contact1Name, value.Contact1Email, value.Contact1Mobile, value.Contact1Designation),
        ("2", value.Contact2Name, value.Contact2Email, value.Contact2Mobile, value.Contact2Designation),
        ("3", value.Contact3Name, value.Contact3Email, value.Contact3Mobile, value.Contact3Designation),
        ("4", value.Contact4Name, value.Contact4Email, value.Contact4Mobile, value.Contact4Designation),
        ("5", value.Contact5Name, value.Contact5Email, value.Contact5Mobile, value.Contact5Designation),
        ("6", value.Contact6Name, value.Contact6Email, value.Contact6Mobile, value.Contact6Designation),
    ];
    foreach ((string slot, string? name, string? email, string? mobile, string? designation) in slots)
        if (name is not null || email is not null || mobile is not null)
            yield return new(value.AccountNumber, value.CompanyName, name, designation, mobile, email,
                $"fixed|{value.Id.ToString(CultureInfo.InvariantCulture)}|{slot}");
}

static async Task<List<LegacyCustomer>> ReadCustomersAsync(SqlConnection connection)
{
    const string sql = """
        SELECT CustomerID, Company, ContactPerson, MobileNumber, LandlineNumber, Address, City, District, State,
               Transport1, Transport2, Transport3, LastUpdate, TinNumber, DueAmount, Pin, Area, Phone_Res, Email,
               Crlimitdays, Crlimitrs, Pricelist, Transport, Salesman, Cstno, Account_no,
               A_contect, A_email, A_mobile, P_contect, P_email, P_mobile, M_contect, M_email, M_mobile,
               Disable, Gsttinno, PreviousStatus, IsBlock, Opbalance, Add2, Add3, Tinno,
               one_contact, one_email, one_mobile, two_contact, two_email, two_mobile,
               three_contact, three_email, three_mobile, four_contact, four_email, four_mobile,
               five_contact, five_email, five_mobile, six_contact, six_email, six_mobile,
               DesignationA, DesignationP, Designation1, Designation2, Designation3, Designation4, Designation5, Designation6
        FROM dbo.Mobile_Customertbl ORDER BY CustomerID
        """;
    await using SqlCommand command = new(sql, connection); await using SqlDataReader reader = await command.ExecuteReaderAsync();
    List<LegacyCustomer> rows = [];
    while (await reader.ReadAsync()) rows.Add(new(
        reader.GetDecimal(0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5),
        Text(reader, 6), Text(reader, 7), Text(reader, 8), Text(reader, 9), Text(reader, 10), Text(reader, 11), DateTimeValue(reader, 12),
        Text(reader, 13), Text(reader, 14), Text(reader, 15), Text(reader, 16), Text(reader, 17), Text(reader, 18), Text(reader, 19),
        Text(reader, 20), Text(reader, 21), Text(reader, 22), Text(reader, 23), Text(reader, 24), Text(reader, 25),
        Text(reader, 26), Text(reader, 27), Text(reader, 28), Text(reader, 29), Text(reader, 30), Text(reader, 31),
        Text(reader, 32), Text(reader, 33), Text(reader, 34), Bool(reader, 35), Text(reader, 36), Text(reader, 37), Text(reader, 38),
        Text(reader, 39), Text(reader, 40), Text(reader, 41), Text(reader, 42),
        Text(reader, 43), Text(reader, 44), Text(reader, 45), Text(reader, 46), Text(reader, 47), Text(reader, 48),
        Text(reader, 49), Text(reader, 50), Text(reader, 51), Text(reader, 52), Text(reader, 53), Text(reader, 54),
        Text(reader, 55), Text(reader, 56), Text(reader, 57), Text(reader, 58), Text(reader, 59), Text(reader, 60),
        Text(reader, 61), Text(reader, 62), Text(reader, 63), Text(reader, 64), Text(reader, 65), Text(reader, 66),
        Text(reader, 67), Text(reader, 68)));
    return rows;
}

static async Task<List<LegacyContact>> ReadContactsAsync(SqlConnection connection)
{
    const string sql = "SELECT Account_no, Account, Name, Designation, Mobile, Email FROM dbo.Mobile_Contact ORDER BY Account_no, Name, Designation, Mobile, Email";
    await using SqlCommand command = new(sql, connection); await using SqlDataReader reader = await command.ExecuteReaderAsync();
    List<LegacyContact> rows = []; int row = 0;
    while (await reader.ReadAsync())
    {
        row++;
        rows.Add(new(Text(reader, 0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5),
            $"normalized|{row.ToString(CultureInfo.InvariantCulture)}|{ContactKey(Text(reader, 0), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5))}"));
    }
    return rows;
}

static async Task<List<LegacyContactChange>> ReadContactChangesAsync(SqlConnection connection)
{
    const string sql = "SELECT Account_no, Account, Name, Designation, Mobile, Email, IsUpdated, IsMobileChanged, CreateDate, Type, UserName, UpdateTime FROM dbo.Mobile_ContactUpdate ORDER BY Account_no, Name, Designation, Mobile, Email, CreateDate, UpdateTime";
    await using SqlCommand command = new(sql, connection); await using SqlDataReader reader = await command.ExecuteReaderAsync();
    List<LegacyContactChange> rows = []; int row = 0;
    while (await reader.ReadAsync())
    {
        row++;
        rows.Add(new(Text(reader, 0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5),
            Bool(reader, 6), Bool(reader, 7), DateTimeValue(reader, 8), Text(reader, 9), Text(reader, 10), DateOnlyValue(reader, 11),
            $"review|{row.ToString(CultureInfo.InvariantCulture)}|{ContactKey(Text(reader, 0), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5))}|{DateTimeValue(reader, 8):O}|{Text(reader, 10)}"));
    }
    return rows;
}

static string? Text(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null
    : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() is { Length: > 0 } value ? value : null;
static bool Bool(SqlDataReader reader, int ordinal) => !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
static DateTime? DateTimeValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
static DateOnly? DateOnlyValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
static string ContactKey(string? account, string? name, string? designation, string? mobile, string? email) =>
    string.Join('|', account, name, designation, mobile, email).ToUpperInvariant();
static decimal? Number(string? text) => TryNumber(text, out decimal? value, false) ? value : null;
static bool TryNumber(string? text, out decimal? value, bool nonnegative)
{
    value = null;
    if (string.IsNullOrWhiteSpace(text)) return true;
    if (!(decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal parsed)
        || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))) return false;
    if (nonnegative && parsed < 0) return false;
    value = parsed; return true;
}
static bool TryWhole(string? text, out int? value, bool nonnegative)
{
    value = null;
    if (!TryNumber(text, out decimal? number, nonnegative)) return false;
    if (!number.HasValue) return true;
    if (number != decimal.Truncate(number.Value) || number > int.MaxValue || number < int.MinValue) return false;
    value = (int)number.Value; return true;
}
static bool TryPriceBand(string? code, out CustomerPriceBand value)
{
    value = code?.Trim().ToLowerInvariant() switch
    {
        "p" => CustomerPriceBand.Purchase, "d" => CustomerPriceBand.Dealer, "w" => CustomerPriceBand.Wholesale,
        "r" => CustomerPriceBand.Retail, null or "" or "o" => CustomerPriceBand.Other, _ => (CustomerPriceBand)(-1),
    };
    return (int)value >= 0;
}

sealed class TenantOneContext : ITenantContext { public bool IsAvailable => true; public TenantId? TenantId { get; } = new(1); }
static class Serialization { public static readonly JsonSerializerOptions Options = new() { WriteIndented = true }; }
sealed record RejectedRow(string Entity, string Key, string Reason);
sealed record ImportReport(bool Applied, int SourceCustomers, int SourceContacts, int SourceContactChanges,
    int TargetCustomers, int MappedCustomers, int TargetContacts, int PendingContactChanges,
    int CustomersCreated, int CustomersUpdated, int ContactsCreated, int ContactChangesCreated,
    IReadOnlyCollection<RejectedRow> Rejected);
sealed record LegacyContact(string? AccountNumber, string? AccountName, string? Name, string? Designation,
    string? Mobile, string? Email, string SourceIdentity);
sealed record LegacyContactChange(string? AccountNumber, string? AccountName, string? Name, string? Designation,
    string? Mobile, string? Email, bool IsUpdated, bool IsMobileChanged, DateTime? CreateDate,
    string? Type, string? UserName, DateOnly? UpdateDate, string SourceIdentity);
sealed record LegacyCustomer(
    decimal Id, string? CompanyName, string? ContactPerson, string? MobileNumber, string? LandlineNumber, string? Address,
    string? City, string? District, string? State, string? ShippingAddress1, string? ShippingAddress2, string? ShippingAddress3,
    DateTime? LastUpdate, string? TinNumber, string? DueAmount, string? PostalCode, string? Area, string? ResidentialPhone,
    string? Email, string? CreditDays, string? CreditLimit, string? PriceList, string? Transport, string? Salesperson,
    string? CstNumber, string? AccountNumber,
    string? AdditionalContactName, string? AdditionalEmail, string? AdditionalMobile,
    string? PurchaseContactName, string? PurchaseEmail, string? PurchaseMobile,
    string? ManagementContactName, string? ManagementEmail, string? ManagementMobile,
    bool IsDisabled, string? Gstin, string? PreviousBlockLevel, string? BlockLevel, string? OpeningBalance,
    string? Address2, string? Address3, string? TinNumberAlternate,
    string? Contact1Name, string? Contact1Email, string? Contact1Mobile,
    string? Contact2Name, string? Contact2Email, string? Contact2Mobile,
    string? Contact3Name, string? Contact3Email, string? Contact3Mobile,
    string? Contact4Name, string? Contact4Email, string? Contact4Mobile,
    string? Contact5Name, string? Contact5Email, string? Contact5Mobile,
    string? Contact6Name, string? Contact6Email, string? Contact6Mobile,
    string? AdditionalDesignation, string? PurchaseDesignation,
    string? Contact1Designation, string? Contact2Designation, string? Contact3Designation,
    string? Contact4Designation, string? Contact5Designation, string? Contact6Designation);
