# LCA Project Context

## Purpose

This file gives coding agents the canonical high-level context for the LCA modernization project: **what exists today, what the target product is, and how the legacy system should influence new implementation**.

It intentionally does not define detailed code structure, physical database schema, repository status, coding rules, or sprint tasks. Those belong in the other `.agents/codex/` files.

If older planning/PRD material conflicts with this file or with accepted decisions in `07-DECISIONS-AND-OPEN-QUESTIONS.md`, treat the newer accepted decisions as authoritative.

---

## 1. Project in One Sentence

**LCA is being modernized from a legacy single-business ASP.NET Web Forms application into a multi-tenant SaaS commerce platform where multiple businesses can use the same core commerce capabilities with strict tenant data isolation, while a platform owner operates the overall platform.**

---

## 2. Existing LCA System

The current application is a legacy operational platform built with:

- ASP.NET Web Forms;
- .NET Framework 4.8;
- SQL Server;
- ASPX browser pages;
- page-based ASPX APIs and older ASMX/WebMethod surfaces;
- ASP.NET Session;
- ADO.NET / `SqlDataSource` / stored procedures;
- filesystem-based uploads and generated documents.

The legacy system is tightly connected: UI, validation, business rules, database access, file generation, and external calls can occur in the same request.

Verified legacy capability areas include:

- identity, users, permissions, and firm context;
- products, categories, catalogue, specifications, and images;
- customers and contacts;
- sales orders and fulfilment;
- transportation and delivery;
- quotations / RFQ;
- suppliers and purchasing;
- reporting / ledger;
- content and engagement;
- file/document generation;
- external integrations.

The legacy application is therefore a **source of business behavior and compatibility evidence**, not the architectural template for the new platform.

---

## 3. Actual Project Goal

This project is not merely:

```text
.NET Framework 4.8
        ↓
ASP.NET Core / .NET 10
```

and it is not a page-for-page rewrite:

```text
ASPX page
    ↓
equivalent REST endpoint
```

The actual direction is:

```text
Legacy LCA capabilities
        ↓
Understand required behavior
        ↓
Rebuild in modern architecture
        ↓
Make capabilities tenant-aware
        ↓
Support multiple businesses
```

**Migration is the mechanism. The multi-tenant SaaS platform is the target product.**

---

## 4. Confirmed Technology Direction

```text
Frontend:       Next.js
Backend:        ASP.NET Core / .NET 10
Database:       SQL Server
Initial style:  Modular monolith / modular platform
Migration:      Incremental strangler-style modernization
```

SQL Server is now the confirmed target database. Older documents that describe the target database as undecided are superseded.

---

## 5. Business / Actor Model

Use this mental model:

```text
C  = Platform Owner
B1 = Tenant / Business 1
B2 = Tenant / Business 2
B3 = Tenant / Business 3
A  = Customer / Consumer
```

Conceptually:

```text
                 PLATFORM OWNER (C)
                        |
                 LCA SaaS Platform
                        |
        +---------------+---------------+
        |               |               |
     Tenant B1       Tenant B2       Tenant B3
        |               |               |
   Business Data   Business Data   Business Data
   + Operations    + Operations    + Operations
```

Each tenant operates on the shared platform but must only access the data and capabilities it is authorized to use.

---

## 6. Existing LCA Business as the First Tenant

The current LCA / Laxmi Ceramic business should be modeled as an **initial tenant** of the new platform.

The same organization also operates the platform, but platform access and Tenant 1 access use separate application identities in the current scope:

```text
Platform-owning organization
        |
        +-- Dedicated platform-owner ApplicationUser
        |     +-- PlatformAdmin role
        |     +-- no TenantMembership
        |
        +-- Tenant: Existing LCA business
              +-- Separate tenant ApplicationUser
              +-- active TenantMembership for Tenant 1
              |
              +-- Products
              +-- Customers
              +-- Pricing
              +-- Inventory
              +-- Orders
              +-- Quotations
              +-- Purchasing
              +-- Reports / other operations
```

The existing business should use the same tenant-aware architecture as future businesses rather than permanent special-case logic.

A single `ApplicationUser` must not hold both platform-owner access and tenant membership in the current scope. Both account types use the same ASP.NET Core Identity store and database.

---

## 7. Feature-Parity Principle

Every new tenant should eventually receive the **applicable business capabilities of the existing LCA application**.

The legacy system therefore defines the initial feature-parity baseline.

Examples include:

```text
Products / Catalogue
Categories
Pricing
Inventory
Customers / Contacts
Sales Orders
Purchase Orders
RFQ / Quotations
Transportation / Delivery
CRM-related operations
Reports / Ledger
Files / Business Documents
Required existing integrations
```

This does **not** mean copying legacy pages, tables, SQL patterns, or code organization.

> Preserve required business capability and behavior; redesign its implementation for the target architecture.

---

## 8. Multi-Tenancy Principle

Multi-tenancy is a backend/data-ownership requirement, not just a UI concern.

One tenant must not automatically see another tenant's protected:

- products;
- pricing;
- inventory;
- customers;
- orders;
- quotations;
- purchasing data;
- CRM information;
- reports;
- tenant-specific files.

Tenant isolation must be enforced by the backend and persistence model.

Detailed tenancy and schema rules belong in:

```text
.agents/codex/03-DATA-AND-TENANCY.md
```

---

## 9. Tenant Signup and Subscription Direction

The target SaaS business model includes a future flow such as:

```text
Business visits platform
        ↓
Creates account
        ↓
Creates/configures tenant
        ↓
Chooses available plan
        ↓
Pays/subscribes
        ↓
Uses LCA capabilities
```

