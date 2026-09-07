# LCA Target Architecture

## Purpose

This file defines the approved **target architecture direction** for new LCA implementation.

It answers:

> What architectural shape should migrated/new capabilities move toward?

It does not define current repo status, the physical schema, coding conventions, or sprint tasks.

Related files:

- `00-PROJECT-CONTEXT.md` — what LCA is and why it is being rebuilt.
- `02-CURRENT-STATE.md` — what already exists in this repository.
- `03-DATA-AND-TENANCY.md` — detailed tenant/data ownership rules.
- `04-LEGACY-MIGRATION.md` — how legacy behavior is migrated.

---

## 1. Architecture Objective

LCA is being rebuilt as a **multi-tenant SaaS commerce platform**, not as a page-for-page Web Forms rewrite.

The target path is:

```text
Legacy capability
      ↓
Characterize required behavior
      ↓
Map to target business capability
      ↓
Make it tenant-aware
      ↓
Expose through ASP.NET Core APIs
      ↓
Consume through Next.js / approved clients
```

The existing LCA business becomes an initial tenant. Future businesses use the same platform architecture.

---

## 2. Initial Architectural Style

The initial backend is a **modular monolith / modular platform**.

```text
              One ASP.NET Core Application
                         |
     +-------------------+-------------------+
     |                   |                   |
  Identity            Product            Commerce
  Tenancy             Pricing            RFQ
  Customer            Inventory          CRM
     |                   |                   |
     +-------------------+-------------------+
                         |
                     SQL Server
```

Logical module boundaries should be clear, but they do not require separate deployables or separate `.csproj` files.

Do not create microservices unless later justified by real requirements such as:

- independent scaling;
- operational ownership;
- strong isolation requirements;
- regulatory/security boundaries;
- materially different deployment needs.

---

## 3. Backend Solution Shape

The preferred initial solution shape is:

```text
Lca.Api
Lca.Core
Lca.Infrastructure
```

This structure may evolve if there is a concrete reason, but do not over-segregate the solution.

### `Lca.Api`

Owns HTTP/hosting concerns:

- ASP.NET Core startup/composition;
- controllers/endpoints;
- middleware;
- authentication pipeline;
- authorization policies;
- tenant-context integration;
- dependency injection;
- HTTP contracts/versioning where needed.

It should not contain core business rules or direct SQL.

### `Lca.Core`

Owns application and business behavior:

- use cases/application services;
- domain rules and models;
- business interfaces/abstractions;
- logical module boundaries.

Expected logical modules include:

```text
Identity
Tenancy
Product / Catalogue
Category
Pricing
Inventory
Customer
Commerce / Orders
RFQ / Quotation
Purchasing
CRM
Logistics
Reporting
```

These are logical boundaries, not instructions to create one project per module.

### `Lca.Infrastructure`

Owns technical implementations:

- SQL Server persistence;
- EF Core / approved data access;
- Redis;
- Search;
- files/storage;
- provider adapters;
- event/message transport when approved;
- other infrastructure concerns.

---

## 4. Dependency Direction

Prefer this dependency model:

```text
Lca.Api
   |
   v
Lca.Core
   ^
   |
Lca.Infrastructure
```

Core should not depend directly on SQL Server-specific, Redis-specific, search-provider-specific, or external-provider-specific implementation details.

Avoid cyclic dependencies.

---

## 5. High-Level Platform Topology

```text
                     Platform Owner
                           |
                  LCA Multi-Tenant Platform
                           |
          +----------------+----------------+
          |                |                |
       Tenant B1        Tenant B2        Tenant B3
          |                |                |
          +----------------+----------------+
                           |
                   Shared Secured APIs
                           |
          +----------------+----------------+
          |                                 |
   Tenant/Admin UI                    Customer UI
          |                                 |
          +----------------+----------------+
                           |
                 Business Capabilities
                           |
 Product | Pricing | Inventory | Orders | Customer | RFQ | CRM
                           |
        Search | Integrations | Reporting | AI support
                           |
           Payment | Logistics | ERP/WMS as approved
```

AI/search/integrations support the platform; they do not become authoritative owners of core commerce facts.

---

## 6. Frontend Direction

The initial web architecture uses **one Next.js application** for all current web-facing roles.

Conceptually:

```text
Next.js
├── Customer-facing routes
├── Tenant / Business Admin routes
└── Platform Admin routes
```

Authentication, authorization, and tenant/platform context determine access.

Do not split these into multiple Next.js applications unless there is a later concrete reason.

The platform-admin UI is later scope unless explicitly prioritized.

