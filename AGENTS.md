# AGENTS.md — LCA Web & Backend Migration

## 1. Purpose

This file defines how AI coding agents must work in this repository.

LCA is a **legacy migration and modernization project**, not a greenfield rewrite.

The user’s implementation scope in this repository is limited to:

- Web frontend
- ASP.NET Core / .NET backend
- Shared secured APIs
- Authentication and authorization
- Multi-tenancy
- Core commerce/business modules
- Legacy SQL Server migration and reconciliation
- Backend integrations required by the web/backend scope
- Testing, compatibility, and strangler migration work

AI-agent features, Android/mobile implementation, and unrelated long-term platform work are outside the current implementation scope unless the user explicitly reopens them.

---

## 2. Repository Documentation

Before planning or coding, inspect the relevant documentation.

Current documentation structure:

```text
/
├── AGENTS.md
├── README.md
└── docs/
    ├── README.md
    ├── ARCHITECTURE.md
    ├── Architecture-principle.jpg
    ├── ER-diagram.jpg
    ├── main-business-flow.jpg
    ├── MIGRATION_STRATEGY.md
    ├── LCA_2.5_Month_PRD.md
    ├── Legacy_docs/
    │   ├── 00-phase1-discovery-status.md
    │   ├── FINAL-PHASE1-RECONCILIATION.md
    │   ├── MASTER-APPLICATION-FLOW.md
    │   ├── MASTER-DATABASE-SCHEMA.md
    │   └── MASTER-REPOSITORY-INVENTORY.md
    └── database/
        ├── README.md
        ├── LEGACY_SCHEMA_INVENTORY.md
        ├── KEYS_RELATIONSHIPS_AND_INDEXES.md
        ├── PROGRAMMABLE_OBJECTS.md
        └── docs/
            ├── database-columns.txt
            ├── database-primary-keys.txt
            ├── database-foreign-keys.txt
            ├── database-indexes.txt
            ├── database-stored-procedures.sql
            ├── database-triggers.sql
            └── database-functions.sql
```

Do not invent additional documentation requirements unless the task genuinely needs them.

---

## 3. Required Reading Order

For general work:

1. `README.md`
2. `docs/README.md`
3. `docs/LCA_2.5_Month_PRD.md`
4. `docs/ARCHITECTURE.md`
5. `docs/MIGRATION_STRATEGY.md`

For database, persistence, repository, SQL, schema, migration, or reconciliation work, also read:

6. `docs/database/README.md`
7. `docs/database/LEGACY_SCHEMA_INVENTORY.md`
8. `docs/database/KEYS_RELATIONSHIPS_AND_INDEXES.md`
9. `docs/database/PROGRAMMABLE_OBJECTS.md`
10. `docs/Legacy_docs/MASTER-DATABASE-SCHEMA.md`
11. `docs/Legacy_docs/FINAL-PHASE1-RECONCILIATION.md`

Do not start database implementation from the conceptual architecture alone.

---

## 4. Scope Source of Truth

The PRD is the main product/scope reference for the migration.

However, this repository’s current implementation responsibility is narrower than the entire PRD.

### In scope here

- Existing ASPX web migration
- Web frontend rebuild
- ASP.NET Core / .NET backend migration
- Shared API layer
- Authentication
- Authorization
- Tenant/business context
- Product/catalogue
- Customer
- Pricing
- Inventory
- Cart
- Orders
- RFQ / quotation
- Search where required by the migrated web experience
- Database migration
- Data reconciliation
- Required backend integrations
- Testing
- Legacy/new-system coexistence
- Cutover preparation

### Out of scope here

Do not implement unless the user explicitly asks:

- AI Gateway
- AI agents
- AI orchestration
- LLM integration
- AI Analytics Chatbot
- AI Media Agent
- AI Marketing Agent
- AI Logistics Agent
- Product Onboarding AI automation
- Embeddings/vector-AI work
- Android / Flutter application
- unrelated long-term roadmap capabilities

