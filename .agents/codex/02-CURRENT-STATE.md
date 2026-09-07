# Current LCA Migration Repo State

Code snapshot: 2026-09-08. This document records implementation evidence from the current repository and local verification. It describes reality, not future scope.

Status meanings:

- **IMPLEMENTED** — wired into the runtime and locally verified where noted.
- **PARTIAL** — usable foundation exists, but the broader capability is incomplete.
- **SCAFFOLDED** — structure exists without a complete capability.
- **NOT PRESENT** — no current implementation.

## 1. Repository Summary

- **IMPLEMENTED:** a buildable .NET 10 modular-monolith backend with `Lca.Api`, `Lca.Core`, and `Lca.Infrastructure`.
- **IMPLEMENTED:** one SQL Server persistence path using a shared schema and a single `LcaDbContext`.
- **IMPLEMENTED:** ASP.NET Core Identity in the same database as tenant and business data.
- **IMPLEMENTED:** separate platform and tenant login flows with backend-issued JWTs and fail-closed account-scope validation.
- **IMPLEMENTED:** `Tenant`, simplified `TenantMembership`, trusted request-scoped tenant context, and tenant-aware Product/Category persistence.
- **IMPLEMENTED:** tenant-scoped Product, Category, Pricing, and Inventory administration, guarded hard deletes, and verified ITEMMAST workbook preview/apply.
- **IMPLEMENTED:** a Tenant 1-only offline legacy catalog importer; the supplied local restore reconciles to 1,467 target Products.
- **IMPLEMENTED:** tenant-scoped Customer master administration, Customer contacts and Updated Contact review state, guarded hard delete, and verified CUSTOMER workbook preview/apply behavior.
- **IMPLEMENTED:** a Tenant 1-only offline legacy Customer importer. The isolated local restore reconciles to 1,562 mapped target Customers and 1,387 target contacts; production migration/cutover is not claimed.
- **IMPLEMENTED:** a minimal Next.js tenant administration UI for login, Catalog, Customer, contact-review, and verified workbook workflows.
- **IMPLEMENTED:** a first Platform Owner administration slice with an isolated platform login/session shell, new-database dashboard aggregates, Tenant lifecycle administration, Tenant-user provisioning/status management, and non-secret platform audit visibility.
- **IMPLEMENTED:** email-owned initial password setup plus separate Tenant and Platform Owner self-service password recovery, using Identity tokens, security-stamp JWT invalidation, configured rate limits, and a provider-independent email boundary.
- **NOT PRESENT:** Redis, dedicated Search, events/background workers, subscriptions, storefront, production transactional email delivery, and AI runtime/schema.

## 2. Solution and Runtime

| Area | Status | Current evidence |
| --- | --- | --- |
| `src/Lca.Api` | **IMPLEMENTED** host | `Program.cs` registers Identity-backed JWT validation, distinct authorization policies, controllers, health checks, Problem Details, CORS, OpenAPI, and Development account seeding. |
| `src/Lca.Core` | **IMPLEMENTED current Catalog, Customer, and Platform slices** | Contains tenant-owned Catalog/Customer models, platform administration/audit contracts, security/account-lifecycle contracts, and tenant entities/value objects. |
| `src/Lca.Infrastructure` | **IMPLEMENTED current business and Platform slices** | Registers shared SQL persistence, tenant enforcement, business writes/deletes, platform administration, Identity recovery/setup, audit, and workbook ingestion. |
| `src/Lca.LegacyCatalogImport` | **IMPLEMENTED offline migration boundary** | Read-only legacy extraction and repeatable canonical Tenant 1 reconciliation. |
| `src/Lca.LegacyCustomerImport` | **IMPLEMENTED offline migration boundary** | Tenant 1-only Customer/contact extraction, mapping, rejects, and reconciliation; repeated apply was verified against the isolated local restore. |
| `frontend` | **IMPLEMENTED minimal tenant and platform admin** | Tenant business administration plus isolated `/platform` login, dashboard, Tenant/user lifecycle, audit, and both account-recovery surfaces. |
| `tests/Lca.Infrastructure.Tests` | **LOCAL** | Ignored local xUnit project, intentionally outside `Lca.slnx`; currently 23 tests. |

Current HTTP surface:

