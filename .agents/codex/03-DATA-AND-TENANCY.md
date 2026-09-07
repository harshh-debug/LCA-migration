# LCA Data and Tenancy Architecture

## Purpose

This document defines the canonical **data ownership, tenant identity, tenant isolation, and tenancy rules** for the new LCA platform.

It is a target-architecture document, not a dump of the legacy database schema.

Use it to answer:

> Who owns this data, how is tenant context established, and how must tenant-scoped reads/writes be enforced?

Related files:

- `00-PROJECT-CONTEXT.md` — project/business context.
- `01-TARGET-ARCHITECTURE.md` — overall application architecture.
- `02-CURRENT-STATE.md` — what the current migration repo actually implements today.
- `04-LEGACY-MIGRATION.md` — how legacy behavior/data is moved into this target model.

---

## 1. Core Tenancy Decision

The initial multi-tenant data model is:

```text
One SQL Server database
+
Shared schema
+
Tenant-owned rows scoped by TenantId
```

The initial architecture is **not database-per-tenant**.

The pre-correction repository contained experimental tenant-to-database routing from an earlier approach. The corrected foundation removes it; do not reintroduce or extend that model.

---

## 2. Primary Data Ownership Categories

Every new persistent entity must be classified as exactly one of these categories:

### Platform-owned

Data owned by the SaaS platform itself.

Examples:

```text
Tenant
PlatformUserRole / PlatformPermission assignments
Plan
Subscription
PlatformConfiguration
PlatformAudit
FeatureEntitlement
```

Some of these are later-scope features, but the ownership boundary should remain clear.

### Tenant-owned

Business data belonging to one tenant.

Examples:

```text
Product
Category where tenant-specific
Price
Inventory
Customer
Order
OrderItem
RFQ / Quotation
Purchase Order
Supplier
CRM activity
Tenant-specific report state
Tenant-specific files/metadata
```

Tenant-owned records must have an enforceable tenant boundary.

### Global/shared reference data

Reference data intentionally shared across tenants.

Examples may include:

```text
Country
Currency
System-wide reference codes
Provider type definitions
Other explicitly global lookup data
```

Do not classify data as global merely because the legacy system stored it once.

Global/shared status must be intentional.

---

## 3. Tenant Entity

The new platform requires a real persisted Tenant concept.

Conceptually:

```text
Tenant
------
Id
Name
Slug
Status
CreatedAt
UpdatedAt
```

The exact columns may evolve, but a Tenant must represent the business boundary used for authorization and data ownership.

The corrected foundation now persists this Tenant model. Future modules must use that persisted business boundary rather than inventing a separate tenant concept.

---

## 4. User Identity Model

A user is a **global application identity**, not a tenant-owned row. Each current-scope account is explicitly either a platform account or a tenant account.

Do not model the core user account as:

```text
User
----
Id
TenantId
...
```

The preferred conceptual model is:

```text
User
----
Id
Identity fields
Status
...
```

Tenant association is handled separately.

The account itself does not carry `TenantId`. A tenant account obtains its one active tenant association through `TenantMembership`.

---

## 5. Tenant Membership

Tenant association is represented through a membership relationship.

Conceptually:

```text
TenantMembership
----------------
TenantId
UserId
Status
CreatedAt
UpdatedAt
```

For the current scope, every active tenant account has exactly one active membership and receives full access to the currently implemented tenant administration capabilities. There is no tenant-role hierarchy, `TenantRole`, or tenant permission assignment model yet.

The database must allow multiple different tenant users to belong to the same tenant. It must prevent one user from having more than one active membership. Historical inactive memberships may remain.

Tenant employee roles or restricted tenant permissions must be introduced only when a concrete future requirement needs different access levels.

---

## 6. Platform Access vs Tenant Membership

Platform-level authorization and tenant membership are mutually exclusive account scopes in the current implementation.

Conceptually:

```text
Platform ApplicationUser
├── AccountType = Platform
├── PlatformAdmin Identity role
└── no TenantMembership

Tenant ApplicationUser
├── AccountType = Tenant
├── no PlatformAdmin role
└── exactly one active TenantMembership
```

