# LCA Decisions and Open Questions

## Purpose

This document records the current architectural/product decisions for the LCA modernization project and separates them into:

- **ACCEPTED** — treat as current source of truth.
- **OPEN** — not finalized; do not invent a decision.
- **DEFERRED** — expected later, but not part of current implementation scope.
- **SUPERSEDED** — older assumptions that must no longer guide implementation.

Use this file to prevent Codex from reviving old assumptions or silently deciding unresolved architecture.

Related files:

- `00-PROJECT-CONTEXT.md`
- `01-TARGET-ARCHITECTURE.md`
- `02-CURRENT-STATE.md`
- `03-DATA-AND-TENANCY.md`
- `04-LEGACY-MIGRATION.md`
- `05-IMPLEMENTATION-RULES.md`
- `06-CURRENT-SCOPE.md`

---

# 1. Accepted Decisions

## 1.1 Product Direction — ACCEPTED

LCA is being rebuilt as a **multi-tenant SaaS business platform**, not merely converted from ASP.NET Web Forms to newer .NET syntax/frameworks.

Applicable legacy LCA business capabilities are the functional baseline.

Legacy implementation architecture is not the target architecture.

---

## 1.2 Backend Stack — ACCEPTED

```text
ASP.NET Core
.NET 10
SQL Server
```

The initial backend should remain a modular monolith unless concrete evidence later justifies extraction.

Preferred current project structure:

```text
Lca.Api
Lca.Core
Lca.Infrastructure
```

---

## 1.3 Frontend Stack — ACCEPTED

```text
Next.js
TypeScript
```

The current frontend focus is a minimal internal administration interface for migrated business functionality.

---

## 1.4 Target Database — ACCEPTED

The target database engine is:

```text
SQL Server
```

Do not treat PostgreSQL or another database engine as an unresolved option unless this decision is explicitly reopened.

---

## 1.5 Database Separation During Migration — ACCEPTED

The legacy system retains its existing SQL Server database during coexistence.

The new SaaS platform uses a **separate new SQL Server database**.

Legacy data required by the new system is transformed/migrated/reconciled into the new database.

---

## 1.6 Multi-Tenant Persistence Model — ACCEPTED

Initial tenancy model:

```text
One new SQL Server database
+
shared schema
+
TenantId on tenant-owned data
```

The target is **not database-per-tenant**.

Tenant isolation is initially enforced through:

```text
trusted TenantContext
+
application/API authorization
+
tenant-scoped EF Core/repository queries and writes
```

SQL Server Row-Level Security is not required initially.

---

## 1.7 User Model — ACCEPTED

A user is a global identity.

Do not bind the core user account directly to one tenant using `User.TenantId`.

Tenant association is modeled through:

```text
TenantMembership
```

Conceptually:

```text
User
  |
  +-- TenantMembership --> Tenant
```

For the current scope, a tenant account must have exactly one active membership. The schema may retain inactive historical memberships and must allow multiple users within the same tenant, but active multi-tenant membership and tenant switching are not supported.

---

## 1.8 Platform Access vs Tenant Access — ACCEPTED

Platform authorization and tenant membership use separate `ApplicationUser` identities in the current scope.

```text
Platform account
├── AccountType.Platform
├── PlatformAdmin Identity role
└── no TenantMembership

Tenant account
├── AccountType.Tenant
├── no PlatformAdmin role
└── exactly one active TenantMembership
```

The LCA organization receives separate platform-owner and Tenant 1 credentials. A single account combining both scopes is unsupported and must fail closed. Both identities use the same Identity store and database. The database must permit additional dedicated platform administrators later.

---

## 1.9 Authentication Technology — ACCEPTED

ASP.NET Core Identity is the target identity system.

The implementation should provide the identity lifecycle needed by the current internal application.

---

## 1.10 JWT Issuance — ACCEPTED

The new LCA backend will issue JWT access tokens.

JWT validation without an issuer/user lifecycle is only a partial current-state implementation and is not the final target.

---

## 1.11 Active Tenant Resolution — ACCEPTED

For the current scope, the tenant user does **not manually select or switch TenantId during normal application use**.

The intended flow is:

```text
User authenticates
      ↓
Backend validates User
      ↓
Backend resolves user's valid TenantMembership
      ↓
Active tenant is established
      ↓
Backend issues JWT containing tenant_id
      ↓
User operates inside that tenant
```

The active tenant must come from trusted platform data, not from an arbitrary client-provided tenant ID.

Normal tenant APIs use the tenant context from the authenticated JWT.

If future product requirements introduce users who actively operate across multiple tenants, the tenant-switching UX/token lifecycle must be designed explicitly at that time.

---

## 1.12 Roles and Permissions — ACCEPTED DIRECTION

Do **not** attempt to design the complete long-term role/permission catalogue now.

The only current role is the platform-scoped Identity role `PlatformAdmin`. An active tenant account with exactly one active membership receives full access to currently implemented tenant capabilities.

Do not introduce `TenantRole`, `TenantAdmin`, or tenant permission assignments until a future requirement needs differing employee access levels.

Legacy permission semantics must be characterized.

If changing an ambiguous legacy permission could alter legitimate business behavior, ask for confirmation before changing it.

---

## 1.13 Existing LCA Business — ACCEPTED

The current LCA business becomes the initial tenant in the new SaaS database.

Conceptually:

```text
Existing LCA business = Tenant 1
```

Applicable existing business data is migrated/reconciled into Tenant 1.

Platform-owner privilege remains a separate authorization scope.

---

## 1.14 Legacy Migration Method — ACCEPTED

Use strangler-style incremental migration.

```text
Legacy remains live
        ↓
one capability characterized
        ↓
new target slice implemented
        ↓
data migrated/reconciled
        ↓
controlled ownership/cutover
        ↓
legacy path retired only after acceptance
```

No big-bang replacement.

---

## 1.15 Legacy Inspection Priority — ACCEPTED

For current capability analysis:

```text
Start with legacy /api/
```

Then follow only the dependencies required to understand the capability.

Inspect root ASPX, `App_Code`, procedures, triggers, `mobile/`, `test-api/`, `deepak/`, ASMX/WebMethods, files, or integrations when relevant evidence requires it.

Do not rediscover the whole legacy repository for every slice.

---

## 1.16 Write Ownership During Migration — ACCEPTED

Each business aggregate/action should have one authoritative production writer at a time.

Do not introduce automatic dual writes by default.

---

## 1.17 Legacy Compatibility Access — ACCEPTED

ASP.NET Core may temporarily read legacy tables/procedures for an approved migration slice when required.

Such access is compatibility/coexistence logic.

It is not the final SaaS persistence model.

---

## 1.18 Persistence Technology — ACCEPTED

Use:

```text
EF Core by default
```

Dapper may be proposed for a concrete justified use case, but Codex must ask for approval before implementing it.

---

## 1.19 Stored Procedures — ACCEPTED

Relevant legacy stored-procedure behavior should generally be preserved/reused unless there is a justified exception.

Do not replace a relevant stored procedure merely because the logic could be rewritten in C#.

Characterize it first.

---

## 1.20 Database Schema Changes — ACCEPTED

New target-schema changes should normally use EF Core migrations.

Raw SQL is acceptable only when there is a documented reason.

The corrected target migration chain starts with a new greenfield shared-schema migration. The obsolete experimental legacy/AI migration is not a predecessor in this chain. Apply the new initial migration only to a fresh new-platform database; do not layer it over the legacy database or over a database carrying the experimental migration history. Disposable development databases using the obsolete history must be recreated.

---

## 1.21 API Style — ACCEPTED

The application is primarily controller-based.

Continue using ASP.NET Core Controllers unless a concrete task has a strong reason to use another endpoint style.

Do not perform broad Controller-to-Minimal-API rewrites.

---

## 1.22 Data Ownership Classification — ACCEPTED

Every new persistent entity must be classified as:

```text
Platform-owned
Tenant-owned
Global/shared reference data
```

Tenant business data should not become global merely because the legacy application was single-business.

---

## 1.23 Tenant-Scoped Uniqueness — ACCEPTED

Decide uniqueness per entity.

For tenant-specific business identifiers, prefer:

```text
UNIQUE(TenantId, BusinessIdentifier)
```

where appropriate.

Examples may include SKU, order number, customer code, invoice number, or quotation number.

Internal platform IDs remain globally unique.