Future mobile clients should use the same secured backend APIs where appropriate.

---

## 7. Standard Request Flow

```text
Client
  ↓
ASP.NET Core REST API
  ↓
Authentication
  ↓
Trusted Tenant / Platform Context
  ↓
Authorization
  ↓
Application Use Case
  ↓
Business Capability
  ↓
Infrastructure
  ↓
SQL Server / Redis / Search / Integration
```

Key rules:

- endpoints/controllers stay thin;
- tenant context is established before tenant-owned data access;
- authorization is server-side;
- business logic is not placed in Next.js;
- new code does not reproduce legacy page-to-database coupling.

---

## 8. API Direction

New migrated capabilities should primarily expose **ASP.NET Core REST APIs**.

These APIs are the shared backend boundary for:

- Next.js;
- future mobile clients;
- intentionally migrated existing clients;
- approved integrations.

Do not recreate ASPX page-based API architecture.

Temporary compatibility endpoints may exist during strangler migration when required by verified consumers.

---

## 9. Multi-Tenant Persistence

The approved initial data model is:

```text
One SQL Server database
+
Shared schema
+
TenantId on tenant-owned data
```

Not:

```text
one database per tenant
```

for the initial architecture.

Tenant-owned areas include, as applicable:

```text
Products
Pricing
Inventory
Customers
Orders
Quotations
Purchasing
CRM
Tenant-specific reports/files
```

Detailed keys, constraints, ownership rules, and tenant-resolution behavior belong in `03-DATA-AND-TENANCY.md`.

---

## 10. SQL Server

SQL Server is the confirmed target database.

This supersedes older documents where the database engine was undecided.

Verified legacy stored procedures or trigger behavior may temporarily remain as explicit compatibility boundaries, but only when characterized and intentionally retained.

Do not preserve legacy database coupling by default.

---

## 11. Redis and Search

Both **Redis and Search are part of the approved initial platform architecture**.

### Redis

Use only for concrete needs such as:

- caching;
- short-lived coordination/state;
- rate-limiting or supporting infrastructure;
- explicit performance requirements.

Do not cache everything merely because Redis exists.

Authoritative business state remains in its approved source of truth.

### Search

Search may support:

- keyword and filtered search;
- product discovery;
- future semantic/vector capabilities;
- future multilingual/image/voice search.

Introduce only the search behavior required by approved slices.

Search indexes are derived data and must respect tenant/customer visibility.

---

## 12. Authoritative Business Data

Core facts must have explicit ownership.

```text
Product / SKU   → Product/Catalogue
Price           → Pricing
Stock           → Inventory
Customer        → Customer
Order           → Commerce
Quotation/RFQ   → RFQ/Quotation
Payment state   → Payment
Shipment state  → Logistics
```

AI, Search, Redis, analytics, or generated files must not silently become the source of truth for these facts.

---

## 13. Event-Driven Architecture

Event-driven architecture is part of the target direction, but it must be introduced **deliberately**.

### When legacy behavior already requires it

If a migrated legacy capability already depends on asynchronous processing, scheduled processing, durable messaging, or equivalent event-like behavior, preserve that behavior appropriately.

Do not remove required asynchronous behavior during migration.

### When event-driven design is newly recommended

If Codex identifies a legacy synchronous flow that would be better represented using events/background processing, it should:

1. identify the opportunity;
2. explain the benefit;
3. explain consistency, retry, and idempotency implications;
4. propose the architectural change;
5. **not introduce new event infrastructure automatically without approval**.

Example:

```text
OrderCreated
├── Notification
├── Reporting
└── CRM update
```

may be a good target pattern, but a new event bus is an architectural decision.

### If events are approved

Tenant-aware business events must carry tenant context where relevant and consumers should be:

- idempotent;
- retry-safe;
- recoverable;
- compatible with dead-letter/recovery handling when durable messaging is used.

---

## 14. Integrations

Keep provider-specific behavior behind infrastructure/integration boundaries.

```text
Business Capability
       ↓
Integration Contract
       ↓
Provider Adapter
       ↓
External Provider
```

Examples may include:

```text
Payment
Logistics
Email
WhatsApp/SMS
ERP/WMS
Remote content/services
```

Only integrations required by verified legacy parity or explicit new scope should be implemented.

---

## 15. Data Ingestion Direction

Initially reproduce only verified legacy ingestion/data-entry capabilities:

```text
Manual entry
Excel/XLS/XLSX import
Existing JSON/API writes
Relevant existing application/mobile writes
```

Do not add:

```text
Tally/TallyPrime
generic ERP ingestion
CSV solely because it is common
generic mapping/import platforms
```

