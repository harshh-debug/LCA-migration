# LCA Implementation Rules

## Purpose

This document defines the coding and implementation guardrails Codex must follow while working in the LCA migration repository.

It answers:

> How should new code be implemented so that it stays consistent with the accepted LCA architecture, migration strategy, and multi-tenant SaaS direction?

Related files:

- `00-PROJECT-CONTEXT.md` — what LCA is and what is being built.
- `01-TARGET-ARCHITECTURE.md` — target application architecture.
- `02-CURRENT-STATE.md` — what is actually implemented today.
- `03-DATA-AND-TENANCY.md` — tenant/data ownership rules.
- `04-LEGACY-MIGRATION.md` — how legacy capabilities are migrated.

These rules do not authorize work outside the current scope.

---

## 1. General Implementation Principle

Codex should prefer the **simplest implementation that satisfies the approved architecture and verified legacy behavior**.

Do not over-engineer.

Do not add architectural layers, projects, abstractions, packages, services, background infrastructure, or design patterns only because they are common in enterprise systems.

The default goal is:

```text
clear
testable
tenant-safe
maintainable
minimal
compatible with migration requirements
```

---

## 2. Existing Solution Structure

The preferred initial backend structure remains:

```text
Lca.Api
Lca.Core
Lca.Infrastructure
```

Do not create additional projects or split every module into separate assemblies unless there is a concrete requirement.

Logical modules may exist inside the current project boundaries.

Keep module boundaries understandable without over-segregating the codebase.

---

## 3. API Style

The current application is primarily controller-based.

Codex should preserve the existing style unless a specific task has a strong reason to use another ASP.NET Core endpoint style.

Default:

```text
ASP.NET Core Controllers
```

Do not start converting existing controller APIs to Minimal APIs merely for stylistic preference.

Do not mix endpoint styles unnecessarily.

---

## 4. Controller Responsibilities

Controllers should remain thin.

Preferred flow:

```text
Controller
   ↓
Application / Business Service
   ↓
Domain / Business Rules
   ↓
Persistence / Infrastructure
```

Controllers may handle:

- HTTP request binding;
- HTTP-specific validation/result mapping;
- authentication/authorization attributes;
- calling application services;
- returning API responses.

Controllers should not contain:

- direct SQL;
- large business workflows;
- provider-specific integration logic;
- tenant ownership decisions that belong in shared application/security infrastructure.

---

## 5. Persistence Strategy

### Default

Use **EF Core by default** for new target-platform persistence.

### Dapper

Dapper may be appropriate for cases such as:

- complex read/reporting queries;
- performance-sensitive read paths;
- compatibility with difficult legacy queries;
- specific SQL/procedure access where EF Core adds unnecessary complexity.

However:

> Codex must not introduce Dapper without first raising the requirement and getting approval.

When Codex believes Dapper is justified, it should explain:

1. which query/use case needs it;
2. why EF Core is insufficient or materially worse;
3. whether the use is temporary migration compatibility or target architecture;
4. how tenant isolation will be enforced.

Wait for approval before implementing Dapper.

---

## 6. SQL Server

SQL Server is the target database.

Do not introduce PostgreSQL, MySQL, SQLite, or another production database engine unless the accepted architecture changes explicitly.

Local/test tooling may use alternatives only if explicitly approved and if it does not hide SQL Server-specific behavior that matters to the slice.

---

## 7. Database Schema Changes

New target-schema changes should normally be represented through **EF Core migrations**.

Use raw SQL inside/alongside migrations only when there is a clearly documented reason, such as:

- SQL Server-specific constraint/index behavior;
- stored procedure/function creation;
- data migration that is impractical through normal EF operations;
- required compatibility logic.

Do not make undocumented manual schema changes.

Before generating or applying migrations, ensure the model reflects the accepted multi-tenant target rather than experimental old assumptions.

---

## 8. Legacy Stored Procedures

Stored procedures are an important part of verified legacy behavior.

For relevant migrated capabilities, the default expectation is to **preserve/reuse existing stored-procedure behavior unless there is a justified exception**.

Before relying on a legacy stored procedure:

- characterize what it reads and writes;
- understand important inputs/outputs;
- identify transactions and side effects;
- confirm tenant/data implications;
- confirm how it fits the separate new SQL Server migration model.