The PRD may mention those capabilities because it covers the broader LCA project. Their presence in the PRD does not make them part of this repository’s current implementation work.

---

## 5. Current Technical Direction

### Legacy system

- Existing application: ASPX / legacy .NET-era web application
- Legacy database: Microsoft SQL Server / T-SQL
- Existing system must remain operational during progressive migration

### Target backend

- .NET 10
- ASP.NET Core
- Modular-first architecture

### Target web frontend

- React
- Next.js
- TypeScript

### Database

The source database is SQL Server.

The final target database engine must not be assumed unless explicitly decided by the project.

The PRD refers to the target store as:

```text
PostgreSQL / SQL Server
```

Therefore do not hard-code a final database-engine decision unless the user or repository explicitly establishes one.

---

## 6. Architectural Direction

The target should begin as a **modular application / modular monolith**.

Do not create many microservices by default.

Prefer clear module boundaries inside one deployable backend unless there is a concrete reason to extract a service.

Possible extraction reasons include:

- independent scaling
- failure isolation
- deployment independence
- strong ownership boundary
- regulatory/compliance requirement
- materially different runtime characteristics

Do not introduce distributed-system complexity before it is justified.

---

## 7. Strangler Migration Rule

This is a migration project.

Do not replace the legacy system all at once.

Preferred approach:

```text
Legacy capability remains operational
        ↓
Understand current behavior
        ↓
Introduce new backend/API path
        ↓
Move selected traffic/use cases
        ↓
Compare and reconcile results
        ↓
Expand ownership
        ↓
Retire legacy path only after verification
```

Before replacing any legacy capability:

1. inspect the existing implementation;
2. identify the relevant database objects;
3. determine current inputs, outputs, and side effects;
4. preserve required business behavior;
5. implement the smallest compatible target slice;
6. verify against legacy behavior;
7. reconcile data/results;
8. keep rollback/coexistence possible until accepted.

A cleaner implementation is not enough reason to change legacy behavior.

---

## 8. Multi-Tenancy

LCA is a multi-tenant platform.

Core actors:

- Platform Owner — C
- Business / Tenant — B1, B2, B3...
- Customer / Consumer — A

Tenant isolation must be enforced by the backend.

Do not rely on frontend filtering.

For tenant-scoped operations:

- derive tenant context from trusted authentication/authorization context;
- explicitly scope data access;
- explicitly scope search;
- explicitly scope caches if used;
- explicitly scope background jobs/events if introduced;
- add tests proving cross-tenant access is denied.

Never trust a client-provided tenant identifier by itself.

---

## 9. Authentication and Authorization

Keep authentication and authorization separate.

Authentication answers:

> Who is this user?

Authorization answers:

> What is this user allowed to do in this tenant/context?

Do not infer the final target permission model directly from legacy flags such as:

- `IsAdmin`
- page-level booleans
- legacy area/group permission columns

Legacy permissions must first be understood and mapped.

---

## 10. Legacy Database Is Evidence, Not a Suggestion

The database documentation under `docs/database/` is based on actual exported SQL Server metadata.

Current verified structural snapshot:

- 127 tables
- 1,297 columns
- 92 tables with declared primary keys
- 35 tables without declared primary keys
- 2 composite primary keys
- 6 declared foreign-key constraints
- 101 distinct index names
- 9 non-primary-key indexes
- 38 stored procedure names
- 1 trigger
- 5 functions

These are observed facts from the supplied exports.

If later direct inspection of the restored database produces different results, update the documentation rather than silently choosing one version.

---

## 11. Database No-Hallucination Rules

### 11.1 Do not infer relationships from names

Matching names such as:

- `CustomerID`
- `OrderID`
- `ItemCode`
- `Productid`
- `SupplierID`
- `Transportation_Id`
- `User_ID`

do not prove a foreign-key relationship.

Only treat a relationship as declared when it is present in the actual database evidence.

Possible relationships discovered from code/data may be recorded as candidates until verified.

### 11.2 Do not automatically redesign the schema

Do not automatically:

