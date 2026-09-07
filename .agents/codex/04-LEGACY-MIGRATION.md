# LCA Legacy Migration Strategy

## Purpose

This document defines **how legacy LCA capabilities are migrated into the new multi-tenant SaaS platform**.

It is not a legacy inventory and it is not the target architecture document.

Use it to answer:

> How should Codex inspect, implement, reconcile, and cut over a legacy capability without copying legacy architecture or breaking existing behavior?

Related files:

- `00-PROJECT-CONTEXT.md` — what LCA is and what the project is building.
- `01-TARGET-ARCHITECTURE.md` — target application architecture.
- `02-CURRENT-STATE.md` — what the migration repo implements today.
- `03-DATA-AND-TENANCY.md` — tenant/data ownership rules.

---

## 1. Migration Strategy

LCA uses **strangler-style incremental modernization**.

The legacy ASP.NET Web Forms application remains operational while capabilities are moved into:

```text
Next.js
+
ASP.NET Core / .NET 10
+
New SQL Server database
```

One complete business capability should move at a time.

Do not attempt a big-bang rewrite.

---

## 2. Important Updated Database Decision

The legacy application keeps its **existing legacy SQL Server database** during coexistence.

The new SaaS platform uses a **separate new SQL Server database** designed for the target multi-tenant model.

Conceptually:

```text
Legacy Web Forms
      |
      v
Legacy SQL Server
      |
      | data is characterized / migrated / reconciled
      v
New SQL Server
      |
      v
ASP.NET Core SaaS Platform
```

The existing LCA business becomes the first tenant in the new platform.

Existing business data is migrated/reconciled into:

```text
Tenant 1 = Existing LCA business
```

The platform-owner role remains a separate authorization scope even if the same organization/person also operates Tenant 1.

---

## 3. Do Not Treat Legacy Tables as the Final SaaS Schema

Legacy tables, stored procedures, APIs, files, and helper code are migration evidence.

They are not automatically the new schema.

For each capability, determine:

```text
Legacy representation
        ↓
Business meaning
        ↓
Target tenant-aware model
        ↓
Transformation / mapping
        ↓
New SQL Server
```

Temporary legacy reads are allowed when required for an approved migration slice.

If ASP.NET Core reads legacy tables/procedures temporarily, treat that code as a **compatibility adapter**, not as the final SaaS persistence model.

---

## 4. Primary Legacy Inspection Surface

For current migration work, the first place to inspect for existing application behavior is:

```text
legacy-repo/api/
```

The `api/` folder is the primary legacy API surface used to understand currently exposed functionality and contracts.

Start there when characterizing a capability.

Inspect additional legacy areas only when required to complete the behavior picture, including:

```text
root ASPX pages
test-api/
mobile/
deepak/
App_Code/
ASMX / WebMethods
stored procedures
database triggers
file/storage paths
external integrations
```

Do not automatically inspect the entire legacy repository for every task.

Use the smallest evidence set necessary to understand the capability correctly.

However, do not assume `api/` contains the whole business flow. Follow dependencies outside `api/` whenever an endpoint relies on them.

---

## 5. Core Migration Loop

Every capability should follow this loop:

```text
1. Characterize legacy behavior
2. Identify authoritative data
3. Identify integrations/files/side effects
4. Map behavior to target business capability
5. Define target tenant ownership
6. Implement smallest safe target slice
7. Test against legacy behavior
8. Migrate/reconcile required data
9. Move write/read ownership deliberately
10. Cut over only after acceptance
11. Retire legacy path only when rollback risk is understood
```

Do not skip characterization just because the target implementation appears straightforward.

---

## 6. Feature Parity Rule

The goal is **business-capability parity**, not file/page parity.

For example:

```text
Product.aspx
ProductEdit.aspx
api/product*.aspx
stored procedures
image folders
import code
```

may all belong to one target Product/Catalogue capability.

Do not create one new service per legacy page.

The migration question is:

> What business capability and behavior must survive?

not:

> What modern file corresponds to this legacy file?

---

## 7. Preserve Business Behavior, Not Legacy Defects

Default rule:

> Preserve required business behavior, but do not knowingly reproduce obvious technical defects, weak security, or unsafe implementation patterns.

Examples that should normally be redesigned:

- string-concatenated SQL;
- direct SQL from UI code;
- insecure authorization;
- single-business/global assumptions;
- unsafe tenant handling;
- accidental race conditions;
- fragile filesystem coupling;
- duplicate side effects.

However, if changing behavior could affect a legitimate business rule, permission model, workflow, or externally consumed contract:

**do not decide silently.**

Raise the specific behavior for confirmation before changing it.

This is especially important for:

- permissions;
- role behavior;
- authorization;
- order status logic;
- pricing;
- stock/inventory behavior;
- quotation conversion;
- customer visibility;
- integration side effects.

---

## 8. One Authoritative Writer at a Time

During coexistence, each business aggregate/action should have one authoritative writer.

Example:

```text
Before Product write cutover:
Legacy owns Product writes

After Product write cutover:
New platform owns Product writes
```