If reused temporarily against the legacy system, treat it as migration/coexistence compatibility logic.

If equivalent procedure behavior is moved into the new database, do so deliberately rather than blindly copying database code.

Do not replace a relevant stored procedure merely because equivalent C# could be written.

If there is a reason to replace or materially change stored-procedure behavior, raise it for review first.

---

## 9. Legacy Data Access

Legacy code patterns such as:

```text
SqlDataSource
direct SqlCommand in ASPX
string-built SQL
App_Code/db.cs
page-specific database writes
```

are evidence of behavior, not target implementation patterns.

Do not reproduce them directly in new application code.

Temporary legacy database access is permitted only as an explicit compatibility adapter for an approved migration slice.

---

## 10. Validation

Request and business validation must be explicit.

Do not rely solely on:

- SQL Server constraints;
- nullable/non-nullable columns;
- frontend checks;
- assumptions inherited from legacy UI.

Use appropriate validation at the API/application/domain boundary.

Validation should distinguish:

```text
invalid request
unauthorized action
forbidden tenant access
business-rule violation
not found
database/infrastructure failure
```

Do not expose internal database errors as normal API validation messages.

---

## 11. Authentication and Authorization

Every protected API must enforce security server-side.

Do not assume:

- hidden frontend buttons;
- hidden navigation;
- Next.js route guards;
- client-provided role values

provide authorization.

New APIs should use the approved authentication/authorization mechanisms from the backend.

Ambiguous legacy permission behavior must be raised for confirmation before changing its semantics.

For the current scope, platform and tenant access use separate `ApplicationUser` accounts in the same Identity store:

- platform accounts use `AccountType.Platform`, the `PlatformAdmin` Identity role, and no `TenantMembership`;
- tenant accounts use `AccountType.Tenant`, no platform role, and exactly one active `TenantMembership`;
- a single account combining both scopes must fail closed;
- tenant membership currently grants full access to implemented tenant capabilities, so do not introduce tenant roles or permission assignments prematurely.

---

## 12. Tenant Safety

Every tenant-owned operation must follow `03-DATA-AND-TENANCY.md`.

Minimum rule:

```text
Authenticated user
      ↓
trusted tenant_id
      ↓
validate tenant
      ↓
validate TenantMembership
      ↓
authorize
      ↓
tenant-scoped data operation
```

Never trust arbitrary request `TenantId` values for normal tenant operations.

Do not add a tenant-owned entity without determining:

- where `TenantId` lives;
- how it is assigned;
- how reads are scoped;
- what uniqueness is tenant-scoped.

---

## 13. Shared-Schema Target

The target model is:

```text
one SQL Server database
+
shared schema
+
TenantId-based ownership
```

Do not extend the current experimental per-tenant database-routing approach.

If existing code depends on that routing, identify it and propose the correction before building additional features on top of it.

---

## 14. Data Ownership

Every new entity must be classified as:

```text
Platform-owned
Tenant-owned
Global/shared reference data
```

Do not silently create globally shared business data because the legacy system was single-business.

---

## 15. API Contracts

New APIs should represent business capabilities, not legacy page names.

Prefer:

```text
/api/v1/products
/api/v1/orders
```

over page-shaped architecture such as:

```text
/api/v1/add-product-page
/api/v1/product-edit-page
```

Legacy-compatible contracts may temporarily exist where active consumers require them.

---

## 16. Legacy Inspection Rule

When a task requires legacy characterization:

1. start with the legacy `api/` folder;
2. identify the relevant endpoint(s);
3. follow only the dependencies needed to understand that capability;
4. inspect other areas only when evidence requires it.

Additional areas may include:

```text
root ASPX
App_Code
stored procedures
triggers
mobile
test-api
deepak
ASMX/WebMethods
files
integrations
```

Do not rescan the entire legacy repository for every migration task.

---

## 17. Business Behavior vs Technical Defects

Preserve required legacy business behavior.

Do not knowingly reproduce obvious technical/security defects.

However, if a proposed correction could change business behavior, especially around:

- permissions;
- roles;
- pricing;
- stock;
- orders;
- quotations;
- customer visibility;
- status transitions;
- business identifiers;

Codex must raise the case for review before changing behavior.

Do not make silent "cleanup" decisions.

---