---

## 1.24 Current Business Scope — ACCEPTED

Current migration work focuses primarily on an:

```text
internal administration
+
order-management
+
business operations system
```

rather than a complete customer-facing ecommerce product.

Current broad implementation order:

```text
Foundation
↓
Identity & Access
↓
Product / Category / Pricing / Inventory
↓
Customer
↓
Orders
↓
RFQ / Quotation
↓
Purchasing / Logistics
↓
Reports / CRM
```

---

## 1.25 Customer Master Data — ACCEPTED

Customer remains in the current business scope because it is required for internal workflows such as:

```text
Orders
RFQs / Quotations
CRM
billing
reports
```

This does not imply that a complete consumer account/storefront model is currently required.

---

## 1.26 Current Frontend Requirement — ACCEPTED

A minimal internal Next.js administration UI is required.

The milestone should demonstrate actual migrated functionality, for example:

```text
login
view Products
create Product
edit Product
related core-data administration
```

A status/connectivity page alone is not sufficient.

---

## 1.27 AI Scope — ACCEPTED

AI is not part of the current core migration implementation scope.

Existing AI-related schema/code is:

```text
freeze
+
review only
+
do not expand
```

unless AI scope is explicitly reopened.

---

## 1.28 Redis — ACCEPTED DIRECTION

Redis is approved as part of the target architecture.

Do not implement it merely because it appears in the target architecture.

Introduce it when a current approved capability has a concrete requirement.

---

## 1.29 Search — ACCEPTED DIRECTION

A dedicated Search capability is part of the target architecture.

Do not choose or implement a search provider until an approved slice requires it.

Search remains derived data and must preserve tenant isolation.

---

## 1.30 Events / Background Processing — ACCEPTED DIRECTION

Do not build a general event bus/background-processing platform by default.

Preserve required legacy asynchronous behavior.

New event-driven architecture improvements must be proposed and approved before infrastructure is introduced.

---

## 1.31 Existing Ingestion Parity — ACCEPTED

Preserve verified ingestion capabilities when their relevant migration slice is implemented:

```text
manual entry
Excel/XLS/XLSX
existing JSON/API writes
relevant existing application/mobile flows
```

Do not add unsupported mechanisms such as Tally or generic ERP ingestion unless separately approved.

---

## 1.32 Tests — ACCEPTED

Add local tests where useful for migrated slices, including:

```text
business behavior
API behavior
tenant isolation/security
important legacy parity
persistence/failure behavior
```

Current project preference is that these development tests remain local and are not pushed to GitHub unless that policy is explicitly changed.

## 1.33 Product / Category / Pricing / Inventory Slice — ACCEPTED

The first migrated business slice uses tenant-owned `Product`, `Category`, one-to-one `ProductPricing`, one-to-one `ProductInventory`, and read-only imported `ProductMedia` rows in the shared target database.

- Normal APIs derive ownership from trusted `TenantContext` and never accept a writable `TenantId`.
- Product search parity covers ItemCode, name, Group1, and Group2. Group fields and historically nullable source values are not made required without evidence.
- Product inactive/disabled state and Product hard delete are separate supported actions.
- Product delete removes directly owned Pricing, Inventory, and Media rows, returns 404 for missing/cross-tenant rows, and returns 409 when protected target relationships exist.
- Category hard delete is supported but returns 409 while Products or child Categories reference it. No Category status model is introduced.
- The verified ITEMMAST `.xls`/`.xlsx` snapshot workflow supports preview and explicitly confirmed apply; omitted Products are disabled only on confirmed apply.
- The offline legacy database importer is permanently restricted to canonical Tenant 1, fails if active Tenant 1 is absent, never writes the legacy database, and has no arbitrary tenant option.
- Legacy Product media paths may be imported and displayed read-only. Product image upload/change/delete is an accepted temporary parity gap and must be resolved before final writer cutover if business acceptance requires media writes.

## 1.34 Customer Slice — ACCEPTED

Customer is tenant-owned master data in the shared target database.

