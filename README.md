# LCA AI Commerce Platform

## Project status

LCA is a **migration and modernization project** for an existing commerce system. It is not a greenfield replacement and must not be implemented as a big-bang rewrite.

The target is a **multi-tenant AI commerce platform** operated by a central platform owner and used by multiple businesses/tenants, while customers interact through shared web/mobile commerce channels.

The complete-project PRD includes AI and Android work. The **current repository implementation focus is Sprint 1 web frontend, ASP.NET Core backend, shared API, and database migration groundwork only**. AI-agent and Android/Flutter implementation is out of scope unless explicitly reopened.

## Core actors

- **C — Platform Owner:** operates the platform, tenants, permissions, integrations, AI controls, audit and platform-level reporting.
- **B1, B2, B3... — Businesses / Tenants:** manage their own products, inventory, pricing, customers, orders, RFQs, CRM and business operations.
- **A — Customer / Consumer:** searches products, obtains recommendations, places orders/RFQs, pays, tracks deliveries and uses customer-facing assistance.

## Target application surfaces

- Customer Web / Mobile
- Business Admin / CRM
- Platform Admin

All channels use shared secured APIs and authoritative business data.

## Target backend direction

The current target backend direction is **ASP.NET Core on .NET**, organized first as a **modular platform / modular monolith**. Modules may be extracted later only when scale, operational isolation, ownership or regulatory requirements justify it.

The baseline request path is:

```text
User / Application
    -> API Gateway / API boundary
    -> Authentication
    -> Authorization + Tenant Context
    -> Application / Business Layer
    -> Authoritative Data / Approved External Services
```

## Core modules

- Identity & Access
- Customer
- Product / Catalogue
- Product Intelligence
- Pricing
- Inventory
- Commerce / Orders
- RFQ / Quote
- CRM
- AI Gateway / Orchestrator
- Search
- Marketing
- Payment
- Logistics
- Notifications
- Reporting / Analytics
- Audit
- Integration Hub

## Non-negotiable architecture rules

1. **Migration first:** preserve the legacy system while ownership moves incrementally.
2. **No schema guessing:** the conceptual ERD is not the legacy database schema.
3. **Server-side tenant isolation:** tenant security must be enforced in backend/data access and search.
4. **Authoritative business data:** AI is never the source of truth for price, stock, order, payment or shipment state.
5. **Provider-neutral integrations:** payment/logistics/ERP/etc. should sit behind adapters.
6. **Idempotent asynchronous processing:** event consumers and webhooks must tolerate retries.
7. **Modular-first architecture:** avoid premature microservices.

## Documentation map

Start with [docs/README.md](docs/README.md).

Database-specific migration documents and their checked-in structural exports are available under [docs/database](docs/database/README.md). They are derived from database evidence and must not be replaced by assumptions from the conceptual ER diagram.