- `POST /api/v1/platform/auth/login` — platform-account login.
- `GET /api/v1/platform/auth/me` — platform-only identity check.
- `POST /api/v1/platform/auth/forgot-password` and `/reset-password` — anonymous, non-enumerating Platform Owner self-service recovery.
- `POST /api/v1/auth/login` — tenant-account login and active membership resolution.
- `GET /api/v1/auth/me` — tenant-only identity/tenant check.
- `POST /api/v1/auth/initial-password-setup`, `/forgot-password`, and `/reset-password` — anonymous, scope-validated Tenant credential lifecycle.
- `GET /api/v1/platform/dashboard` — platform-only aggregates from the new database.
- `/api/v1/platform/tenants` — platform-only Tenant list/search/detail/create/update/status APIs; Tenant hard delete is intentionally absent.
- `/api/v1/platform/tenants/{id}/users` and `/api/v1/platform/users/**` — platform-only Tenant-account provisioning, status, membership, detail, and initial-setup resend. No established-user reset capability is exposed.
- `GET /api/v1/platform/audit` — platform-only administrative audit visibility.
- `/api/v1/products` — tenant-scoped list/search/create plus detail/update/delete by id.
- `/api/v1/products/{id}/pricing` and `/inventory` — tenant-scoped value updates.
- `/api/v1/products/imports/preview` and `/apply` — verified ITEMMAST workbook workflow.
- `/api/v1/categories` — tenant-scoped list/create plus update/delete by id.
- `/api/v1/customers` — tenant-scoped list/search/create plus detail/update/delete by id.
- `/api/v1/customers/{id}/contacts` — nested normalized contact create/update/delete; contacts are returned with Customer detail.
- `GET /api/v1/customers/{id}/contact-changes` — Customer-specific contact change/review history.
- `GET /api/v1/customer-contact-changes` and `PUT /{id}/acknowledge` — tenant-wide Updated Contact review workflow.
- `/api/v1/customers/imports/preview` and `/apply` — verified named CUSTOMER-sheet workbook workflow.
- `GET /api/v1/system/status`, `/health/live`, and `/health/ready` — operational endpoints.

The old Approval Queue/product-draft endpoints are no longer present.

## 3. Identity, Authentication, and Authorization

`ApplicationUser` extends ASP.NET Core Identity's user and stores `AccountType`, active status, and `RequiresPasswordSetup`. Both account types use the same Identity store and database.

Current account invariants are enforced during login and on every authenticated request:

- A platform account is active, has `AccountType.Platform`, has the `PlatformAdmin` Identity role, and has no `TenantMembership`.
- A tenant account is active, has `AccountType.Tenant`, has no `PlatformAdmin` role, and has exactly one active membership to an active tenant.
- Newly provisioned Tenant users have no password, `EmailConfirmed = false`, and `RequiresPasswordSetup = true`; the emailed 24-hour Identity setup token is single-use and successful setup atomically establishes the password, email confirmation, and completed-setup state.
- Mixed platform/tenant accounts fail closed.
- JWTs carry the current Identity security stamp and are rejected when it changes. Password reset and account/membership/Tenant deactivation rotate or invalidate the relevant stamp, so old access tokens remain unusable after later reactivation.

`JwtTokenIssuer` issues signed access tokens using configured issuer, audience, key, and lifetime. Platform tokens contain `account_type=platform` and the `PlatformAdmin` role but no `tenant_id`. Tenant tokens contain `account_type=tenant` and the resolved `tenant_id` but no platform role.

Authorization is intentionally separated:

- `Policies.PlatformAdmin` protects platform APIs.
- `Policies.TenantAccess` protects tenant APIs and requires a validated trusted tenant context.

There is no tenant-role or tenant-permission model in the current scope. Active tenant membership grants access to the currently implemented tenant administration capabilities.

Tenant forgot/reset and Platform Owner forgot/reset are independent, non-enumerating flows. Password-reset tokens expire after one hour and are single-use. Public recovery/setup attempts use configuration-driven IP rate limits plus a bounded replaceable in-process email throttle. Identity email normalization, `RequireUniqueEmail`, and the unique `NormalizedUserName` index enforce canonical login-email uniqueness; `NormalizedEmail` retains Identity's normal non-unique index. Global `RequireConfirmedEmail` is not enabled, preserving seeded-account behavior.

Refresh tokens, logout/revocation lists, MFA, public tenant self-service signup, subscription entitlements, and additional PlatformAdmin account management are not implemented.

