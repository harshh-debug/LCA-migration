# AGENTS.md

## LCA Codex Working Rules

This repository is the **new LCA multi-tenant SaaS migration platform**.

Do not treat it as a simple ASP.NET Web Forms → ASP.NET Core code conversion.

The goal is to migrate verified LCA business capabilities into a tenant-aware ASP.NET Core / .NET 10 + Next.js platform using a new SQL Server database.

---

## 1. Legacy Repository Location

Set the legacy repository root before migration work:

```text
LEGACY_REPO_ROOT = /media/harshcode/New Volume/Development/LCA/httpdocs-20260824T054956Z-1-001/httpdocs
```

When this placeholder has not been replaced and a task requires legacy inspection, ask for the legacy repository path before proceeding.

Do not guess the legacy repo location.

---

## 2. Mandatory Context Before Every Task

Always read:

```text
.agents/codex/00-PROJECT-CONTEXT.md
.agents/codex/06-CURRENT-SCOPE.md
.agents/codex/07-DECISIONS-AND-OPEN-QUESTIONS.md
```

Then read only the task-specific files needed.

### Task-specific reading

```text
Architecture/design
→ .agents/codex/01-TARGET-ARCHITECTURE.md

Understanding what currently exists
→ .agents/codex/02-CURRENT-STATE.md

Database / tenancy / TenantId / membership
→ .agents/codex/03-DATA-AND-TENANCY.md

Legacy migration / parity / cutover / reconciliation
→ .agents/codex/04-LEGACY-MIGRATION.md

Coding / implementation / testing
→ .agents/codex/05-IMPLEMENTATION-RULES.md
```

Do not load every historical document for every task.

---

## 3. Source-of-Truth Order

When information conflicts, use this order:

```text
1. Current explicit user instruction
2. .agents/codex/07-DECISIONS-AND-OPEN-QUESTIONS.md
3. .agents/codex/06-CURRENT-SCOPE.md
4. Accepted target/data/migration/implementation rules
5. Actual current repository implementation
6. Historical/reference documentation
7. Legacy implementation details
```

Existing code does not override a newer accepted architecture decision merely because it already exists.

If current code conflicts with an accepted decision, flag the conflict and implement against the accepted direction after plan approval.

---

## 4. Current Architectural Baseline

Treat these as current accepted direction:

```text
ASP.NET Core / .NET 10
Next.js
SQL Server
modular monolith initially
Lca.Api + Lca.Core + Lca.Infrastructure
one new SQL Server database
shared schema
TenantId-based tenant ownership
global User identity
TenantMembership for tenant association
ASP.NET Core Identity
backend-issued JWT
active tenant resolved from valid membership during login
tenant_id carried in JWT
platform and tenant authorization scopes kept separate
```

The existing per-tenant database-routing implementation is superseded and must not be extended.

---

## 5. Current Product Focus

The current priority is an **internal administration / order-management system**.

Current work includes:

```text
multi-tenant foundation
Identity & Access
Product
Category
Pricing
Inventory
Customer
Orders
minimal internal Next.js administration UI
```

Do not expand into:

```text
full consumer ecommerce storefront
AI implementation
tenant self-service signup
subscription billing
plan management
platform-owner dashboard
microservice extraction
```

unless scope is explicitly changed.

---

## 6. Before Coding

For a coding task:

1. inspect the relevant current implementation;
2. read the required agent docs;
3. if it is a migration slice, inspect the relevant legacy behavior;
4. produce a concise implementation plan;
5. identify genuine decision points or blockers;
6. wait for plan confirmation when the user requested/requires review;
7. after confirmation, implement the agreed plan without repeatedly asking permission for steps already covered by that approval.

Do not stop repeatedly for routine implementation choices already implied by the approved plan.

If a **new approval-gated decision** appears during implementation, stop and raise only that new decision.

---

## 7. Legacy Inspection Rule

For legacy behavior, start with:

```text
<LEGACY_REPO_ROOT>/api/
```

Inspect the smallest relevant endpoint set first.

Follow dependencies outside `/api/` only when required, including:

```text
App_Code/
root ASPX pages
stored procedures
triggers
mobile/
test-api/
deepak/
ASMX / WebMethods
files
external integrations
```

Do not scan the entire legacy repository for every task.

`/api/` is the primary inspection surface, not proof that all behavior lives there.

---

## 8. Migration Principle

Migrate **business capability**, not legacy file structure.

Do not create one modern endpoint/service for every ASPX page.

For each migrated capability:

```text
understand legacy behavior
→ identify data / permissions / integrations / side effects
→ define tenant-aware target behavior
→ implement smallest safe slice
→ migrate/reconcile legacy data into the new SQL Server
→ verify behavior and tenant isolation
→ move ownership deliberately
```

The legacy application keeps its own database during coexistence.

The new platform uses its own SQL Server database.

Existing LCA business data is migrated/reconciled as the initial tenant.

---

## 9. Write Ownership

Each production business action/aggregate should have one authoritative writer at a time.

Do not introduce automatic dual writes.

