# Engineering Rules Reference

Use this reference as the compact checklist for Rail-Factory Fork implementation.

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