- `AccountNumber` is immutable after create and unique within a tenant through `(TenantId, AccountNumber)`; technical Customer IDs remain global.
- Verified legacy search parity covers Company, Account number, primary contact name, primary mobile, and management mobile. Search over normalized contacts is a separately identified target enhancement.
- The five Customer PriceList codes map to the existing Product price bands: `p` purchase, `d` dealer, `w` wholesale, `r` retail, and `o`/blank other. Order-time price calculation remains deferred to Orders.
- Customer active/inactive state and guarded hard delete remain separate operations. Delete is tenant-scoped, removes direct Customer-owned dependents, returns 404 for missing/cross-tenant rows, and returns 409 for protected target history.
- Additional contacts are normalized as Customer-owned rows. Contact create/update/delete produces immutable before/after change evidence plus the minimal pending/acknowledged/contact-deleted review state and `IsMobileChanged` meaning required by the verified Updated Contact workflow.
- Invitation, SMS, push/native synchronization, public invitation images, and legacy technical sync flags are not reproduced in this slice.
- `CreditDays` and `CreditLimit` use only the nonnegative constraints supported by source profiling; no invented upper business limit is imposed. Due/opening balance and legacy block values are read-only migration snapshots, not a new receivables ledger.
- The verified named `CUSTOMER` workbook sheet supports preview and explicitly confirmed snapshot apply. Present rows upsert/reactivate; omitted Customers are disabled only on confirmed apply. The separate `CONTACTS` sheet is deferred.
- The offline legacy Customer importer is permanently restricted to canonical active Tenant 1, reads the legacy database only, has no arbitrary tenant option, preserves legacy ID maps and meaningful contact-review rows, and produces reject/reconciliation output.
- Customer write ownership has not cut over merely because the target capability is implemented; migration apply, reconciliation, business acceptance, and an explicit cutover are still required.

## 1.35 First Platform Owner Administration Slice — ACCEPTED

The first Platform Owner milestone uses the existing dedicated `AccountType.Platform` + `PlatformAdmin` account boundary and must not grant tenant business access or initialize `TenantContext`.

- A clearly isolated `/platform` Next.js route/layout and platform-specific session key are used now; the boundary remains compatible with a later separate platform domain/deployment.
- Platform APIs cover current-database dashboard aggregates, Tenant list/search/detail/create/update/status, Tenant-user provisioning/status/membership, initial-setup resend, and platform administrative audit.
- Every platform-management API requires `Policies.PlatformAdmin`; Tenant JWTs receive 403. Cross-tenant Product/Customer counts use explicit, contained, read-only query-filter bypass inside the platform administration service only.
- Tenant hard delete is not offered. `Tenant.Status`, `TenantMembership.Status`, and `ApplicationUser.IsActive` remain administrative/security states and must not be reused as future subscription/payment entitlement state.
- Current platform authorization remains a single `PlatformAdmin` role. Additional PlatformAdmin management, granular platform RBAC, and Tenant impersonation are deferred.
- `PlatformAuditEntry` records non-secret platform administrative actions only. It is not an event store, tenant business audit, or replacement for `CustomerContactChange`.
- Integrations, AI configuration, broad reports, platform settings without a current business use, subscriptions, billing, and public Tenant signup remain deferred.

## 1.36 Tenant Account Provisioning and Credential Ownership — ACCEPTED

PlatformAdmin-assisted provisioning atomically commits:

```text
ApplicationUser
+ TenantMembership
+ TenantUserProvisioned PlatformAuditEntry
```

The Tenant account is created without a password, with `EmailConfirmed = false` and `RequiresPasswordSetup = true`. Only after that transaction commits does the system generate a 24-hour ASP.NET Core Identity setup token and email the link directly to the registered Tenant email.

- PlatformAdmin may see `Awaiting Setup` and resend the setup email only while setup remains incomplete.
- PlatformAdmin never sees/copies a setup URL or token, chooses a Tenant password, manually confirms email, or initiates recovery for an established Tenant user.
- A setup resend rotates the Identity security stamp before issuing a replacement, invalidating the previous setup link.
- Successful initial setup atomically adds the user-selected password, sets `EmailConfirmed = true`, clears `RequiresPasswordSetup`, and invalidates the setup token. A repeated completed setup returns conflict.
- Post-commit setup-token/email failure leaves the active awaiting-setup identity/membership intact and retryable. Email-delivery success and later audit-persistence failure remain distinct; no automatic duplicate email is sent.
- Email is the login identity. Identity normalization, `RequireUniqueEmail`, `UserValidator`, and the race-safe unique `NormalizedUserName` constraint enforce canonical uniqueness because `UserName = Email`; `NormalizedEmail` itself retains the normal non-unique Identity index. Case-only provisioning duplicates return 409.
- Global `SignInOptions.RequireConfirmedEmail` remains disabled to avoid changing existing seeded accounts. The initial emailed setup flow itself proves control of the new Tenant user's email and explicitly confirms it.