Tenant signup, plans, subscription/payment, tenant lifecycle, and platform-owner administration are part of the **target product direction**, but they are not automatically part of the current implementation milestone.

---

## 10. Customer-Facing Storefront

Customer-facing commerce is part of the broader product direction.

However, the exact rule for whether every tenant receives its own storefront, domain, or identical customer-facing feature set is **not finalized and may depend on plan/product decisions later**.

Do not hard-code an assumption that every tenant automatically receives a separate storefront deployment.

---

## 11. Tenant Data Onboarding / Ingestion

The new platform should initially reproduce the **data-entry and ingestion capabilities already verified in the legacy system**, rather than expanding scope with new integrations.

Verified legacy patterns include:

- manual application data entry;
- Excel/XLS/XLSX bulk import;
- existing JSON/API-based write flows;
- relevant existing mobile/application write flows.

The legacy Excel importer is useful evidence for current datasets, mappings, and operational expectations.

Current scope does **not** add unsupported ingestion mechanisms such as:

- Tally / TallyPrime;
- generic ERP connectors;
- new CSV ingestion solely because it is common;
- arbitrary configurable import frameworks;
- unrelated accounting integrations.

If a new ingestion source is requested later, treat it as a separate approved capability.

---

## 12. Platform Owner

The platform owner operates the overall SaaS platform.

Future platform-level capabilities may include:

- tenant/business management;
- tenant status/lifecycle;
- tenant users;
- plans/subscriptions;
- platform configuration;
- feature entitlements;
- audit;
- platform reports;
- integration/AI policy management.

The detailed platform-owner dashboard is later scope unless explicitly prioritized.

---

## 13. Logical Target Capability Areas

Long-term logical capability areas may include:

```text
Identity & Access
Tenancy
Customer
Product / Catalogue
Pricing
Inventory
Commerce
RFQ / Quotation
CRM
Payment
Logistics
Notification
Reporting / Analytics
Audit
Integration Hub
Search
Product Intelligence / AI
Marketing
```

These are **logical boundaries**, not instructions to deploy each as a microservice.

The initial architecture remains a modular platform / modular monolith.

Detailed architecture belongs in:

```text
.agents/codex/01-TARGET-ARCHITECTURE.md
```

---

## 14. AI and Search Context

AI/search capabilities are part of the broader target direction, but:

- AI must not become the source of truth for price, stock, order, payment, or shipment state;
- AI-specific agents/orchestration are not automatically part of the current migration scope;
- authoritative commerce APIs and tenant-safe data access come first.

Do not introduce AI-specific architecture into a migration slice unless explicitly requested.

---

## 15. How to Use the Legacy Application

Treat the legacy application as:

```text
Functional baseline
+ business-rule evidence
+ data-model evidence
+ integration evidence
+ compatibility constraint
```

Do not treat it as:

```text
target code structure
target security model
target database design
```

A legacy capability may span ASPX markup, code-behind, helpers, SQL tables, stored procedures, triggers, files, APIs, and integrations.

Therefore ask:

> What business capability does this legacy behavior implement, and how should it exist safely in the new tenant-aware platform?

Do not ask:

> How do we translate this page line-by-line?

---

## 16. Migration Principle

The modernization is incremental:

```text
Legacy capability
       ↓
Characterize verified behavior
       ↓
Map to target capability/module
       ↓
Design tenant-aware API/data ownership
       ↓
Implement in ASP.NET Core / Next.js
       ↓
Validate parity and compatibility
       ↓
Move ownership to new platform
       ↓
Retire legacy path when safe
```

Detailed migration rules belong in:

```text
.agents/codex/04-LEGACY-MIGRATION.md
```

---

## 17. Finalized vs Unresolved

### Accepted direction

- ASP.NET Core / .NET 10 backend.
- Next.js frontend.
- SQL Server target database.
- Multi-tenant SaaS target.
- Modular monolith initially.
- Incremental strangler migration.
- Existing LCA business becomes an initial tenant.
- Applicable legacy LCA capabilities define the initial parity baseline.
- Existing ingestion capabilities are reproduced before adding new types.
- Tenant signup + paid subscription are part of the target business model, not automatically current scope.

### Not yet finalized

- exact tenant storefront packaging and plan-dependent behavior;
- subscription plans and billing rules;
- platform-owner dashboard UX/permission details;
- future AI/search deployment boundaries;
- new ingestion sources not verified in legacy;
- future microservice extraction.

Do not silently turn unresolved items into implementation assumptions.

---

## 18. Canonical Mental Model

```text
                    EXISTING LCA
            ASP.NET Web Forms / .NET 4.8
                      SQL Server
                          |
                behavior/data evidence
                          |
                          v
               NEW LCA SaaS PLATFORM
            ASP.NET Core / .NET 10
                    Next.js
                   SQL Server
               Modular Monolith
                          |
        +-----------------+-----------------+
        |                 |                 |
  Existing LCA       Future Tenant     Future Tenant
     Tenant               B2                B3
        |                 |                 |
        +-----------------+-----------------+
                          |
              Tenant-aware capabilities
                          |
 Product / Customer / Pricing / Inventory / Orders
       RFQ / Purchasing / CRM / Reporting
                          |
               Supporting capabilities
                          |
       Integrations / Search / AI / etc.
```

**The target is not a modernized single-company LCA website. The target is a reusable multi-tenant LCA commerce platform that preserves required existing capabilities while allowing additional businesses to use them safely.**