## 18. One Writer Rule

During migration, one system should own each production business write.

Do not introduce dual writes unless explicitly designed and approved.

Before implementing a write path, identify:

```text
current authoritative writer
target writer
cutover point
reconciliation strategy
rollback impact
```

---

## 19. New Database Writes

New authoritative SaaS writes should target the new SQL Server database once that capability owns the write.

Do not make direct writes to legacy tables merely because current experimental code already does so.

Existing direct legacy mappings in the migration repo require review before extension.

---

## 20. Redis

Redis is part of the target platform architecture.

Use it only for an explicit requirement.

Do not automatically add caching to every service.

For tenant-sensitive cache data:

```text
tenant scope must be part of the cache key/ownership
```

Define invalidation behavior before adding caches for authoritative business reads.

---

## 21. Search

Search is part of the target architecture.

Do not introduce a search provider/index solely because Search is mentioned in the architecture.

Implement search infrastructure when the approved slice requires it.

Search must remain derived data and tenant-aware.

---

## 22. Event-Driven Architecture

Codex may identify opportunities for events/background processing.

### Existing required async behavior

If the migrated legacy capability already has required asynchronous/event-like behavior, preserve it appropriately.

### New event-driven recommendation

If introducing events would be a new architectural improvement, Codex must:

1. describe the current synchronous behavior;
2. explain the proposed event/background flow;
3. explain why it is beneficial;
4. explain idempotency/retry/consistency implications;
5. request approval before introducing new broker/worker infrastructure.

Do not add RabbitMQ, Kafka, MassTransit, Hangfire, Quartz, Azure Service Bus, or similar infrastructure without explicit approval.

---

## 23. Integrations

Do not add a new external integration without approval.

Provider-specific code should be behind an infrastructure/integration abstraction where appropriate.

Do not introduce:

```text
Tally
generic ERP connectors
new payment providers
new logistics providers
new messaging providers
```

unless the task explicitly requires them.

---

## 24. Data Ingestion

Current ingestion parity is limited to verified legacy capabilities:

```text
manual entry
Excel/XLS/XLSX
existing JSON/API writes
relevant existing mobile/application flows
```

Do not add unsupported ingestion types without approval.

When implementing ingestion, keep it tenant-aware and route writes through the relevant business/application services.

---

## 25. Dependency / Package Rule

Avoid adding new NuGet or npm dependencies unless:

- the existing stack cannot reasonably solve the problem;
- the package has a clear purpose;
- the maintenance/security cost is justified.

Before adding a significant package, explain why it is required.

Prefer platform/framework capabilities already available in ASP.NET Core, .NET, EF Core, and Next.js where reasonable.

---

## 26. Avoid Premature Abstractions

Do not create:

```text
IService
Service
Manager
Provider
Factory
Strategy
Repository
Adapter
Handler
Coordinator
```

for every class by default.

Introduce abstractions when they provide a real boundary, such as:

- infrastructure dependency inversion;
- multiple implementations;
- testability of important side effects;
- provider isolation;
- migration compatibility.

Avoid architecture-by-naming.

---

## 27. Error Handling

Use the repository's established Problem Details / exception-handling boundary where appropriate.

Do not:

- expose stack traces in production responses;
- swallow exceptions silently;
- convert every exception to HTTP 500 without context;
- return successful responses for failed writes.

Preserve correlation IDs/logging for diagnosability.

---

## 28. Logging

Log enough information to diagnose a migration slice while avoiding sensitive data.

Useful context may include:

```text
correlation id
capability
operation
tenant identifier where appropriate
result
failure category
integration outcome
```

Do not log:

- passwords;
- JWT tokens;
- API keys;
- secrets;
- sensitive customer/business payloads unnecessarily.

---

## 29. Testing Rule

For migrated slices, add local tests covering the important behavior where practical.

Relevant test categories include:

- core business behavior;
- API behavior;
- tenant isolation/security;
- important legacy-parity/characterization cases;
- persistence behavior;
- failure behavior.

Tenant-owned features should specifically test cross-tenant denial.

---

## 30. Test Repository Policy

Current project preference:

> Tests created for migration/development verification are local development artifacts and are not intended to be pushed to GitHub unless this policy is explicitly changed.

Therefore Codex may create/use local test projects or test files when required for implementation confidence.

