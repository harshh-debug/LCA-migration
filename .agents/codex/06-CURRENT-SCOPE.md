# LCA Current Scope

## Purpose

This document defines the **current implementation scope and immediate delivery priorities** for the LCA modernization repository.

It answers:

> What should Codex work on now, what comes next, and what must remain deferred?

This file is intentionally operational and should be kept shorter and more frequently updated than the architecture documents.

Related files:

- `00-PROJECT-CONTEXT.md` — overall project goal.
- `01-TARGET-ARCHITECTURE.md` — target architecture.
- `02-CURRENT-STATE.md` — current repo implementation snapshot.
- `03-DATA-AND-TENANCY.md` — tenancy/data rules.
- `04-LEGACY-MIGRATION.md` — migration method.
- `05-IMPLEMENTATION-RULES.md` — coding guardrails.

---

## 1. Current Priority

The immediate priority is:

> **Keep the verified shared-schema, Catalog, Customer, and first Platform Owner capabilities stable, then continue the next approved business-module migration.**

Do not build major new business functionality on the superseded per-tenant database-routing model. The foundation correction replaces that earlier migration-first implementation with the accepted shared-schema baseline.

---

## 2. Foundation Work In Scope Now

The current implementation scope includes:

```text
Tenant
TenantMembership
Platform-scoped authorization
Tenant-scoped authorization
Trusted TenantContext
JWT tenant_id handling
Membership validation
Shared-schema TenantId enforcement
SQL Server target schema groundwork
EF Core tenant scoping
Removal/replacement of per-tenant DB routing
Auth/authorization correction required for tenancy
Existing LCA business as initial tenant
Initial legacy-data reconciliation into Tenant 1
```

This work is foundational and should be completed before treating Product/Category/Inventory/Customer APIs as final migration paths.

---

## 3. Identity & Access Is Part of the Current Foundation

Identity and access must become usable, not remain only architectural scaffolding.

Current work should establish enough of the following for the internal application to operate safely:

```text
User identity
Authentication
JWT issuance/validation path
Tenant membership
Platform vs tenant authorization scopes
Roles / permissions required by current migrated features
Trusted tenant context
```

Current identity scope uses separate platform and tenant accounts in the same Identity store. Platform login requires a dedicated `PlatformAdmin` account with no membership. Tenant login requires a distinct tenant account with exactly one active membership. Tenant roles and restricted tenant permissions are deferred; active membership currently grants access to all implemented tenant administration capabilities.

The first Platform Owner account-lifecycle scope is now active and implemented:

```text
PlatformAdmin-assisted Tenant and Tenant-user provisioning
email-owned initial password setup
Tenant self-service forgot/reset password
Platform Owner self-service forgot/reset password
administrative account/membership/Tenant status
security-stamp invalidation of stale JWTs
```

Platform administrators never receive Tenant passwords, setup/reset tokens, or secret-bearing URLs. A production transactional email provider remains an external deployment decision; the repository's file email sink is Development-only.

The exact long-term enterprise identity model may evolve, but the current system must be usable and tenant-safe.

Do not expand into unrelated advanced identity features unless required.

---

## 4. Core Business Modules in Current Scope

After the foundation is corrected, the next business work is:

```text
Product
Category
Pricing
Inventory
```

These should be treated as one coherent core-data stage where dependencies require it.

Do not implement only a superficial Product read API while ignoring the Product behavior needed by current business operations.

Relevant legacy behavior must still be characterized slice-by-slice.

---

## 5. Customer Is in Current Scope

Customer remains part of the current milestone.

Customer is required as internal business/master data for:

```text
Orders
RFQs / Quotations
CRM
billing-related operations
reports
other tenant business workflows
```

The current focus is not a customer-facing ecommerce account model.

Treat Customer primarily as internal tenant-owned business/customer master data for now.

---

## 6. Orders Are in Current Scope

Orders follow Customer in the current milestone.

Broad order:

```text
Foundation
    ↓
Product / Category / Pricing / Inventory
    ↓
Customer
    ↓
Orders
```

Order implementation must follow the migration rules in `04-LEGACY-MIGRATION.md`, including characterization of relevant legacy API behavior, data dependencies, side effects, and write ownership.

---

## 7. Current Frontend Scope

The current Next.js work should focus on a **minimal internal administration interface**.