## 4. Tenancy

- `TenantId` is a positive `long` value object and maps to SQL Server `bigint`.
- `Tenant` stores the business identity and active status.
- `TenantMembership` stores only `UserId`, `TenantId`, status, and timestamps.
- A filtered unique index permits at most one active membership per user while allowing multiple users to belong to the same tenant.
- `HttpTenantContext` is initialized only after the bearer token's user, account type, tenant, and membership have been revalidated from the database. It does not independently trust a raw client claim.
- Tenant APIs do not accept a normal-operation `TenantId` override from clients.

The model seeds the existing LCA organization as active Tenant `Id = 1`. Development account seeding can provision separate platform-owner and LCA Tenant 1 accounts from configuration; credentials are not stored in source.

## 5. Database and EF Core

`LcaDbContext` derives from `IdentityDbContext<ApplicationUser>` and owns:

- ASP.NET Core Identity tables;
- `dbo.Tenants`;
- `dbo.TenantMemberships`;
- `dbo.Products`;
- `dbo.Categories`;
- `dbo.ProductPricing`;
- `dbo.ProductInventory`;
- `dbo.ProductMedia`;
- `migration.LegacyCategoryMaps`.
- `dbo.Customers`, `dbo.CustomerContacts`, and `dbo.CustomerContactChanges`;
- `migration.LegacyCustomerAccountingSnapshots`, Customer/contact identifier maps, and contact-change maps.
- `dbo.PlatformAuditEntries`, a platform-owned administrative audit table; it is not a tenant-business or Customer contact audit substitute.

Catalog and Customer tables are new target tables, not legacy runtime mappings. Every tenant-owned entity carries `TenantId`, uses tenant-scoped relationships/query filters, and passes through `SaveChanges` ownership enforcement.

The old `TenantDbContextFactory`, tenant-to-connection options, legacy connection aliases, direct legacy table mappings, Approval Queue service/entities, AI image entity/configuration, and AI governance migration have been removed.

The greenfield migration under `src/Lca.Infrastructure/Migrations` creates the shared foundation. `AddTenantCatalogCapability` and `AddTenantCustomerCapability` add the business schemas without rewriting foundation history; `AllowMultipleLegacySourcesPerCustomerContact` records the verified many-source-to-one-target contact reconciliation rule. `AddPlatformAdministrationAndAccountRecovery` adds `RequiresPasswordSetup` and the platform audit table. The complete migration chain was exercised against the disposable SQL test database and applied to the local Docker development target. Application startup does not automatically apply migrations.

`/health/ready` includes a SQL Server connectivity check. Redis/Search readiness checks do not exist because those capabilities are out of scope.

## 6. Product and Category

Product, Category, Pricing, and Inventory now provide the approved tenant-scoped administration slice.

- Product uniqueness is scoped by `(TenantId, ItemCode)`.
- Category parent and Product-to-Category foreign keys include `TenantId`, preventing cross-tenant relationships.
- Product query filtering/pagination searches verified fields: ItemCode, name, Group1, and Group2; status and Category filters are supported.
- Product create/update, separate Pricing/Inventory updates, inactive status, and guarded hard delete are implemented.
- Product delete is tracked and tenant-scoped, cascades only directly owned Pricing/Inventory/Media, and converts protected-FK failure to 409.
- Category create/update and guarded hard delete are implemented; Product/child references return 409 and no synthetic Category status exists.
- Product media is imported/displayed as read-only legacy paths. New media storage and all media writes remain deferred.

## 7. Legacy Reconciliation State

The legacy repository remains separate during coexistence. No runtime fallback, dual write, or automatic synchronization exists. `Lca.LegacyCatalogImport` is the explicit compatibility boundary: it exposes no tenant argument, validates active Tenant 1, reads legacy SQL only, maps Category IDs, upserts Product by ItemCode, and emits rejects/reconciliation counts.

Against the isolated supplied backup, dry run and repeated applies reported 1,467 source/target Products, zero source Categories, and zero rejects. The latest verified ITEMMAST workbook preview reported 1,510/1,510 valid rows with zero issues; it was not applied automatically.