Established Tenant credential recovery is entirely self-service through the Tenant forgot/reset endpoints. The primary Platform Owner similarly recovers only their own Platform account through separate Platform forgot/reset endpoints. Both flows are anonymous, non-enumerating where applicable, account-scope validated, and use one-hour single-use Identity reset tokens.

Successful password reset and administrative account/membership/Tenant deactivation invalidate stale JWTs through the Identity security stamp plus per-request account-scope validation. No custom password hashing or token cryptography is introduced.

Recovery/setup abuse controls are configuration-driven ASP.NET Core IP rate limits plus a bounded replaceable in-process email throttle. Redis/distributed throttling is deferred until multi-instance deployment requires it.

## 1.37 Account Email Boundary — ACCEPTED

Account emails use a narrow provider-independent application contract for Tenant initial setup, Tenant self-service reset, and Platform Owner self-service reset. Provider-specific delivery belongs in Infrastructure.

The checked-in file sender is a Development-only email sink and must not be described as production delivery. Production deployment of these account lifecycles is blocked until a real transactional email provider and its secret configuration are separately approved.

Passwords, tokens, secret-bearing setup/reset URLs, and email bodies containing them must never be application-logged or written to `PlatformAuditEntry`. Docker development persists Data Protection keys so local issued links survive API container recreation.

---

# 2. Open Questions

The following decisions are intentionally unresolved.

Codex must not invent an answer.

---

## 2.1 Search Provider — OPEN

Exact provider is not finalized.

Possible future choices may include:

```text
Elasticsearch
OpenSearch
Azure AI Search
other justified provider
```

Select only when the relevant Search requirements are known.

---

## 2.2 Redis Hosting / Provider — OPEN

Redis is architecturally approved.

Exact deployment/hosting is not finalized.

Examples could include:

```text
self-hosted Redis
managed cloud Redis
provider-specific managed service
```

Do not choose one silently.

---

## 2.3 Event Bus / Job Technology — OPEN

No event-bus/background-processing technology is finalized.

Do not select:

```text
RabbitMQ
Kafka
MassTransit
Azure Service Bus
Hangfire
Quartz
other equivalent technology
```

until an approved use case requires it.

---

## 2.4 Reverse Proxy / Gateway Product — OPEN

Required strangler routing behavior is decided.

Exact product is not finalized.

Candidates may include:

```text
IIS ARR / URL Rewrite
Nginx
YARP
existing load balancer
cloud application gateway
```

Production topology should drive the choice.

---

## 2.5 New Platform File / Media Storage — OPEN

The final target storage solution for files/media is not finalized.

This includes items such as:

```text
product images
generated PDFs
order documents
quotation documents
imports
delivery evidence
```

Legacy paths may need temporary compatibility during migration.

Do not choose local/shared filesystem or object/blob storage without an explicit decision.

---

## 2.6 Legacy-to-New Database Migration Technology — OPEN PER DOMAIN

There is no single mandatory migration mechanism.

Choose per domain based on requirements.

Possible mechanisms include:

```text
SQL migration scripts
one-time import/ETL utility
application migration job
controlled combination
```

For every domain, define:

```text
source
target
transformation
identifier mapping
tenant assignment
reconciliation
rollback consequences
```

before implementation.

---

## 2.7 Detailed Role / Permission Catalogue — OPEN

Only required roles/permissions should be implemented for approved slices.

The complete long-term RBAC/permission model remains open.

Legacy permissions need characterization before mapping.

---

## 2.8 Advanced Multi-Tenant User Switching — OPEN / NOT NEEDED NOW

Current tenant users operate inside the tenant resolved from their valid membership during login.

A future requirement where a user deliberately switches between multiple tenant contexts is not part of the current normal-use flow.