Current web scope includes only what is necessary to make migrated backend capabilities visibly usable.

Examples:

```text
Login / authentication UI
Basic internal navigation
Product list
Product create/edit
Category/core-data screens as required
Pricing / inventory views or forms where required
Customer admin screens
Order admin screens when the backend slice is ready
Isolated Platform Owner login/navigation
Platform dashboard from new-database data
Tenant and Tenant-user administration
Platform administrative audit
Tenant and Platform Owner password recovery screens
```

The goal is not a polished full product experience yet.

The UI should be sufficient for the client/team to log in and visibly demonstrate migrated functionality.

---

## 8. Customer-Facing Ecommerce Is Out of Scope for Now

A complete customer-facing shopping website is **not part of the current implementation scope**.

Do not currently prioritize:

```text
consumer storefront
public catalogue experience
shopping cart
consumer checkout
consumer account UX
tenant-specific storefront packaging
consumer recommendations
multi-tenant storefront branding
```

These are later product concerns.

The current application should primarily serve internal administration/order-management use cases.

---

## 9. AI Scope

AI is **not part of the current implementation scope**.

The pre-correction repo contained AI-related schema/code added earlier due to AI-team requests. It has been removed from the active foundation.

Current rule:

```text
Freeze
Review when relevant
Do not expand
```

Do not add:

```text
AI agents
LLM providers
agent orchestration
embeddings
AI search
AI-generated writes
new AI governance infrastructure
```

unless scope is explicitly reopened.

Existing AI-related changes may remain temporarily while they are reviewed for compatibility with the corrected SaaS data model.

---

## 10. Redis and Search

Redis and Search remain part of the approved target architecture.

However, in the current milestone they should be introduced **only when an active migrated capability actually needs them**.

Do not implement Redis/Search merely to make the architecture diagram complete.

Examples:

```text
If Product search needs a dedicated search engine:
raise/implement as part of that approved slice.

If caching solves a measured/current requirement:
introduce Redis deliberately.
```

Until then, keep them as target architecture, not forced current infrastructure.

---

## 11. Event-Driven Work

Do not build a new event bus or general background-worker platform in the current milestone by default.

Current rule:

```text
Preserve existing required async behavior
+
identify event-driven improvement opportunities
+
raise them for discussion
```

If a migrated legacy capability already depends on async/scheduled behavior, reproduce that requirement appropriately.

If events/background processing would be a new architectural improvement, propose it first and wait for approval.

---

## 12. Current Data Ingestion Scope

Existing ingestion capability remains part of migration parity, but it should not be the first implementation step.

Current sequence:

```text
Stabilize Product / Pricing / Inventory foundations
        ↓
then implement relevant legacy ingestion parity
```

Current ingestion parity includes:

```text
manual entry
Excel/XLS/XLSX import
existing JSON/API writes
relevant existing application/mobile write flows
```

Do not add:

```text
Tally
generic ERP import
new CSV capability solely for convenience
generic configurable ingestion framework
```

unless separately approved.

---

## 13. Platform Owner Scope

The first Platform Owner administration milestone is current implemented scope:

```text
isolated /platform login/session
new-database dashboard aggregates
Tenant list/search/detail/create/update/activate/deactivate
Tenant-user provision/list/status/membership/setup-email resend
non-secret platform administrative audit
Platform Owner self-service password recovery
```

The following broader SaaS/platform areas remain **outside current implementation scope**:

```text
tenant self-service signup
subscription billing
plan management
feature entitlements
additional PlatformAdmin account management
granular platform RBAC
Tenant impersonation or arbitrary tenant-business CRUD
production transactional email provider selection
Integrations or AI configuration
broad platform reporting/analytics
```

Tenant hard delete is not exposed. Administrative/security state remains distinct from future subscription/payment entitlement state.

The current requirement is to make the core architecture ready for them later.

---

## 14. Current Legacy Inspection Strategy

For migration work, start from:

```text
legacy repo /api/
```

Use it as the primary source for current exposed behavior.

Follow relevant dependencies only as needed into:

```text
App_Code
root ASPX
stored procedures
triggers
mobile
test-api
deepak
ASMX/WebMethods
files
integrations
```

Do not run complete legacy-repo rediscovery for every slice.

---

## 15. Current Broad Delivery Sequence

The current implementation sequence is:

