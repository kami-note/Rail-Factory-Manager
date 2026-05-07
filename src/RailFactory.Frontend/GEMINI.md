# Frontend Architectural Mandate: Rail-Factory-Fork

This document defines the frontend architecture standards to ensure modularity, maintainability, and clear boundaries between domains.

## 1. Feature-Sliced Architecture (Simplified)

We follow a feature-based structure to ensure that domain logic is isolated and easy to reason about.

### 1.1 Directory Structure
All domain logic must reside in `src/features/`. Each feature slice must follow this internal structure:

```text
src/features/[feature-name]/
├── api/          # API calls and React Query hooks
├── components/   # Feature-specific UI components
├── hooks/        # Feature-specific logic hooks
├── types/        # Type definitions for the feature
└── index.ts      # Public API: Export ONLY what is needed by other features/pages
```

### 1.2 The "Shared Kernel" (`src/shared/`)
Common logic, UI primitives, and infrastructure are stored in `src/shared/`.

- `components/`: Generic UI components (Atomic design: atoms/molecules).
- `lib/`: Infrastructure (HTTP client, internationalization, etc.).
- `hooks/`: Generic utility hooks.
- `layouts/`: Application-wide layouts.
- `types/`: Common DTOs and utility types.

## 2. Dependency Rules (The Golden Rules)

1.  **Strict Isolation:** A feature MUST NOT import from the internal directories of another feature.
    - *Bad:* `import { UserCard } from '@/features/auth/components/UserCard'`
    - *Good:* `import { UserCard } from '@/features/auth'` (where UserCard is exported in auth/index.ts)
2.  **Unidirectional Flow:** `Features` -> `Shared`. 
    - `Shared` MUST NEVER depend on `Features`.
3.  **BFF Coupling:** All frontend API calls MUST be directed to the Gateway (BFF). Direct communication with microservices is forbidden.
4.  **Local-First Logic:** If a component or hook is only used within a single feature, keep it inside that feature. Only promote to `src/shared/` if used by 2 or more features.

## 3. Implementation Protocols (Task 2.9.3)

When refactoring the dashboard monolith into slices:
-   **Step 1:** Create the target feature folder (e.g., `src/features/inventory`).
-   **Step 2:** Move relevant components and logic from `features/dashboard`.
-   **Step 3:** Update imports to use absolute paths (`@/features/...` if configured, or relative to `src`).
-   **Step 4:** Ensure `index.ts` exports the main Page components or entry points.

## 4. Documentation
Every component and hook must include TSDoc explaining its purpose and any specific business invariants it enforces.