If required later, explicitly design:

```text
tenant selection UX
membership checks
token re-issuance/context switch
auditing
authorization
```

Do not implement it preemptively.

---

## 2.9 Customer-Facing Storefront — NOT CONFIRMED

A complete customer-facing storefront is **not currently confirmed as a finalized product requirement**.

Do not treat assumptions about:

```text
public tenant storefronts
consumer checkout
tenant storefront plans
multi-tenant customer accounts
customer tenant switching
storefront branding
```

as accepted architecture.

The current implementation remains focused on the internal administration/business system.

If customer-facing ecommerce scope is confirmed later, update the project context, target architecture, current scope, and this decision log accordingly.

## 2.10 Production Transactional Email Provider — OPEN / DEPLOYMENT BLOCKER

The required provider-independent email boundary is implemented, but the production provider is not selected.

Possible choices may include SMTP, SendGrid, AWS SES, or another approved transactional provider. Do not add a significant provider package or production secret-management configuration without approval. The Development file sink is not a production option.

---

# 3. Deferred Decisions / Later Scope

These are recognized potential/target product areas but are not current implementation work.

---

## 3.1 Tenant Self-Service Signup — DEFERRED

Tenant self-service onboarding is part of the broader SaaS direction but is not part of the current milestone.

Detailed onboarding flow is not finalized.

---

## 3.2 Subscription Billing — DEFERRED

Subscription billing is a later SaaS requirement.

Billing provider, pricing model, billing cycles, cancellation rules, and payment implementation are not finalized.

---

## 3.3 Plan Management — DEFERRED

Plan/package management is later scope.

Exact packaging and feature entitlement behavior are not finalized.

---

## 3.4 Advanced Platform Owner Capabilities — DEFERRED

The first platform dashboard, Tenant administration, Tenant-user provisioning/status, account recovery, and platform audit slice is implemented.

Additional PlatformAdmin account management, granular roles/permissions, Tenant impersonation/support access, Integrations, AI configuration, platform configuration without a current use, and broad reports remain deferred.

---

## 3.5 Feature Entitlements — DEFERRED

The architecture may later support plan/feature entitlements.

Do not implement a full entitlement engine in the current milestone unless a current requirement specifically needs it.

---

# 4. Superseded Decisions and Assumptions

The following assumptions are explicitly **SUPERSEDED**.

They must not be used as current architecture.

---

## 4.1 Framework-Migration-Only Goal — SUPERSEDED

Old assumption:

> The primary goal is simply ASP.NET Web Forms/.NET Framework 4.8 → ASP.NET Core/Next.js migration.

Current decision:

> The goal is to migrate verified LCA business capabilities into a multi-tenant SaaS architecture while modernizing the technology stack.

---

## 4.2 Database-Per-Tenant Routing — SUPERSEDED

Old assumption:

> Each tenant should select/use a separate configured database connection.

Current decision:

```text
one new SQL Server database
+
shared schema
+
TenantId-based ownership
```

The pre-correction `TenantDbContextFactory` / tenant-to-connection routing was experimental old-assumption code and had to be replaced rather than extended.

---

## 4.3 Target Database Engine Undecided — SUPERSEDED

Old assumption:

> Target engine is undecided and PostgreSQL may be selected.

Current decision:

```text
SQL Server
```

---

## 4.4 AI as Current Migration Scope — SUPERSEDED

Old assumption:

> AI governance/AI product functionality is part of the current core migration work.

Current decision:

> AI is outside the current migration implementation scope. Existing AI-related work is review/freeze-only unless explicitly reopened.

---

## 4.5 One-to-One Legacy Endpoint/Page Translation — SUPERSEDED

Old assumption:

> Each legacy ASPX page/API should receive a direct modern equivalent.

Current decision:

> Migrate business capabilities and behavior, not legacy file structure.

Multiple legacy pages/APIs/procedures may map into one target domain/API capability.

---

# 5. Superseded Repo Items Addressed by the Foundation Correction

The following items described the pre-correction implementation and were never accepted final architecture:

```text
per-tenant database routing
direct mappings to selected legacy Product/Category tables
AI-specific schema migration
direct approved-draft writes into legacy Product table
external-JWT-validation-only authentication
incomplete shared-schema TenantId coverage
```