unless separately approved.

The target implementation should route ingestion through tenant-aware backend behavior rather than direct UI-to-table writes.

```text
Next.js / Approved Client
        ↓
ASP.NET Core API
        ↓
Tenant Context + Authorization
        ↓
Validation / Import Logic
        ↓
Business Capability
        ↓
SQL Server
```

Do not build a large generic ingestion framework before an actual migration slice requires it.

---

## 16. Files and Generated Documents

Legacy uploads, images, import artifacts, PDFs, and predictable URLs may be business contracts.

When their owning capability is migrated:

- identify active consumers;
- preserve compatibility as required;
- avoid blindly continuing web-root storage assumptions;
- do not rename/move paths until dependencies are understood.

---

## 17. Logical Module Direction

```text
Foundation
├── Identity
├── Tenancy
└── Shared cross-cutting concerns

Core Data
├── Product / Catalogue
├── Category
├── Pricing
├── Inventory
└── Customer

Commerce
├── Orders
├── RFQ / Quotation
├── Purchasing
└── Logistics

Operational / Supporting
├── CRM
├── Reporting
├── Search
├── Notifications / Integrations
├── Payment
└── AI / Product Intelligence when approved
```

Do not create abstractions with no current use just to mirror an enterprise diagram.

---

## 18. Migration Dependency Direction

The existing AI-agent-team migration recommendation is useful as a **dependency guide**, not an unquestionable implementation plan.

Current architectural order is approximately:

```text
Foundation
├── Database access
├── Configuration
├── Authentication
├── Authorization
└── Trusted tenant context
        ↓
Core Data
├── Product
├── Category
├── Customer
├── Pricing
└── Inventory
        ↓
Commerce
├── Orders
├── Quotations / RFQ
├── Purchasing
└── Logistics
        ↓
Reports / CRM
```

Before implementing a capability:

- verify its legacy behavior;
- identify dependencies and side effects;
- reconcile any old recommendation with the current multi-tenant architecture and current scope.

Existing AI-related tables or prior groundwork do not make AI migration the current business priority.

---

## 19. Architecture Evolution

### Initial target

```text
ASP.NET Core modular monolith
+
SQL Server shared-schema multi-tenancy
+
Redis
+
Search
+
single Next.js application
```

### Potential later evolution

Only when justified:

- background workers;
- durable event bus;
- independently scaled Search/AI workers;
- extracted high-load modules;
- extracted Payment/Logistics/CRM services;
- dedicated mobile clients.

Future-state diagrams are direction, not automatic implementation scope.

---

## 20. Migration Architecture Rules

### Preserve

- verified business rules;
- required data behavior;
- active integration contracts;
- required file/document behavior;
- required asynchronous behavior;
- verified external compatibility.

### Redesign

- tenant isolation;
- authentication/authorization;
- API boundaries;
- persistence boundaries;
- dependency direction;
- transaction/error handling;
- provider isolation;
- observability and testability.

### Do not copy blindly

- direct SQL from UI/page code;
- ASP.NET Session as the new security model;
- single-company/global assumptions;
- page-specific API architecture;
- shared web-root storage assumptions;
- weak/inconsistent authorization;
- synchronous side effects solely because legacy uses them.

---

## 21. Explicit Initial Non-Goals

Unless an approved task requires them, do not introduce:

- database-per-tenant;
- microservices for every module;
- Kubernetes-specific architecture;
- a separate API gateway product;
- Tally/TallyPrime;
- generic ERP ingestion;
- unsupported new ingestion types;
- AI-agent orchestration;
- independently deployed AI services;
- multiple Next.js applications;
- platform-owner dashboard implementation;
- speculative billing infrastructure;
- an event bus solely because event-driven architecture is desirable.

Codex should raise relevant architecture opportunities for discussion rather than silently expanding scope.

---

## 22. Canonical Mental Model

```text
                       Single Next.js App
                              |
                    ASP.NET Core REST APIs
                              |
                       Authentication
                              |
                    Trusted Tenant Context
                              |
                        Authorization
                              |
                    Application Use Cases
                              |
        +---------------------+---------------------+
        |                     |                     |
     Product              Commerce             Customer
     Pricing              RFQ/Quote              CRM
    Inventory             Purchasing           Logistics
        |                     |                     |
        +---------------------+---------------------+
                              |
                       Infrastructure
                              |
              +---------------+---------------+
              |               |               |
          SQL Server        Redis           Search
                                              |
                                  Approved Integrations
```

The central architectural invariant is:

> **Business capabilities are shared by tenants; protected tenant data and authorization context are not.**
