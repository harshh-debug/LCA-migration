# Target Architecture

> **Scope note:** This document describes complete-project target direction. Current implementation is limited to the Sprint 1 web frontend, ASP.NET Core backend, shared API, and database groundwork defined in [the documentation guide](buffer/README.md). AI and Android elements shown in the conceptual visuals are not current implementation scope.

## Architectural style

The recommended initial style is a **modular platform / modular monolith**, not a fleet of microservices. The current backend is one ASP.NET Core server with three project boundaries: `Lca.Api` for HTTP/hosting composition, `Lca.Core` for the implemented LCA application and business behavior, and `Lca.Infrastructure` for technical implementations. Sprint 1 currently has only security and tenancy code in Core; business modules are added when their legacy behavior is migrated.

Logical module boundaries should be explicit from the start so that selected modules can later be extracted when justified.

## High-level topology

```text
                    Platform Owner C
                          |
                 Multi-Tenant Platform
                          |
          +---------------+---------------+
          |               |               |
       Tenant B1       Tenant B2       Tenant B3
          |               |               |
          +---------------+---------------+
                          |
                 Shared Secured APIs
                          |
       +------------------+------------------+
       |                  |                  |
 Customer Web/Mobile  Business Admin/CRM  Platform Admin
       |                  |                  |
       +------------------+------------------+
                          |
                  Business Platform
                          |
    Product | Pricing | Inventory | Commerce | CRM | RFQ
                          |
          AI | Search | Integrations | Reporting
```

## Backend request flow

```text
User
 -> Application
 -> API Gateway / API boundary
 -> Authentication
 -> Authorization + Tenant Context
 -> Application / Business Layer
 -> Domain capability
 -> Database / Cache / Search / Integration as appropriate
```

The term `API Gateway` in the diagrams describes the common secured entry boundary. The deployment may initially implement this inside the modular application rather than requiring a separate gateway product.

## Core architecture layers

### Interface/API boundary

Accepts customer, tenant-admin and platform-admin requests and exposes stable contracts.

### Authentication and authorization

Establishes identity, roles/policies and tenant context.

### Application/business layer

Coordinates use cases and calls domain capabilities. It must not bypass authorization or authoritative data ownership.

### Domain modules

Own business rules for Product, Pricing, Inventory, Commerce, CRM, RFQ, Payment, Logistics and other capabilities.

### Infrastructure/integration layer

Provides persistence, caches, search, event transport and provider adapters without leaking provider-specific concerns into domain logic.

## Possible scale evolution

These are future target proposals rather than PRD Sprint numbers. Only components required by an approved migration slice should be introduced.

### Initial modular platform

- modular application
- shared database
- Redis
- search

### Phase 2

- background workers
- event bus
- independent Search/AI workers where useful

### Phase 3

Potentially extract high-load or independently operated modules such as:

- Search
- AI
- Notifications
- Marketing

### Phase 4

Potentially extract additional modules such as:

- Payment
- Logistics
- CRM

Extraction is conditional, not mandatory.

## Approved delivery dependency order

The modular boundaries are introduced incrementally in this order:

1. database access layer;
2. configuration foundation;
3. authentication, authorization, and trusted tenant context;
4. shared utilities required by an approved slice;
5. Product API;
6. Category API, with Product and Category due by the end of Sprint 2;
7. Customer API;
8. Sales / Order APIs;
9. Quotation API;
10. Logistics API;
11. required Sales/report and CRM read endpoints.

This is a delivery priority, not permission to skip legacy characterization or cutover gates. Product, price, stock, Customer, and Order facts remain owned by authoritative backend data. External AI systems may consume these APIs, but AI agents, orchestration, and AI-specific migration modules remain outside this repository's current implementation scope.

## Architecture diagrams

The following images describe the complete-project target concept. They do not establish the legacy schema, current deployment topology, or Sprint 1 implementation scope.

### Architecture principle

![Complete-project architecture principle](Architecture-principle.jpg)

### Main business flow

![Complete-project main business flow](main-business-flow.jpg)

### Conceptual target ER diagram

![Conceptual target ER diagram—not the legacy database schema](ER-diagram.jpg)

Legacy physical database facts must come from the [database documentation](database/README.md) and restored-database evidence.

## Related documents

- [Documentation guide and current Sprint 1 scope](buffer/README.md)
- [Migration strategy](buffer/MIGRATION_STRATEGY.md)
- [Complete-project PRD](LCA_2.5_Month_PRD.md)