Examples:

```text
Platform Administrator
- may manage tenants at platform level
- is not implicitly a business user inside every tenant

Tenant Administrator
- belongs to a specific tenant
- manages that tenant's business operations
```

The LCA organization uses two separate identities and credentials: a platform-owner account and a Tenant 1 account. A single `ApplicationUser` must not combine `PlatformAdmin` and `TenantMembership`. Both identities use the same ASP.NET Core Identity store and SQL Server database.

The schema must not limit the platform to exactly one `PlatformAdmin`; additional dedicated platform accounts may be provisioned later.

---

## 7. Current Product Focus

The current product focus is primarily an **internal business administration / order-management platform**.

Do not currently optimize the tenancy model around a complete multi-tenant customer-facing shopping experience.

Customer storefront behavior across multiple tenants is later scope and may depend on plan/product decisions.

This does not change the requirement that all business-admin data and APIs be tenant-safe from the beginning.

---

## 8. Trusted Tenant Context

Tenant context must be established on the server before accessing tenant-owned data.

The exact routing/discovery mechanism is not finalized.

One current decision is that the active `TenantId` will be carried in the authenticated JWT token.

Conceptually:

```text
Tenant JWT
├── sub = tenant user identity
├── account_type = tenant
└── tenant_id = active tenant
```

The JWT tenant claim alone is not sufficient unless the platform also verifies that the user has exactly one valid active membership and that it matches the active tenant. Platform JWTs contain no `tenant_id` and cannot establish tenant context.

The trusted server-side flow should therefore become:

```text
Authenticated JWT
      ↓
Read user identity
      ↓
Read active tenant claim
      ↓
Validate tenant exists and is active
      ↓
Validate user membership / entitlement
      ↓
Create CurrentTenant / TenantContext
      ↓
Authorize request
      ↓
Access tenant-owned data
```

Downstream code should depend on a trusted tenant-context abstraction rather than parsing tenant values independently.

---

## 9. Do Not Trust Arbitrary Client Tenant IDs

Normal tenant-owned endpoints should not authorize access based solely on a tenant ID supplied in a query string, body, route, or custom header.

The authenticated/trusted tenant context must determine the active tenant.

Client-provided tenant identifiers are only appropriate for explicit platform-level workflows where the caller has permission to target another tenant.

---

## 10. Initial Tenant Isolation Strategy

The approved initial isolation strategy is:

```text
Application/API enforcement
+
trusted TenantContext
+
tenant-scoped EF Core/repository queries and writes
```

SQL Server Row-Level Security is **not required initially**.

RLS may later be evaluated as defense-in-depth if compliance, security, or operational requirements justify it.

---

## 11. TenantId on Tenant-Owned Data

Major tenant-owned entities should carry `TenantId` directly unless there is a strong modeled reason not to.

Examples:

```text
Product
---------
Id
TenantId
SKU
Name

Customer
---------
Id
TenantId
Name

Order
---------
Id
TenantId
CustomerId
OrderNumber

Inventory
---------
Id
TenantId
ProductId
Quantity
```

Direct tenant ownership on important aggregates improves isolation, indexing, auditing, tenant-safe uniqueness, and query clarity.

---

## 12. EF Core / Repository Enforcement

Tenant-aware persistence should be systematic.

Preferred techniques may include:

- EF Core global query filters where appropriate;
- tenant-aware repositories/services;
- mandatory tenant predicates;
- centralized write validation;
- tenant-aware entity creation.

Do not assume global query filters alone solve every case. Platform queries, background jobs, imports, and explicit cross-tenant operations need carefully controlled bypass paths.

Any bypass must be explicit and privileged.

---

## 13. Write Rules

For normal tenant-owned writes:

```text
Request
  ↓
Trusted TenantContext
  ↓
Authorization
  ↓
Application service
  ↓
Entity.TenantId = CurrentTenant.Id
  ↓
SQL Server
```

Do not accept a tenant-owned entity's `TenantId` from an arbitrary frontend payload and persist it unchanged.

For updates, verify the existing row belongs to the active tenant before mutation.