- add primary keys
- add foreign keys
- rename columns
- normalize tables
- merge similar tables
- change nullability
- add uniqueness
- change data types
- remove duplicate-looking structures

First establish:

- current usage
- legacy compatibility
- actual data quality
- migration impact

### 11.3 Preserve unusual evidence

Some exported FK definitions appear unusual.

Do not silently “fix” them because another relationship looks more logical.

Recheck the restored database and record the discrepancy.

---

## 12. Programmable Database Objects

The currently supplied stored procedure, function, and trigger exports are incomplete/truncated for most objects.

They are authoritative for:

- object names
- exact visible SQL fragments
- visible dependencies only

They are not sufficient for reconstructing complete business logic.

Current inventory:

- 38 stored procedures
- 1 trigger: `dbo.tg_Mobile_ItemMaster`
- 5 functions:
  - `dbo.FlattenedJSON`
  - `dbo.fnSplitString`
  - `dbo.getLocationName`
  - `dbo.getRoodId`
  - `dbo.SplitString`

Visible procedure fragments also reveal user-defined/table-type dependencies such as:

- `CUSTOMER`
- `RECEIVABLE`

Do not invent missing SQL behavior.

Before rewriting active stored logic:

1. obtain the full SQL definition where possible;
2. locate callers in the legacy application;
3. identify reads/writes and side effects;
4. identify transaction boundaries;
5. capture representative behavior;
6. add characterization tests;
7. decide whether to preserve SQL temporarily or move behavior into .NET;
8. compare target behavior with legacy behavior.

---

## 13. Database Migration and Reconciliation

The PRD requires migration with reconciliation against the legacy system.

Migration work should be:

- repeatable where practical;
- observable;
- auditable;
- restart-safe where practical;
- explicit about source/target identifiers;
- explicit about transformations;
- testable on production-like data.

For each migrated area, define reconciliation such as:

- source row count;
- target row count;
- rejected-row count;
- identifier mapping;
- aggregate totals where meaningful;
- conversion failures;
- orphan detection;
- sample record comparison;
- business-level validation.

Do not declare migration complete merely because an import script ran successfully.

---

## 14. Data-Type Migration

The legacy schema contains many nullable and string-heavy columns.

Do not convert a legacy string field into a target:

- date
- number
- boolean
- enum
- UUID
- foreign key

without profiling actual values first.

For any conversion, define:

- valid source patterns;
- invalid values;
- null/empty behavior;
- conversion rule;
- rejected-row policy;
- reconciliation rule.

Schema appearance alone is not sufficient.

---

## 15. Shared API Layer

The web frontend must use the shared secured backend APIs.

Do not allow the frontend to connect directly to the database.

API design should be consistent around:

- authentication;
- authorization;
- validation;
- pagination;
- filtering;
- error handling;
- versioning where required;
- tenant context;
- correlation/observability where appropriate.

Do not duplicate core business data into separate frontend-owned stores.

---

## 16. Core Business Data

Critical business facts must come from backend/business services and authoritative data.

Examples:

- Product / SKU
- Price
- Inventory / stock
- Customer
- Order
- RFQ / quotation
- Payment state if handled in scope
- Shipment state if handled in scope

Do not create duplicate sources of truth without an explicit migration/compatibility reason.

---

## 17. Integrations

Keep external provider logic behind clear backend boundaries/adapters.

Relevant integration categories may include:

- payment providers
- logistics / courier / 3PL
- ERP / WMS
- WhatsApp
- email

Do not leak provider-specific SDK details into core domain logic.

For webhook/external-event processing where applicable:

- verify authenticity;
- make processing idempotent;
- handle duplicates;
- define retries;
- persist provider references;
- add operational visibility.

---

## 18. Events and Background Work

Use asynchronous events/background work only when they materially help the use case.

Do not introduce an event bus merely because the long-term architecture mentions event-driven workflows.

Prefer synchronous logic when it is simpler and correct.

If events/background jobs are introduced, make them:

- idempotent;
- retry-safe;
- observable;
- tenant-aware where relevant;
- recoverable after failure.