Avoid:

```text
Legacy writes Product
+
New platform writes Product independently
```

unless an explicit synchronization strategy has been designed, tested, and approved.

Do not introduce automatic dual-write behavior by default.

---

## 9. Read Coexistence

Parallel reads are acceptable when useful for migration verification.

Example:

```text
Legacy Product read
        |
        +---- compare
        |
New Product read
```

This can be used for:

- response comparison;
- data reconciliation;
- characterization testing;
- staging validation.

Parallel reads must not weaken tenant/security boundaries.

---

## 10. New Database Migration Model

Because the new system uses a separate SQL Server database, each migrated data area needs an explicit mapping.

For each domain define:

```text
Legacy source
Target entity/table
Tenant ownership
Identifier mapping
Transformation rules
Migration order
Referential dependencies
Reconciliation checks
Rollback implications
```

Example:

```text
Legacy Mobile_ItemMaster
        ↓
Product mapping/transformation
        ↓
New Product
TenantId = ExistingLcaTenantId
```

Do not assume legacy IDs or business codes should become new primary keys.

---

## 11. Existing LCA Data Becomes Tenant 1 Data

Existing production/business data belongs to the current LCA business.

When migrated:

```text
Legacy Products
Legacy Customers
Legacy Orders
Legacy Quotations
Legacy Inventory
...
        ↓
New tenant-aware records
        ↓
TenantId = Tenant 1
```

This is a migration rule, not a permanent special case.

Future tenants use the same schema and capabilities.

---

## 12. Current Broad Migration Order

Use this as the current dependency/priority order:

```text
Foundation
    ↓
Product / Category
    ↓
Pricing / Inventory
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

This sequence is a guide, not permission to skip required legacy characterization.

---

## 13. Foundation First

Before business modules become authoritative, stabilize:

```text
SQL Server access
configuration
authentication
authorization
Tenant
TenantMembership
trusted TenantContext
tenant isolation
shared infrastructure needed by current slice
```

The earlier tenancy/database-routing code was replaced by the shared-schema foundation. Do not reintroduce it or build domain work on a tenancy model that conflicts with the accepted architecture.

---

## 14. Product / Category Migration

Product and Category remain early migration targets.

Before Product write ownership moves, characterize at least the relevant legacy behavior around:

```text
product master
categories
identifiers/SKU
pricing fields
stock/inventory relationships
images/media
Excel import
stored procedures
trigger effects
legacy API consumers
other side effects used by the active path
```

Start read-only where practical.

A read API being functional does **not** prove Product migration is complete.

---

## 15. Excel / Existing Ingestion Migration

The legacy system has verified Excel/XLS/XLSX ingestion.

That capability should be preserved when the relevant data migration/onboarding slice is implemented.

Preserve:

- required workbook datasets;
- required field mappings;
- important validations;
- business outcomes.

Do not copy the legacy implementation architecture.

Target direction:

```text
Tenant user / migration process
        ↓
ASP.NET Core ingestion endpoint/service
        ↓
Tenant context
        ↓
validation + mapping
        ↓
target business services
        ↓
