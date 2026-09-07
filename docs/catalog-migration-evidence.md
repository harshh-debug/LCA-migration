# Product, Category, Pricing, and Inventory migration evidence

Verified on 2026-09-03 from the legacy source, the supplied SQL backup restored read-only as `LegacyLcaProfile`, and the latest `ITEMMAST` workbook.

## Legacy behavior retained

- Product administration supports create, update, Disable/inactive, and direct hard delete by `ItemCode`.
- Category administration supports create, update, parent selection, and direct hard delete. The legacy database primarily relied on referential failures rather than friendly preflight handling.
- Product list/search code uses ItemCode, item name, Group1, and Group2. The target does not label Description or other added filters as legacy parity.
- Five Product price bands exist: purchase, dealer, wholesale, retail, and other. `sp_ItemPriceUnitforOrder` chooses a band from the customer's one-letter price-list value; Customer ownership/selection remains out of this slice.
- VAT, additional VAT, CST, IGST, SGST, and CGST values are Product pricing inputs. Historical VAT/additional VAT/CST rows contain nulls; zero is meaningful across price/tax data.
- Both numeric `Balance` and string `Curstock` exist and differ on 125 supplied rows. The target preserves both rather than silently selecting one during database migration. Verified stock includes negative values.
- Product images and thumbnail fields are legacy path references. No media write capability is migrated in this milestone.

## Source profile

- SQL backup: 1,467 Products; no blank ItemCodes/names; 9 blank Group1; 434 blank Group2; 12 blank Curstock; no invalid nonblank Curstock numerics.
- All supplied SQL Product rows are active; Disable remains implemented because verified administration supports it.
- The supplied backup contains zero `CategoryMastertbl` rows, so no Categories are synthesized during its import.
- Latest workbook: `ITEMMAST` contains 1,510 data rows and 36 expected columns. Target preview accepted 1,510 rows with zero issues.
- Workbook import upserts included ItemCodes, updates Pricing/Inventory values, and marks omitted Products inactive only after explicit snapshot confirmation.

## Target safeguards

- All normal APIs use the authenticated trusted tenant context and contain no writable TenantId.
- Product and Category relationships use composite tenant keys.
- Product hard delete is tracked, cleans directly owned Pricing/Inventory/Media transactionally, and returns conflict for protected references.
- Category hard delete returns conflict while Products or child Categories reference it; no Category active/status workaround exists.
- `Lca.LegacyCatalogImport` is an offline Tenant 1-only boundary, uses legacy `SELECT` statements only, and emits validation/reconciliation output.
- No runtime legacy persistence, automatic synchronization, dual write, media-file deletion, Customer, or Order work is included.