Never assume exactly-once delivery.

---

## 19. Search

Search may be part of the migrated web/backend scope.

If implemented:

- search results must respect tenant/customer visibility;
- search is not authoritative for price, stock, order state, or other transactional facts;
- business-critical values should be resolved from authoritative backend services/data.

Do not implement future AI/vector/image/voice search unless explicitly requested.

---

## 20. Testing Expectations

Use the appropriate combination of:

- unit tests;
- integration tests;
- API tests;
- tenant-isolation tests;
- authorization tests;
- database migration tests;
- characterization tests against legacy behavior;
- reconciliation tests;
- idempotency tests;
- end-to-end tests for critical web flows.

For migration work, a test proving legacy/target compatibility is often more valuable than one proving only internal correctness of the new implementation.

---

## 21. Implementation Workflow

Before coding:

### Step 1 — Inspect

Inspect:

- repository structure;
- relevant docs;
- existing implementation;
- legacy implementation where available;
- relevant database evidence;
- tests.

### Step 2 — Separate facts from unknowns

State:

- verified facts;
- target requirements;
- assumptions;
- open questions.

Do not hide assumptions inside code.

### Step 3 — Plan a small slice

Define:

- files/modules affected;
- legacy compatibility boundary;
- authoritative data;
- tenant/security impact;
- API contract;
- migration/reconciliation impact;
- tests;
- rollback/coexistence concerns.

### Step 4 — Implement incrementally

Prefer small, reviewable, testable changes.

Do not scaffold the entire long-term architecture at once.

### Step 5 — Verify

Run the relevant:

- build;
- tests;
- lint/format checks;
- database checks;
- reconciliation checks.

---

## 22. Security

Do not commit:

- `.bak` production database backups;
- secrets;
- API keys;
- passwords;
- production connection strings with credentials;
- customer/business-sensitive exports unless sanitized and approved.

Treat legacy database artifacts as sensitive.

Do not expose secrets or sensitive values in logs.

---

## 23. Evidence Labels

When documenting migration discoveries, use:

- **Declared** — directly present in database/source evidence
- **Observed** — verified through actual code/data/runtime behavior
- **Candidate** — plausible but not yet verified
- **Target Requirement** — required by PRD/approved architecture
- **Target Proposal** — suggested future design
- **Unknown** — insufficient evidence

Never promote a Candidate into a fact without new evidence.

---

## 24. When to Stop Instead of Guessing

Do not guess when missing information can materially affect:

- data integrity;
- tenant isolation;
- authorization;
- order/pricing/inventory behavior;
- migration transformations;
- target database design;
- legacy compatibility;
- external side effects.

Surface the gap and continue only with work that does not depend on it.

---

## 25. Prohibited Agent Behavior

Do not:

- treat LCA as a greenfield rewrite;
- implement AI features in this repository unless explicitly requested;
- implement Android/mobile work unless explicitly requested;
- invent legacy schema or relationships;
- infer foreign keys from matching names;
- reconstruct missing stored-procedure logic;
- silently redesign the legacy schema;
- choose the final target database engine without a decision;
- expose cross-tenant data;
- create microservices without justification;
- remove legacy paths before verification;
- declare migration complete without reconciliation;
- commit production backups or secrets;
- generate large unrelated scaffolding when the user asks for a small step.

---

## 26. Definition of Done for a Migrated Slice

A migrated slice is complete only when the applicable items are satisfied:

- target behavior implemented;
- authentication/authorization applied;
- tenant isolation verified;
- legacy behavior understood;
- migration/transformation tested;
- reconciliation completed;
- relevant integration behavior verified;
- tests pass;
- coexistence/rollback impact understood;
- no critical unexplained divergence remains.

---

## 27. Core Principle

The objective is not to copy the legacy implementation blindly and not to redesign the system from imagination.

The objective is to:

**understand the existing system accurately, preserve required business behavior, migrate the web and backend incrementally, reconcile data and behavior, and retire legacy paths only after the new implementation is demonstrably correct.**