```text
1. Correct multi-tenant foundation

2. Identity & Access usable end-to-end

3. Product
4. Category
5. Pricing
6. Inventory

7. Customer

8. First Platform Owner administration milestone

9. Orders

10. RFQ / Quotation

11. Purchasing / Logistics

12. Reports / CRM
```

Exact implementation grouping may vary where legacy dependencies require it.

Do not use the sequence to skip characterization or tenant/data design work.

---

## 16. Current Success Criteria

The immediate milestone is complete when:

> **The shared-schema multi-tenant foundation is implemented; Tenant/TenantMembership exist; platform and tenant authorization scopes are separated; tenant context is trusted and enforced; SQL Server + EF Core tenant scoping is working; and the existing LCA business/data is represented as the initial tenant.**

In addition:

> **Identity & Access and the first core business modules must be actually usable through the new .NET APIs, with migrated legacy data reconciled.**

And:

> **A minimal internal administration UI must be available so the client can log in and visibly demonstrate migrated functionality such as viewing, creating, and editing Products and related core data instead of seeing only architecture/foundation work.**

And:

> **The Platform Owner can use a separate platform session to view current-platform aggregates, onboard and administratively manage a new Tenant and its password-owning Tenant user, and inspect non-secret platform administration history without acquiring tenant business access.**

This means the milestone is **not complete** merely because:

- architecture docs exist;
- Tenant classes exist;
- migrations compile;
- API placeholders exist;
- the frontend shows only a connectivity/status page.

The result must demonstrate usable migrated business functionality.

---

## 17. Current In-Scope Summary

```text
IN SCOPE NOW

✓ Shared-schema SQL Server tenancy foundation
✓ Tenant + TenantMembership
✓ Trusted tenant context
✓ Platform vs tenant authorization scopes
✓ Identity/Auth required for usable internal app
✓ Existing LCA business as Tenant 1
✓ Legacy data reconciliation into new DB
✓ Product
✓ Category
✓ Pricing
✓ Inventory
✓ Customer
✓ First Platform Owner dashboard/Tenant/Tenant-user/audit milestone
✓ Tenant initial password setup and Tenant/Platform Owner self-service recovery
✓ Orders
✓ Minimal internal Next.js administration UI
✓ Relevant local tests
✓ Legacy parity characterization for migrated slices
```

---

## 18. Explicitly Deferred

```text
OUT OF SCOPE NOW

✗ Full consumer ecommerce storefront
✗ Tenant-specific storefront strategy
✗ Tenant self-service signup
✗ Subscription billing
✗ Plan management
✗ Additional PlatformAdmin account administration
✗ Granular platform RBAC
✗ Tenant impersonation
✗ Production transactional email provider selection
✗ Feature entitlements UI
✗ AI agents/orchestration
✗ New AI functionality
✗ New generic ingestion mechanisms
✗ Tally/ERP integration
✗ Forced Redis implementation without need
✗ Forced Search implementation without need
✗ New event-bus infrastructure without approval
✗ Microservice extraction
```

---

## 19. Rule for Codex

When deciding what to implement next, use this order of authority:

```text
Current explicit task
        ↓
06-CURRENT-SCOPE.md
        ↓
accepted architecture/tenancy rules
        ↓
migration strategy
        ↓
current repo state
        ↓
legacy evidence
```

Do not expand work because a future-state architecture diagram contains additional features.

If a requested implementation requires leaving this scope, surface that before coding.

---

## 20. Current Milestone Mental Model

```text
CURRENT REPO
   |
   | correct foundation
   v
SHARED-SCHEMA MULTI-TENANCY
   |
   +-- Tenant
   +-- TenantMembership
   +-- Auth / Authorization
   +-- Trusted TenantContext
   +-- Tenant-scoped EF Core
   |
   v
CORE BUSINESS
   |
   +-- Product
   +-- Category
   +-- Pricing
   +-- Inventory
   +-- Customer
   +-- Orders
   |
   v
MINIMAL INTERNAL NEXT.JS UI
   |
   v
CLIENT CAN USE / DEMONSTRATE
REAL MIGRATED FUNCTIONALITY
```

The current milestone invariant is:

> **Do not stop at architecture groundwork; deliver a corrected tenant-safe foundation plus enough migrated business functionality and internal UI to make the new platform visibly usable.**
