---
name: rail-factory-engineering
description: Engineering rules for AI agents working on Rail-Factory Fork. Use when implementing, refactoring, reviewing, planning, or documenting this project, especially for enforcing strict SOLID, Hexagonal Architecture, bounded contexts, tenant handling, Aspire/.NET services, Gateway/BFF/UI boundaries, task checklist updates, and context synchronization.
---

# Rail Factory Engineering

Use this skill before changing code or documentation in Rail-Factory Fork.

## Required First Reads

Read these files in order:

1. `docs/CONTEXTO_ATUAL.md`
2. `docs/PLANO_DE_TASKS.md`
3. `docs/REGRAS_PARA_IAS.md`

Read additional docs only when needed:

- architecture decisions: `docs/ARQUITETURA_GERAL.md`
- requirements: `docs/REQUISITOS.md`
- build order: `docs/ANALISE_REQUISITOS_E_PASSADAS.md`
- domain responsibilities: `docs/FUNCIONALIDADES.md`
- visual dependencies: `docs/GRAFOS_DO_PROJETO.md`

For a compact rule reference, read `references/engineering-rules.md`.

## Work Protocol

1. Identify the current pass and task.
2. Identify the bounded context affected.
3. Keep the change small and verifiable.
4. Apply strict SOLID and Hexagonal Architecture.
5. Validate with build/test or explain why validation was not possible.
6. Update `docs/CONTEXTO_ATUAL.md` when the real state changes.
7. Update `docs/PLANO_DE_TASKS.md` when task state changes.

Do not skip passes or implement future functionality without a dependency from the current flow.

## Architecture Rules

- Domain contains business rules only.
- Application orchestrates use cases and owns ports.
- Infrastructure implements ports and external details.
- Api adapts HTTP to Application.
- Gateway routes and applies edge policy; it does not own business rules.
- BFF owns browser session, cookie, CSRF, and frontend facade; it does not own domain rules.
- UI calls BFF only.

Forbidden dependencies:

- Domain -> Infrastructure, Api, EF Core, ASP.NET, Redis, RabbitMQ, Aspire, YARP.
- Application -> Api or concrete infrastructure.
- UI -> Gateway or internal services.

## Established Standards

- Code identifiers, comments, API contracts, and engineering documentation are written in English.
- Services use `Api`, `Application`, `Domain`, and `Infrastructure` as default boundaries.
- Request DTOs live in `Api/Requests`; response DTOs live in `Api/Responses`; DTO validators live in `Api/Validation`.
- Relational persistence uses EF Core and formal migrations by default.
- Raw SQL is allowed only with documented technical justification.
- Runtime in-memory persistence fallback is not allowed for production services.
- HTTP DTO validation and Application/Domain business validation are both required when applicable.
- `Program.cs` stays small and limited to composition, middleware, and endpoint/module mapping.
- Do not add `tenant_code` columns when data is isolated by separate PostgreSQL instances or databases.

## SOLID Enforcement

- Single Responsibility: one clear reason to change.
- Open/Closed: extend at real variation points, not with speculative abstractions.
- Liskov: every adapter honors the same port contract.
- Interface Segregation: ports are small and use-case oriented.
- Dependency Inversion: core code depends on abstractions, not details.

Reject generic `Manager`, `Helper`, `Processor`, or broad `Service` classes unless their responsibility is precise and justified.

## Tenant And Context

Tenant-aware operations must carry tenant explicitly:

- HTTP: `X-Tenant-Code`
- events/jobs: `tenantCode`
- logs/traces: tenant when available

Initial tenant is `dev`, but permanent domain rules must not hardcode `dev`.

## Completion Gate

Before closing a task, confirm:

- the task belongs to the current pass;
- the bounded context owns the responsibility;
- SOLID and Hexagonal boundaries are respected;
- validation ran or the reason is documented;
- context and task docs are synchronized.