---

## 14. Cross-Tenant Access

Cross-tenant reads/writes are exceptional platform operations.

They must:

- require explicit platform-level authorization;
- not reuse ordinary tenant-user permissions;
- make the target tenant explicit;
- be auditable where appropriate;
- avoid accidental broad queries.

Do not give ordinary tenant services an unrestricted all-tenants mode.

---

## 15. Tenant-Scoped Uniqueness

Uniqueness must be decided per entity.

Default rule:

> If an identifier has business meaning within a tenant, prefer tenant-scoped uniqueness.

Examples:

```text
UNIQUE (TenantId, SKU)
UNIQUE (TenantId, OrderNumber)
UNIQUE (TenantId, CustomerCode)
UNIQUE (TenantId, InvoiceNumber)
UNIQUE (TenantId, QuoteNumber)
```

Use global uniqueness for inherently platform-wide identifiers such as internal UUID primary keys or truly global reference codes.

---

## 16. Existing LCA Business as Tenant 1

The existing LCA business becomes an initial tenant in the new platform.

Migrated legacy business data is associated with that tenant.

For example:

```text
Legacy Product
     ↓
New Product
TenantId = ExistingLcaTenantId

Legacy Customer
     ↓
New Customer
TenantId = ExistingLcaTenantId

Legacy Order
     ↓
New Order
TenantId = ExistingLcaTenantId
```

Avoid permanent special-case logic for the existing LCA business.

---

## 17. Legacy Firm/Company Context Is Not Trusted Tenancy

The legacy application contains multiple firm/company/context mechanisms such as session values, firm IDs, hostnames, database aliases, caller-provided values, and separate connections.

These may matter during coexistence, but they do not define the new trusted tenant boundary.

Legacy firm/company identifiers must be intentionally mapped to the new Tenant model.

Do not equate a legacy `FirmId` with the new `TenantId` without an explicit migration mapping.

---

## 18. Current Repo Tenancy State vs Target

The foundation correction now implements the accepted shared-schema model for the current Product/Category surface:

```text
Tenant and simplified TenantMembership persistence
separate Platform and Tenant ApplicationUser account types
separate platform and tenant login flows
membership resolution and validation
trusted server-side TenantContext
shared-schema TenantId on Product and Category
global tenant query filters and guarded writes
one shared SQL Server LcaDbContext
local cross-tenant isolation verification
```

The superseded tenant-to-database routing and AI/Approval Queue schema have been removed. See `02-CURRENT-STATE.md` for exact implementation evidence and remaining limits.

---

## 19. Legacy Table Compatibility

The pre-correction Product/Category models mapped directly to legacy tables. Those mappings have been removed from the target `LcaDbContext`.

If direct legacy access is introduced for a later approved reconciliation slice, treat it as explicit migration/coexistence compatibility work, not as the authoritative SaaS schema. Before adding such a mapping, determine whether it represents:

```text
a compatibility adapter to legacy data
or
the new authoritative SaaS model
```

Do not silently mix both purposes.

---

## 20. Default Ownership Map

| Capability / Data | Ownership |
|---|---|
| Tenant | Platform-owned |
| User identity | Platform/global identity |
| TenantMembership | Platform-owned relationship defining tenant access |
| Platform role/permission assignment | Platform-owned |
| Product | Tenant-owned |
| Tenant-specific Category | Tenant-owned |
| Pricing | Tenant-owned |
| Inventory | Tenant-owned |
| Customer | Tenant-owned |
| Order / Order Item | Tenant-owned |
| RFQ / Quotation | Tenant-owned |
| Supplier | Tenant-owned |
| Purchase Order | Tenant-owned |
| CRM Activity | Tenant-owned |
| Shipment / Logistics record | Tenant-owned |
| Tenant-specific report data | Tenant-owned |
| Tenant-specific media/file metadata | Tenant-owned |
| Country/Currency/global lookup where explicitly shared | Global/shared |
| Plan / Subscription | Platform-owned |
| Platform audit | Platform-owned |

A concrete domain slice may refine this table.

---

