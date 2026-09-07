# Customer migration evidence and target mapping

Snapshot date: 2026-09-05. This note records the focused evidence used for the Customer slice; it is not a full legacy inventory or a production-cutover claim.

## Verified legacy behavior

- `Mobile_Customertbl.CustomerID` is the numeric identity primary key. Application business joins and imports also use `Account_no`; source profiling found it populated and distinct in the inspected snapshot even though the legacy database declares no unique constraint.
- Customer administration supports list/search/detail/create/update, separate disable/reactivate behavior, and hard delete. Delete also emits legacy synchronization evidence; the target does not reproduce that unsafe cross-system coupling.
- Verified Customer search fields are Company, Account number, primary `ContactPerson`, primary `MobileNumber`, and `M_mobile`. `Mobile_Contact` name/mobile search in the target is a convenience enhancement, not claimed parity.
- `Pricelist` uses `p`, `d`, `w`, `r`, and `o` for purchase/dealer/wholesale/retail/other Product price bands. Blank source values exist and map to Other.
- `Mobile_Contact` stores normalized contacts without a declared key/FK. `Mobile_ContactUpdate` carries meaningful `IsUpdated` and `IsMobileChanged` review state and feeds `UpdatedContact.aspx`; invitation/delivery/sync flags are separate technical behavior.
- The inspected legacy snapshot contained 1,663 Customer rows. Account numbers were populated/distinct, three rows were disabled, PriceList values were blank/dealer/wholesale, CreditDays parsed as nonnegative integers, and CreditLimit parsed as nonnegative numbers. No unsupported upper range was inferred.
- Due/opening balance and block-level fields are read by accounting/operational paths but do not justify moving the receivables ledger into Customer.
- The current verified workbook uses a named `CUSTOMER` sheet with 33 columns and `Account_no` upsert identity. Present rows insert/update/reactivate; omitted current Customers are disabled. The separate `CONTACTS` sheet belongs to a different contact flow and is deferred.

Primary evidence surfaces include focused `/api/` Customer endpoints, root/mobile Customer and Updated Contact pages, Customer invitation/contact code, the current database metadata export, `sp_InsertUpdateCustomer`, and the inspected workbook. Historical discovery documents were used only for orientation.

## Implemented target mapping

- `dbo.Customers`: tenant-owned Customer master, immutable tenant-unique AccountNumber, status, contact/address/tax/credit defaults, and price-band selection.
- `dbo.CustomerContacts`: tenant-owned normalized additional contacts.
- `dbo.CustomerContactChanges`: immutable before/after evidence plus minimal pending/acknowledged/contact-deleted and mobile-change review meaning.
- `migration.LegacyCustomerAccountingSnapshots`: explicitly non-authoritative Due/opening/block compatibility context.
- `migration.LegacyCustomerMaps`, `LegacyCustomerContactMaps`, and `LegacyCustomerContactChangeMaps`: repeatable Tenant 1 reconciliation keys.

Normal Customer APIs never accept TenantId and never read/write the legacy database. Customer hard delete cascades only Customer-owned dependents; future Orders/Quotation/Receivable foreign keys can block unsafe deletion with 409.

`Lca.LegacyCustomerImport` is permanently Tenant 1-only and dry-run by default. The configured remote source was unavailable, so verification used the existing isolated `LegacyLcaProfile` restore. That restore contained 1,562 Customer masters, 1,272 normalized-contact rows, and zero `Mobile_ContactUpdate` rows. Apply produced 1,562 mapped target Customers and 1,387 unique target contacts, with 13 orphan contacts rejected. Repeated apply created zero Customers and zero contacts and retained the same totals. Multiple legacy representations may map to one target contact, so the compatibility mapping index is intentionally many-source-to-one-target.

No legacy data write, automatic dual write, production-source reconciliation, business acceptance, or ownership cutover is claimed.
