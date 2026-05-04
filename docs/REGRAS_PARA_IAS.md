# AI Agent Engineering Rules

These rules are mandatory for any AI agent working in this repository. They exist to prevent repeated refactors caused by simple architectural mistakes.

## Source Priority

1. `docs/CONTEXTO_ATUAL.md`: real project state and next action.
2. `docs/PLANO_DE_TASKS.md`: executable checklist.
3. `docs/REGRAS_PARA_IAS.md`: engineering and documentation rules.
4. `docs/ARQUITETURA_GERAL.md`: target architecture.
5. `docs/REQUISITOS.md`: canonical requirements.

If documents conflict, do not invent behavior. Record the divergence and fix the most specific document.

## Main Rule

Implement production-quality code the first time. Do not add temporary shortcuts when an established project standard already exists.

Before coding:

- Read `docs/CONTEXTO_ATUAL.md`.
- Confirm the active task in `docs/PLANO_DE_TASKS.md`.
- Identify the correct boundary: `Domain`, `Application`, `Infrastructure`, `Api`, `Gateway`, `BFF`, or `UI`.
- Reuse existing project standards before creating new patterns.
- Do not implement future functionality unless it is required by the current flow.

## Established Standards

These standards are already decided for the project.

- Language: code identifiers, API contracts, folders, comments, and engineering documentation must be written in English.
- Architecture: microservices use Hexagonal Architecture.
- Service layout: use `Api`, `Application`, `Domain`, and `Infrastructure` as the default top-level boundaries.
- HTTP DTOs: request DTOs live in `Api/Requests`; response DTOs live in `Api/Responses`.
- HTTP validation: DTO validators live in `Api/Validation`.
- Use cases: application orchestration lives in `Application`.
- Ports: application-owned interfaces live in `Application` or `Domain`, depending on who owns the abstraction.
- Persistence adapters: EF Core implementations live in `Infrastructure/Persistence`.
- Migrations: database schema changes are represented by formal EF Core migrations near the owning `DbContext`.
- Composition root: `Program.cs` must stay small and only handle dependency registration, middleware, and endpoint/module mapping.
- Persistence default: use ORM, specifically EF Core, for normal relational persistence.
- Raw SQL: allowed only for clearly justified cases such as reporting, vendor-specific locking, bulk operations, or proven performance bottlenecks.
- In-memory persistence: not allowed as runtime fallback for production services.
- Configuration: critical configuration must fail explicitly during startup or use-case execution; do not silently degrade.
- UI boundary: UI calls the BFF only.
- BFF boundary: BFF owns session, cookies, CSRF, and UI-facing facade concerns; it does not implement domain rules.
- Gateway boundary: Gateway routes and applies cross-cutting edge policies; it does not implement business rules.
- Service boundary: internal services own business capabilities and persistence.

## First-Time Quality Checklist

Before writing or changing code, verify:

- Is this the smallest correct design that satisfies the current task?
- Does each class have one reason to change?
- Is business logic outside `Program.cs`, controllers, endpoint lambdas, EF configurations, and DTOs?
- Is persistence implemented through the established ORM/migration pattern?
- Are validations split between HTTP shape validation and business invariants?
- Are errors explicit, stable, and observable?
- Are tests added or updated at the correct level?
- Are task and context documents updated when state changes?

## Hexagonal Architecture

- `Domain` contains business concepts, invariants, value objects, aggregates, and domain services.
- `Domain` must not reference EF Core, ASP.NET, HTTP, queues, Aspire, YARP, UI, or infrastructure packages.
- `Application` orchestrates use cases and depends on ports.
- `Application` must not depend on concrete database, HTTP client, message broker, or framework implementations.
- `Infrastructure` implements ports for persistence, external providers, queues, files, email, and other integrations.
- `Api` adapts HTTP requests to application use cases and application responses to HTTP responses.
- Controllers/endpoints must remain thin.
- Framework attributes are allowed at the API boundary, not inside domain models.

## SOLID Without Ceremony

- SRP: split files and classes by reason to change, not by personal preference.
- OCP: extend via focused abstractions or handlers when variation is real.
- LSP: derived implementations must preserve the contract of the abstraction.
- ISP: avoid broad interfaces that force unused methods.
- DIP: business flow depends on ports, not concrete infrastructure.
- Avoid decorative abstraction. Do not create interfaces, factories, strategies, or layers without a concrete variation point or testability need.

## Code Organization

Use established folder conventions before inventing new ones.

- Endpoint mapping belongs in dedicated endpoint/module files when it grows beyond trivial registration.
- Request DTOs belong in `Api/Requests`.
- Response DTOs belong in `Api/Responses`.
- Request validators belong in `Api/Validation`.
- Application commands, queries, handlers, and use cases belong in `Application`.
- Domain models and invariants belong in `Domain`.
- EF Core `DbContext`, entity mappings, repositories, and migrations belong in `Infrastructure/Persistence`.
- Provider-specific clients and adapters belong in `Infrastructure`.
- Tests must mirror the boundary they validate.