Do not assume they should be added to Git or included in the committed solution.

Do not modify `.gitignore` to start tracking local tests unless explicitly requested.

Tests must still be run locally before treating a slice as validated.

---

## 31. Build and Verification

Before declaring implementation complete, run the relevant available checks.

For backend work, typically:

```text
dotnet build
relevant local tests
```

For frontend work, as applicable:

```text
lint
TypeScript checks
build
```

If an environment prevents a check, state that explicitly rather than claiming success.

---

## 32. Do Not Implement from Documentation Alone

Documentation provides context and decisions.

When migrating a concrete legacy capability, inspect the actual relevant source/evidence.

Do not implement business rules solely because a markdown summary says they exist.

Use:

```text
documentation for orientation
+
legacy source/database evidence for behavior
+
accepted architecture for target implementation
```

---

## 33. Superseded Experimental Code

The pre-correction repository contained experimental/old-assumption work including:

- per-tenant database routing;
- incomplete shared-schema tenancy;
- AI-first schema changes;
- direct Product writes into legacy tables;
- legacy connection aliases.

The foundation correction removes or replaces these paths. Do not reintroduce them merely because they existed historically; any proposed compatibility reuse must be reviewed against the current architecture.

---

## 34. AI Scope

Do not add AI agents, orchestration, LLM provider integration, embeddings, autonomous writes, or new AI governance infrastructure unless explicitly requested.

The removed AI-related schema/code remains historical only and must not be restored or expanded without explicit approval.

AI is not the current priority for core migration.

---

## 35. Scope Escalation Rule

Stop and ask for approval before implementing a change that requires any of the following:

```text
changing accepted architecture
changing tenancy model
new deployment/service
new external integration
new ingestion mechanism
new event-bus/worker infrastructure
Dapper introduction
material permission/authorization behavior change
material legacy business-semantics change
microservice extraction
database-engine change
```

Explain:

```text
what is needed
why it is needed
alternatives
impact
```

Do not silently expand scope.

---

## 36. No Unrelated Refactoring

When working on a migration slice:

- avoid unrelated cleanup;
- avoid renaming broad areas unnecessarily;
- avoid formatting/restructuring unrelated files;
- avoid rewriting working code solely for preference.

Make the smallest coherent change required by the task.

If unrelated technical debt blocks the task, explain it first.

---

## 37. Security Defaults

Prefer fail-closed behavior.

Examples:

```text
missing tenant → deny
invalid membership → deny
missing required permission → deny
unknown tenant → deny
invalid authentication → deny
```

Do not silently fall back to:

```text
default tenant
first tenant
platform owner
legacy unrestricted access
```

---

## 38. Migration Compatibility

When legacy compatibility is required:

- mark compatibility code clearly;
- keep it isolated where practical;
- document the reason;
- define when it can be removed.

Do not allow temporary compatibility logic to silently become permanent domain architecture.

---

## 39. Codex Implementation Checklist

Before writing code, confirm:

```text
1. What capability/task is being implemented?
2. Is it in current scope?
3. What legacy behavior must be preserved?
4. Has the relevant legacy /api path been inspected?
5. What additional legacy dependencies are relevant?
6. Is the data Platform-owned, Tenant-owned, or Global?
7. How is tenant isolation enforced?
8. Which system currently owns writes?
9. Is EF Core sufficient?
10. Would Dapper be needed? If yes, stop for approval.
11. Are stored procedures involved?
12. Does the change introduce an event/integration/package?
13. Does that require approval?
14. What tests should be run locally?
15. What build/verification commands will prove the change?
```

If a critical answer is unknown, surface the gap instead of guessing.

---

## 40. Canonical Implementation Flow

```text
Current task
    ↓
Read relevant agent docs
    ↓
Inspect current repo state
    ↓
Inspect targeted legacy /api behavior if migrating a capability
    ↓
Follow only relevant dependencies
    ↓
Confirm tenant/data ownership
    ↓
Design smallest target slice
    ↓
Raise any architecture/scope exception
    ↓
Implement
    ↓
Run local verification/tests
    ↓
Report exactly what changed and what remains unresolved
```

The implementation invariant is:

> **Preserve verified business behavior, enforce tenant safety, keep the design simple, and never introduce significant architecture or scope changes silently.**