`Lca.LegacyCustomerImport` is a separate Customer compatibility boundary. It has no tenant argument, refuses to run without active Tenant 1, performs only `SELECT` operations against legacy SQL, maps legacy Customer/contact/review identifiers, and reports rejects and reconciliation counts. The configured remote legacy instance could not be reached, but the isolated local restore was available: apply and repeated apply retained 1,562 mapped Customers and 1,387 unique target contacts, created no duplicates on repetition, and consistently reported 13 orphan legacy contacts. That restore contains zero `Mobile_ContactUpdate` rows. This is local migration verification, not production cutover.

## 8. Customer

- Customer Account numbers are immutable through the update contract and unique by `(TenantId, AccountNumber)`.
- List/search/detail/create/update, active/inactive state, and tenant-safe guarded hard delete are implemented.
- Verified parity search fields are Company, Account number, primary contact name/mobile, and management mobile; normalized contact name/mobile search is explicitly an additional target convenience.
- Customer PriceBand records the five-band selection but performs no Order pricing calculation.
- Normalized contacts support create/update/delete. Each change writes immutable before/after JSON, `IsMobileChanged`, actor/time/source, and pending/acknowledged/contact-deleted review state.
- Legacy DueAmount, OpeningBalance, IsBlock, and PreviousStatus map to a read-only compatibility snapshot rather than writable accounting behavior.
- The named CUSTOMER workbook supports preview and confirmed snapshot apply with upsert/reactivate/disable-omitted semantics. The legacy CONTACTS sheet is not implemented.
- No Orders, receivables ledger, invitation delivery, SMS/push synchronization, customer storefront, or new integration mechanism was added.

## 9. Platform Owner Administration

- `/platform/login` uses only the platform login flow and stores its JWT under a platform-specific browser-session key; it does not initialize Tenant context or reuse the tenant session.
- `/platform` shows Tenant/user/Product/Customer aggregates and recent platform activity from the new database only.
- `/platform/tenants` and `/platform/tenants/[id]` support Tenant search/create/metadata/status plus Tenant-user provisioning, awaiting-setup resend, and account/membership status. Platform administrators never receive a password, setup token, setup URL, or established-user reset action.
- `/platform/audit` shows non-secret administrative events. Credential tokens, secret-bearing URLs, passwords, and email bodies are not stored in `PlatformAuditEntry`.
- Legitimate cross-tenant counts live only in `PlatformAdministrationService`; `IgnoreQueryFilters` is explicit and contained to read-only Product/Customer aggregate queries. Platform JWTs still cannot call normal tenant business controllers.
- Tenant-user provisioning commits `ApplicationUser`, `TenantMembership`, and `TenantUserProvisioned` audit atomically. Setup-token generation/email happens after commit; failure leaves an awaiting-setup account that can be retried.
- The Development email sink writes local `.eml` delivery artifacts and is not production email delivery. Docker persists Data Protection keys across API container recreation so issued local links remain valid for their configured lifetime.

## 10. AI and Other Out-of-Scope Infrastructure

AI governance entities, Approval Queue behavior, and the legacy-altering AI migration have been removed from the active foundation. No AI capability was expanded.

Redis, dedicated Search, events, background workers, subscription billing, customer storefront, integration/AI administration, broad platform reports, and microservices are not present.

## 11. Tests and Local Verification

On 2026-09-08, in addition to the earlier foundation/Catalog/Customer checks:

- `dotnet build Lca.slnx --no-restore` passed with 0 warnings and 0 errors.
- The ignored Docker-SQL infrastructure suite passed 23/23 tests. Platform coverage includes atomic provisioning/rollback, normalized email uniqueness through Identity, setup link replacement/single use, Tenant-vs-Platform recovery separation, security-stamp reset invalidation, SQL-translated user listing, and account/membership/Tenant deactivation stamp rotation.
- `dotnet ef migrations has-pending-model-changes` reported no pending schema changes, and `AddPlatformAdministrationAndAccountRecovery` is applied to the local Docker `Lca` database.
- Frontend ESLint and TypeScript checks passed. The production Next.js build passed with webpack; the default Turbopack build was blocked by the execution environment from binding its internal port rather than by a code/type failure.
- Docker HTTP verification completed a future-Tenant onboarding flow: platform login, Tenant create/read, no-password user provisioning, direct Development-email setup, restart-safe and single-use setup, Tenant login, empty Product/Customer isolation, self-service Tenant reset, and Platform Owner self-service reset.
- Cross-scope calls returned 403; pre-setup login returned 401; repeated setup returned 409; repeated reset returned 400; password resets returned 401 for stale Tenant and Platform JWTs.
- Account, membership, and Tenant deactivation each returned 401 for the old Tenant JWT both while inactive and after reactivation; fresh login succeeded after reactivation.
- Configured recovery rate limiting returned 429 at the expected boundary. Platform audit details contained no password, token, or secret-bearing URL, and a Tenant recovery request for the Platform Owner email produced no Tenant reset message.