new SQL Server
```

Do not add unsupported ingestion methods such as Tally or generic ERP imports unless separately approved.

---

## 16. Customer Migration

Customer migration should happen after the core tenant/security/data foundation is stable.

Characterize:

```text
customer master
contacts
customer codes
permissions/visibility
addresses where applicable
order dependencies
quotation dependencies
existing API contracts
```

Do not assume the legacy customer/contact representation is the target model.

---

## 17. Orders / Commerce Migration

Order work must include more than basic CRUD.

Characterize relevant behavior around:

```text
order master/detail
pricing
customer/product dependencies
status transitions
inventory/stock effects
dispatch
generated files/documents
notifications
legacy API callers
stored procedures
```

If an event-driven design would improve a currently synchronous legacy side effect, Codex should propose it for discussion rather than silently introduce event infrastructure.

If asynchronous behavior already exists and is required, preserve it appropriately.

---

## 18. Quotation / RFQ Migration

Quotation/RFQ migration must account for:

```text
quotation master/detail
customer/product dependencies
pricing
accept/reject behavior
conversion to order
attachments/documents
notifications
existing API contracts
```

Do not combine quotation and order ownership merely because one can create the other.

---

## 19. Purchasing / Logistics Migration

Treat purchasing and logistics as separate capabilities with their own ownership and cutover gates.

Characterize shared dependencies such as:

```text
supplier
product
purchase order
trip/carrier state
transportation
delivery evidence
files/PDFs
email/SMS/provider calls
```

Shared legacy tables do not imply one target module.

---

## 20. Reports / CRM

Reports should move after the authoritative transactional APIs/data are stable.

Reports are read models over governed tenant data.

They must preserve:

- tenant visibility;
- business definitions;
- totals/aggregations;
- relevant filters.

Do not make reporting tables or AI outputs the authoritative business source.

---

## 21. Cutover Gate

A legacy capability should not be retired until all relevant gates are satisfied.

Minimum gate:

```text
Legacy behavior characterized
Target API/UI implemented
Tenant isolation verified
Required data migrated/reconciled
Relevant integrations considered
Relevant files/documents considered
Known external callers considered
Tests pass
Business acceptance obtained where needed
Rollback consequences understood
```

A cleaner implementation alone is not enough reason to cut over.

---

## 22. Reconciliation

For every migrated data area define measurable reconciliation.

Examples:

```text
row counts
business identifiers
order totals
stock quantities
customer counts
quotation counts
rejected/invalid rows
sample record equality
status distributions
generated-document presence
```

Reconciliation should compare business outcomes, not only table counts.

---

## 23. Permissions Require Explicit Care

Legacy permissions are known to be inconsistent and represented in multiple ways.

When migrating permissions:

- inspect active behavior;
- identify the intended business rule;
- do not blindly translate menu visibility into API authorization;
- do not preserve obvious security weaknesses;
- ask for clarification before changing ambiguous permission semantics.

Permission behavior should be migrated deliberately into server-side authorization policies.

---

## 24. Files and External Integrations

A capability is not fully migrated if its required files or integration side effects are ignored.

Check relevant behavior such as:

```text
product images
PDFs
generated business documents
delivery evidence
email
SMS
push notifications
remote services
other active integrations
```

During coexistence, preserve compatibility until ownership is intentionally transferred.

---

## 25. Strangler Routing

During coexistence, both systems may remain deployed.

Conceptually:

```text
Public Entry
    |
    +-- unmigrated legacy routes → Legacy Web Forms
    |
    +-- migrated web routes → Next.js
    |
    +-- new /api/v1/* routes → ASP.NET Core
```

Do not broadly redirect `/api/*` to the new backend because the legacy system already uses `api/*.aspx`.

Use explicit routing/cutover boundaries.

The exact reverse-proxy product remains an infrastructure decision.

---

## 26. Deployment Is Not Cutover

Treat these separately:

```text
New code deployed = yes
Traffic switched   = no
```

A capability may be deployed for testing before becoming authoritative.

This supports:

- preview testing;
- reconciliation;
- controlled pilot;
- rollback preparation.

---

## 27. Rollback Rule

A route switch can roll back traffic, but it cannot undo:

- database writes;
- migrated records;
- generated files;
- sent emails/SMS/notifications;
- external provider calls.

Write-owning cutovers therefore require more care than read-only cutovers.

---

## 28. Event-Driven Changes

When migrating a legacy capability:

### If legacy already has asynchronous/event-like behavior

Preserve the required behavior.

### If event-driven design is newly recommended

Codex should:

1. identify the synchronous coupling;
2. explain the proposed event;
3. explain retry/idempotency/consistency impact;
4. ask for architecture approval;
5. not add broker/worker infrastructure automatically.

This prevents migration scope from expanding silently.

---

## 29. Legacy Evidence Priority

For current migration analysis use this priority:

```text
1. legacy /api/ implementation for exposed behavior
2. directly called helpers / database objects
3. root ASPX pages when needed
4. stored procedures/triggers/database evidence
5. files/integrations needed by that capability
6. test-api/mobile/deepak/other surfaces when evidence shows relevance
```

This is a **focused investigation strategy**, not a claim that other legacy surfaces are unimportant.

If evidence shows a dependency outside `/api/`, follow it.

---

## 30. Do Not Re-Research the Entire Legacy System Per Slice

The repository already has broad discovery/inventory documentation.

Use existing discovery as orientation.

Perform targeted source inspection for the specific capability being migrated.

Do not spend large agent runs rebuilding a complete site inventory unless new evidence proves the existing inventory is insufficient.

---

## 31. Migration Checklist for Codex

Before implementing a migration slice, answer:

```text
1. Which legacy API/capability is being replaced?
2. What business behavior does it provide?
3. What legacy data does it read/write?
4. What files/integrations/side effects matter?
5. Who is the authoritative writer now?
6. What is the new tenant ownership?
7. What target entities/APIs are required?
8. What data must move to the new SQL Server?
9. How will behavior/data be reconciled?
10. What test proves tenant isolation?
11. What legacy callers remain?
12. What is the cutover/rollback boundary?
13. Is any new event-driven behavior being proposed?
14. Does any ambiguous permission/business behavior require confirmation?
```

If critical answers are unknown, stop and surface the gap instead of inventing behavior.

---

## 32. Canonical Migration Mental Model

```text
                   LEGACY LCA
            Web Forms + Legacy DB
                     |
              inspect behavior
                     |
                     v
              Capability Mapping
                     |
          +----------+----------+
          |                     |
   Data Transformation     Behavior Mapping
          |                     |
          +----------+----------+
                     |
                     v
             NEW SaaS CAPABILITY
          ASP.NET Core + Next.js
                     |
                     v
              NEW SQL SERVER
                     |
            TenantId = Tenant 1
                     |
              reconcile/test
                     |
                     v
                 CUTOVER
```

The core migration invariant is:

> **Preserve verified business capability, move ownership deliberately, and redesign implementation for the tenant-aware SaaS platform instead of recreating legacy architecture.**