The approved foundation implementation removes or replaces these items. `02-CURRENT-STATE.md` is the source for current implementation status.

---

# 6. Decision Escalation Rule

Before implementing, Codex must stop and ask if a task requires deciding an unresolved item or changing an accepted decision.

Examples:

```text
new database engine
different tenancy model
Dapper introduction
new event bus / worker platform
new integration
new Search provider
new Redis hosting choice
new file-storage architecture
material permission semantics change
customer-facing storefront architecture
new tenant-switching behavior
AI scope reopening
microservice extraction
```

The question should state:

```text
Decision needed
Why it is needed now
Available options
Recommended option
Impact of the decision
```

Do not silently make the decision in code.

---

# 7. Quick Decision Matrix

| Topic | Status | Current Decision |
|---|---|---|
| ASP.NET Core / .NET 10 | ACCEPTED | Backend target |
| Next.js | ACCEPTED | Frontend target |
| SQL Server | ACCEPTED | Target DB engine |
| Modular monolith | ACCEPTED | Initial architecture |
| Shared-schema tenancy | ACCEPTED | One DB + TenantId |
| Database-per-tenant | SUPERSEDED | Do not extend |
| Global User identity | ACCEPTED | Tenant link via membership |
| TenantMembership | ACCEPTED | Required |
| ASP.NET Core Identity | ACCEPTED | Target identity system |
| Backend-issued JWT | ACCEPTED | Current target |
| Active tenant | ACCEPTED | Resolve from valid membership during login and include in JWT |
| Manual tenant switching | NOT NEEDED NOW | Do not implement |
| Platform vs tenant authorization | ACCEPTED | Separate scopes |
| Complete RBAC catalogue | OPEN | Define per approved slice |
| EF Core | ACCEPTED | Default persistence |
| Dapper | APPROVAL REQUIRED | Ask before implementation |
| Legacy stored procedures | ACCEPTED WITH CHARACTERIZATION | Reuse/preserve unless exception |
| New DB separate from legacy DB | ACCEPTED | Migrate/reconcile data |
| Existing LCA business | ACCEPTED | Initial tenant |
| `/api/` first legacy inspection | ACCEPTED | Follow dependencies as needed |
| One authoritative writer | ACCEPTED | No automatic dual writes |
| Redis | ACCEPTED DIRECTION | Implement when needed |
| Redis provider | OPEN | Not selected |
| Dedicated Search | ACCEPTED DIRECTION | Implement when needed |
| Search provider | OPEN | Not selected |
| Event-driven improvements | APPROVAL REQUIRED | No automatic infra |
| Event/job technology | OPEN | Not selected |
| Reverse-proxy behavior | ACCEPTED | Strangler routing |
| Reverse-proxy product | OPEN | Not selected |
| File/media target storage | OPEN | Not selected |
| DB migration technology | OPEN PER DOMAIN | Choose based on slice |
| AI implementation | OUT OF SCOPE | Freeze/review only |
| Internal admin UI | ACCEPTED | Current frontend focus |
| Customer storefront | NOT CONFIRMED | Do not assume |
| Tenant signup | DEFERRED | Later SaaS scope |
| Subscription billing | DEFERRED | Later SaaS scope |
| First Platform Owner milestone | ACCEPTED | Dashboard, Tenant/Tenant-user lifecycle, recovery, and audit without tenant access |
| Production account email provider | OPEN / DEPLOYMENT BLOCKER | Development file sink only; provider approval required |
| Additional PlatformAdmin management | DEFERRED | Primary Platform Owner only in current UI scope |
| Local development tests | ACCEPTED | Not currently intended for GitHub |

---

# 8. Canonical Rule

When documents, existing code, or old plans conflict:

```text
latest explicit accepted decision
        ↓
07-DECISIONS-AND-OPEN-QUESTIONS.md
        ↓
06-CURRENT-SCOPE.md
        ↓
target architecture / tenancy / migration rules
        ↓
current implementation evidence
        ↓
historical documentation
```

Existing code does not override an accepted architecture decision merely because it already exists.

The decision invariant is:

> **Accepted decisions guide implementation, open questions remain open until explicitly resolved, and superseded assumptions must not quietly return through old code or documentation.**