If a file changes for unrelated reasons, split it.

## Persistence

- Use EF Core for relational persistence by default.
- Use migrations for schema evolution.
- Keep persistence concerns out of `Domain` and `Application`.
- Do not hand-write CRUD SQL when EF Core can express the operation clearly.
- Do not add Npgsql/Dapper/manual ADO.NET persistence unless the reason is documented in code and project docs.
- Do not add runtime in-memory stores as fallback for missing infrastructure.
- Local development may use local databases or containers, but the production code path must stay real.

## Validation

Use two validation layers, because they solve different problems.

- DTO validation protects the HTTP boundary: required fields, length, format, enum values, URL shape, and basic syntactic rules.
- Application/domain validation protects business correctness: invariants, authorization decisions, state transitions, uniqueness, tenant ownership, and cross-aggregate rules.
- Do not rely only on DTO validation for business rules.
- Do not push HTTP-specific validation into domain models.
- Validation failures must return stable error codes when the API already has an error contract.

## Tenant And Data Isolation

- Do not add `tenant_code` to tables automatically.
- If data is isolated by separate PostgreSQL instances/databases, do not duplicate tenant identifiers in every table.
- Tenant context belongs at the routing, connection selection, configuration, or request boundary.
- Add tenant columns only when the same physical data store intentionally contains multiple tenants.
- Document the isolation model before changing schema.

## Events And Idempotency

When introducing events or integration messages:

- Define an explicit event contract.
- Include stable identity, timestamps, correlation identifiers, and schema version.
- Define idempotency behavior for consumers.
- Keep event mapping outside domain entities unless the domain event itself is part of the domain model.
- Do not introduce messaging infrastructure before a current use case needs it.

## Errors And Observability

- Use stable error codes for API errors.
- Prefer `ProblemDetails` or the existing project error shape.
- Do not expose secrets, provider tokens, connection strings, or internal stack traces to clients.
- Log operational failures with enough context to diagnose them.
- Include correlation/request identifiers where the project already supports them.
- Expected business failures must not be logged as unhandled exceptions.

## Frontend, BFF, And Gateway

- UI calls BFF, never internal services directly.
- BFF owns session, cookies, CSRF, UI-specific aggregation, and browser-facing auth flow.
- BFF must not own domain rules.
- Gateway owns routing and edge policies.
- Gateway must not own business workflows.
- Internal services expose capability APIs and own their persistence.

## Testing And Validation

Run the smallest meaningful validation set for the changed boundary.

- Domain change: domain/unit tests.
- Application use case change: application tests with mocked ports.
- Infrastructure persistence change: integration-style tests or build plus migration verification.
- API contract change: API tests.
- BFF/auth/session change: BFF tests and authenticated smoke when applicable.
- UI change: frontend tests/build.

Do not mark a task complete without build/test evidence or an explicit documented blocker.

## Documentation

Update documentation only when project state, rules, requirements, or task status changes.

- Update `docs/CONTEXTO_ATUAL.md` when the real project state changes.
- Update `docs/PLANO_DE_TASKS.md` when opening, closing, or reclassifying tasks.
- Update architecture or requirements docs only for real decisions or direction changes.
- Record validation commands and outcomes for completed tasks.
- Do not mark a task as done without evidence.

## Criticality Scale

Use this scale when classifying engineering problems:

- `0`: cosmetic only.
- `1-2`: minor maintainability issue.
- `3-4`: localized design or test gap.
- `5-6`: recurring maintainability risk or localized architecture violation.
- `7`: critical issue when the pattern spreads across services or forces repeated refactors.
- `8`: critical issue already affecting multiple services or blocking reliable delivery.
- `9`: severe systemic issue requiring an ADR, architectural enforcement, migration plan, and automated checks.
- `10`: production incident, data loss risk, security breach, or system-wide outage.

Do not inflate severity without evidence. Raise from `7` to `9` only when the issue has systemic scope and an enforcement plan is required.

## Final Gate

Before finishing a task, confirm:

- The implementation follows established project standards.
- The correct boundary owns the code.
- DTO validation and business validation are both handled where applicable.
- Persistence uses EF Core and migrations when relational data is involved.
- No tenant column was added without proving shared physical storage.
- No business rule was placed in UI, BFF, Gateway, endpoint registration, or EF mapping.
- Tests/builds were run or the blocker was documented.
- `docs/CONTEXTO_ATUAL.md` and `docs/PLANO_DE_TASKS.md` are synchronized when needed.