Temporary legacy reads are allowed for an approved migration slice, but legacy access must be treated as compatibility/coexistence logic rather than final SaaS persistence.

---

## 10. Implementation Defaults

Use:

```text
ASP.NET Core Controllers as the primary API style
EF Core as the default persistence technology
EF Core migrations for new target-schema changes
explicit request/business validation
server-side authorization
tenant-scoped reads and writes
local verification/tests
```

Preserve relevant legacy stored-procedure behavior unless there is a justified exception.

Do not reproduce unsafe legacy implementation patterns such as direct page SQL, weak authorization, or implicit single-business assumptions.

---

## 11. Dapper Rule

Do not introduce Dapper automatically.

If Dapper appears useful:

1. explain the specific query/use case;
2. explain why EF Core is insufficient or materially worse;
3. explain whether the use is temporary compatibility or target architecture;
4. explain tenant-isolation implications;
5. ask for approval.

Implement Dapper only after approval.

---

## 12. Tenant Safety

For tenant-owned operations, enforce:

```text
authenticated user
→ valid TenantMembership
→ trusted tenant context
→ authorization
→ TenantId-scoped persistence
```

Do not trust arbitrary `TenantId` values supplied by the client for normal tenant operations.

Do not silently fall back to a default tenant.

Prefer fail-closed behavior.

---

## 13. Roles and Permissions

Implement only the roles/permissions required by approved slices.

Do not invent the entire future RBAC model.

Legacy permission behavior must be inspected where relevant.

If changing an ambiguous permission could change legitimate business behavior, ask before changing its semantics.

---

## 14. Approval Gates

Stop and ask before implementing any new requirement involving:

```text
Dapper
significant new NuGet/npm dependency
event broker / general worker infrastructure
new external integration
tenancy-model change
database-engine change
material permission/authorization behavior change
material legacy business-rule change
microservice extraction
AI scope expansion
new unsupported ingestion mechanism
new deployment/service architecture
```

Explain:

```text
what is needed
why
available options
recommended option
impact
```

Once approved, do not repeatedly ask permission for implementation steps already covered by that decision.

---

## 15. Redis, Search, and Events

Redis and dedicated Search are target capabilities, but implement them only when a current approved slice actually requires them.

The exact Search provider and Redis hosting are not finalized.

Do not introduce new event-bus/background-worker infrastructure merely because event-driven architecture may be useful.

Preserve required existing async behavior; propose new event-driven improvements for approval first.

---

## 16. AI

AI is outside the current core migration scope.

Existing AI-related code/schema is review/freeze-only.

Do not expand:

```text
AI agents
LLM providers
orchestration
embeddings
autonomous writes
AI governance infrastructure
```

unless AI scope is explicitly reopened.

---

## 17. Tests

Add and run local tests where useful for:

```text
business behavior
API behavior
tenant isolation
authorization
legacy parity
persistence behavior
important failures
```

Current project preference is that development tests remain local and are not pushed to GitHub unless explicitly requested.

Do not change `.gitignore` to start tracking local tests without approval.

---

## 18. Packages and Abstractions

Do not add packages, projects, interfaces, repositories, factories, managers, or architectural layers merely for convention.

Prefer the simplest design that preserves:

```text
clarity
tenant safety
testability
migration compatibility
maintainability
```

Add significant dependencies only when justified.

---

## 19. Documentation Maintenance

When implementation materially changes the current repository state, update:

```text
.agents/codex/02-CURRENT-STATE.md
```

so it continues to describe what is actually implemented.

Update it factually from code/runtime evidence.

Do not use `02-CURRENT-STATE.md` to redefine target architecture or scope.

Do **not** silently change accepted decisions in:

```text
03-DATA-AND-TENANCY.md
06-CURRENT-SCOPE.md
07-DECISIONS-AND-OPEN-QUESTIONS.md
```

If implementation requires changing those decisions, raise the decision first.

---

## 20. Current-State Documentation Rule

`02-CURRENT-STATE.md` describes reality, not intent.

When updating it:

- distinguish implemented vs partial vs scaffolded vs experimental;
- cite concrete repository evidence/file paths;
- do not mark something implemented just because a class/folder/package exists;
- record conflicts with target direction as review-required/experimental;
- never expose secrets.

---

## 21. Scope Discipline

Do not silently broaden a task.

Avoid unrelated refactoring.

If a migration task only needs Product behavior, do not redesign Customer, Orders, Search, AI, or infrastructure unless a real dependency requires it.

If an out-of-scope dependency blocks the requested work, explain the dependency and ask for the smallest decision needed.

---

## 22. Completion Rule

Do not claim completion merely because code compiles.

Run relevant checks such as:

```text
dotnet build
local tests
frontend lint
TypeScript checks
frontend build where environment allows
```

If a check could not be run, say so explicitly.

For migrated business functionality, also verify the relevant legacy parity, tenant isolation, and data reconciliation required by the slice.

---

## 23. Working Invariant

> Preserve verified LCA business behavior, redesign it for the accepted tenant-aware SaaS architecture, keep implementation focused, and never let old migration assumptions or existing experimental code silently override accepted decisions.