## 21. Ingestion and Tenant Ownership

All ingestion paths must apply tenant ownership.

Current ingestion parity includes:

- manual entry;
- Excel/XLS/XLSX import;
- existing JSON/API writes;
- relevant existing application/mobile flows.

Target flow:

```text
Authenticated Tenant User
        ↓
Trusted TenantContext
        ↓
Upload / API / Manual Entry
        ↓
Validation / Mapping
        ↓
Domain/Application Service
        ↓
Tenant-owned rows with TenantId
```

Imports must not create rows without tenant ownership.

Workbook/API values must not override the active tenant.

---

## 22. Files, Search, Cache, Events

Tenant boundaries must survive outside SQL Server.

### Files / media

Tenant-specific files and generated documents need clear tenant ownership in metadata and access control.

Filesystem separation alone is not sufficient isolation.

### Search

Tenant-owned indexed documents must carry/enforce tenant visibility.

```text
Indexed Product
---------------
TenantId
ProductId
...
```

### Redis

Tenant-sensitive cache keys must include tenant scope.

Example:

```text
tenant:{tenantId}:product:{productId}
```

### Events / background jobs

Tenant-owned events/jobs must carry `TenantId` explicitly.

Background workers must reconstruct trusted tenant context from validated job/event data rather than HTTP request state.

---

## 23. AI-Related Data

The experimental AI-governance/product-draft schema has been removed from the corrected foundation. AI remains outside current implementation scope.

If AI scope is reopened later, any AI-related data belonging to a tenant must be tenant-scoped and must not become a path around tenant authorization.

Do not introduce or extend AI schema without explicit approval.

AI data must not become a path around tenant authorization.

---

## 24. Customer-Side Multi-Tenancy

Customer-facing multi-tenant shopping behavior is later scope.

For now, prioritize tenant-safe internal capabilities such as:

```text
business administration
catalogue/product management
pricing/inventory
customer records
orders
quotations
purchasing
logistics
reports/CRM
```

Do not introduce complex customer-to-multiple-tenant modeling unless an approved slice requires it.

---

## 25. SQL Server Row-Level Security

Initial decision:

```text
SQL Server RLS = not required
```

Current protection should come from:

```text
trusted tenant context
+
authorization
+
tenant-scoped persistence
+
tests
```

RLS may later be added as defense-in-depth.

---

## 26. Required Tenant Isolation Tests

As tenant-owned modules are implemented, tests should prove:

```text
Tenant A cannot read Tenant B data
Tenant A cannot update Tenant B data
Tenant A cannot delete Tenant B data
Tenant A cannot create data assigned to Tenant B
missing tenant context fails closed
invalid membership fails closed
tenant-scoped uniqueness behaves correctly
```

Platform-level cross-tenant operations need separate authorization tests.

---

## 27. Data Design Checklist

Before adding a persistent entity, answer:

```text
1. Is it Platform-owned, Tenant-owned, or Global/shared?
2. If Tenant-owned, where is TenantId stored?
3. How is TenantId assigned on writes?
4. How are reads tenant-scoped?
5. What is unique globally vs within a tenant?
6. Can platform-level users access it cross-tenant?
7. How is that access authorized?
8. Does it appear in Search/cache/events/files?
9. If yes, how is tenant context preserved there?
10. Does legacy data require a migration mapping?
```

Do not invent unresolved answers silently.

---

## 28. Canonical Tenant Request Model

```text
Tenant JWT (sub + account_type=tenant + tenant_id)
        |
        v
Authentication
        |
        v
Validate active Tenant ApplicationUser
        |
        v
Validate exactly one active TenantMembership and active Tenant
        |
        v
Trusted TenantContext
        |
        v
Tenant authorization policy
        |
        v
Application Service
        |
        v
Tenant-scoped EF Core
        |
        v
SQL Server
```

Platform JWTs follow the separate platform path: `account_type=platform`, `PlatformAdmin` Identity role, no membership, no `tenant_id`, and platform-only authorization policies.

The core invariant is:

> **A shared application and shared database are acceptable; accidental sharing of tenant-owned data is not.**
