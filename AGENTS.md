# AI Agent Rules

These rules apply to every AI agent working in this repository.

## Source Priority

1. `docs/CONTEXTO_ATUAL.md`: real code state and next action.
2. `docs/PLANO_DE_TASKS.md`: executable checklist.
3. `docs/REGRAS_PARA_IAS.md`: engineering and documentation rules.
4. `docs/ARQUITETURA_GERAL.md`: target architecture.
5. `docs/REQUISITOS.md`: canonical requirements.

If sources conflict, do not invent behavior. Record the divergence and fix the most specific document.

## Before Implementing

- Read `docs/CONTEXTO_ATUAL.md`.
- Confirm the active task in `docs/PLANO_DE_TASKS.md`.
- Do not skip passes.
- Do not implement future functionality without a real dependency from the current flow.
- Identify whether the change belongs to `Domain`, `Application`, `Infrastructure`, `Api`, `Gateway`, `BFF`, or `UI`.

## Established Standards

- Write code identifiers, comments, API contracts, and engineering documentation in English.
- Use SOLID rigorously, without decorative overengineering.
- Use Hexagonal Architecture.
- Use `Api`, `Application`, `Domain`, and `Infrastructure` as service boundaries.
- Keep `Domain` free of infrastructure, web frameworks, database, HTTP, queue, Aspire, Gateway, BFF, and UI concerns.
- Let `Application` orchestrate use cases and depend on ports.
- Let `Infrastructure` implement ports for persistence and providers.
- Let `Api` adapt HTTP to `Application`.
- Let `Gateway` route; it must not implement business rules.
- Let `BFF` handle session, cookies, CSRF, and UI facade concerns; it must not implement domain rules.
- Let `UI` call BFF only, never internal services directly.
- Use EF Core plus formal migrations for relational persistence.
- Do not use raw SQL for common CRUD unless the reason is documented.
- Do not add runtime in-memory persistence fallback for production services.
- Validate HTTP DTOs at the API boundary and business invariants in `Application` or `Domain`.
- Separate DTOs, validators, endpoints, use cases, domain models, and persistence adapters by reason to change.
- Keep `Program.cs` small and limited to composition, middleware, and mapping.
- Do not add `tenant_code` columns when data is already isolated by separate PostgreSQL instances or databases.
- Fail explicitly for missing critical configuration; do not silently degrade.

## Documentation

Every relevant change must update:

- `docs/CONTEXTO_ATUAL.md` when the real project state changes.
- `docs/PLANO_DE_TASKS.md` when a task is opened, completed, or reclassified.
- Architecture or requirements docs only when a real decision or direction changes.

Do not mark a task complete without build/test evidence or a documented blocker.

## Minimum Validation

- Run the applicable build/test set for the changed boundary.
- If validation cannot run, document the exact reason.
- Do not hide technical debt as completed work.

See `docs/REGRAS_PARA_IAS.md` for the full engineering standard.

## Figma Design System Rules (UI Consistency)

These rules are mandatory for Figma-driven UI changes in this repository.

### Component Organization

- IMPORTANT: Keep UI implementation under `src/RailFactory.Frontend/App/src`.
- Reuse existing feature modules before creating new files: `src/auth`, route-level modules in `src/main.tsx` (until route extraction is introduced).
- New UI components must use PascalCase names and single-responsibility boundaries.
- UI must call BFF endpoints only (`/api/*`), never internal services directly.

### Styling and Tokens

- Styling approach is plain CSS with global tokens in `src/RailFactory.Frontend/App/src/styles.css`.
- IMPORTANT: Never hardcode repeated colors/spacing in JSX; use CSS classes and shared variables in `:root`.
- Keep visual primitives centralized in `styles.css` (`--brand`, `--line`, `--surface`, typography, spacing scales).
- Preserve responsive behavior with existing breakpoints and mobile-first fallbacks.

### Figma MCP Flow

1. Run `get_design_context` for the target node.
2. Run `get_screenshot` for visual parity reference.
3. Adapt generated structure to project conventions (`React + TypeScript + global CSS`).
4. Reuse existing page/layout/form/table blocks before creating new primitives.
5. Validate session-protected behavior on `/app*` routes and ensure UI still depends on BFF session state (`/api/auth/session`).

### Asset and Icon Rules

- Icons are currently sourced from `lucide-react`; do not introduce new icon packages without explicit need.
- IMPORTANT: If Figma MCP returns localhost assets, use those assets directly; do not replace with placeholders.
- Store static assets in `src/RailFactory.Frontend/App/public/`.

### Protected Dashboard Rules

- Keep `/app` as protected entry point; unauthenticated state must redirect to login CTA and never expose internal data.
- Keep navigation, KPI cards, and module panels visually consistent with shared `card`, `module-*`, and `kpi-*` class patterns.
- Any new dashboard widget must provide loading, empty, and error states.