- `dotnet build Lca.slnx --no-restore` passed with 0 warnings and 0 errors, including both offline import tools.
- `dotnet ef migrations has-pending-model-changes` reported no changes after the Customer migration.
- The ignored local infrastructure test project passed 19/19 tests against a dedicated Docker SQL Server database.
- Customer tests cover tenant-isolated CRUD, per-tenant AccountNumber uniqueness, status/update behavior, normalized contact change history/mobile-change/acknowledgement, direct-dependent cleanup, cross-tenant/missing delete behavior, and 409 through a test-only restrictive foreign key.
- Customer workbook tests cover preview, explicit confirmation, create/update, contact mapping, repeated apply idempotency, price-band mapping, and disable-omitted snapshot behavior.
- `pnpm check` passed ESLint, TypeScript, and the repository-local production Next.js build.
- `AddTenantCustomerCapability` applied successfully to the local Docker development database.
- The configured remote legacy SQL Server could not be located, but the isolated `LegacyLcaProfile` restore was profiled and applied locally: 1,562/1,562 Customers mapped, 1,387 contacts reconciled, 13 orphan contacts rejected, and a repeated apply created zero duplicate Customers/contacts.
- HTTP smoke verification passed tenant/platform login, platform-token rejection (403), tenant Customer access, Customer create/update, contact create/update, review-history read, hard delete (204), and post-delete 404.
- The verified legacy CUSTOMER workbook previewed through the tenant API with 1,649/1,649 valid rows and zero issues; it was not applied automatically.
- The final rebuilt API and SQL Server containers both reported healthy.

Earlier verified foundation/Catalog evidence remains:

- `dotnet build Lca.slnx --no-restore` passed with 0 warnings and 0 errors.
- The ignored local infrastructure test project passed 10/10 tests, including API policy/409 contracts, catalog tenant filters, missing/cross-tenant deletion, owned-row cleanup, guarded Category deletion, and a Product protected-FK conflict through a test-only disposable SQL fixture.
- The initial migration applied successfully to a fresh Docker SQL Server database.
- The API and SQL Server containers became healthy.
- Platform and tenant login contracts produced the expected mutually exclusive claims.
- Cross-scope platform/tenant API calls returned 403.
- Supplying one account type to the other login flow returned 401.
- A pre-existing tenant token returned 401 after its membership was made inactive; the membership was restored afterward.
- SQL verification confirmed same-tenant Product item-code uniqueness, one-active-membership-per-user, multiple users per tenant, and tenant-filtered Product reads. Temporary verification rows were removed.

The local test project remains ignored by repository policy and is not part of solution-level test discovery. Current ESLint, TypeScript, and workspace-local webpack production-build checks pass. The default Turbopack production build currently fails only because this execution environment denies its internal port binding. Earlier Docker API verification remains valid for 1,467 imported Products, tenant Product/Category CRUD, Pricing and negative Inventory writes, and platform-token rejection from tenant Catalog APIs. The latest ITEMMAST workbook preview completed with 1,510 valid rows and zero issues.

## 12. Remaining Foundation Limits

- A production transactional email provider/configuration is not selected. Production provisioning and recovery deployment remain blocked on that provider decision; the current file sink is Development-only.
- The UI and APIs manage Tenant accounts but intentionally do not manage additional PlatformAdmin accounts. The current primary Platform Owner remains configuration-seeded and has self-service recovery.
- JWT refresh/revocation infrastructure is not implemented; database revalidation provides immediate rejection when account, membership, or tenant state becomes invalid.
- Permanent Product media upload/change/delete storage remains deferred; complete Product write parity cannot be claimed until that accepted gap is resolved if required for cutover.
- Customer data is locally reconciled from the isolated restore only; production source migration, business acceptance, and sole-writer cutover remain outstanding.
- Orders and broader tenant-owned modules are not implemented.
- Automated HTTP/SQL integration tests are not yet tracked; the current verification includes local tests and manual Docker-backed checks.
