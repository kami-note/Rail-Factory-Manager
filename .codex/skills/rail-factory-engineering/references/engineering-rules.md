# Engineering Rules Reference

Use this reference as the compact checklist for Rail-Factory Fork implementation.

## Established Standards

- Write code identifiers, comments, API contracts, and engineering documentation in English.
- Use Hexagonal Architecture with `Api`, `Application`, `Domain`, and `Infrastructure` boundaries.
- Use EF Core plus formal migrations for relational persistence.
- Do not use raw SQL for common CRUD unless the reason is documented.
- Do not add runtime in-memory persistence fallback for production services.
- Validate HTTP DTOs at the API boundary and business invariants in `Application` or `Domain`.
- Keep `Program.cs` small: composition, middleware, and endpoint/module mapping only.
- UI calls BFF only; BFF owns session/cookies/CSRF; Gateway routes only.
- Do not add `tenant_code` columns when data is already isolated by separate PostgreSQL instances or databases.
- Fail explicitly for missing critical configuration.

## Source Order

1. `docs/CONTEXTO_ATUAL.md`
2. `docs/PLANO_DE_TASKS.md`
3. `docs/REGRAS_PARA_IAS.md`
4. `docs/ARQUITETURA_GERAL.md`
5. `docs/REQUISITOS.md`

## Layering

```text
Api -> Application -> Domain
Infrastructure -> Application ports
UI -> BFF -> Gateway -> Services
```

Domain never references infrastructure, web, database, queue, cache, Aspire, YARP, or UI.

## SOLID Rules

- SRP: one reason to change.
- OCP: extend only at real variation points.
- LSP: adapters must honor the same port behavior.
- ISP: ports stay small and use-case oriented.
- DIP: core depends on ports, infrastructure implements ports.

## Bounded Context Ownership

| Context | Owns |
|---|---|
| Tenancy | tenant catalog and resolution |
| IAM | identity, login, session, permission |
| Supply Chain | receiving, XML/NF-e, blind count, divergence |
| Inventory | balance, reservation, blocking, consumption, ledger |
| Production | BOM, production order, execution, scrap, quality, lot |
| Logistics | picking, packing, shipment, freight, tracking |
| HR | people, hours, skills, shifts |
| Fleet | vehicles, capacity, maintenance, fueling |
| Dashboard | read models, indicators, reports |

Inventory is the only source of stock balance.

## Context Sync

Update `docs/CONTEXTO_ATUAL.md` when real project state changes.

Update `docs/PLANO_DE_TASKS.md` when task state changes.

Do not update architecture/requisitos for implementation details unless a decision changed.

## Done Means

- Build/test relevant path.
- No boundary violation.
- Tenant explicit when applicable.
- Docs synchronized.
- Remaining risks stated.

## First-Time Quality Defaults

- Prefer existing repo patterns over inventing a new one.
- Keep DTOs, validation, endpoints, use cases, persistence, and external adapters in separate files/classes when they have different reasons to change.
- HTTP inputs use DTO/border validation plus Application/Domain business validation.
- Application persistence uses EF Core and formal migrations by default.
- Raw SQL is allowed only with documented technical justification.
- Critical configuration must fail explicitly; do not hide it behind in-memory fallback.
- Do not mark a task complete without verifiable acceptance evidence.
