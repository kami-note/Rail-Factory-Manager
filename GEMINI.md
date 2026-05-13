# Project Engineering Mandates: Rail-Factory-Fork

This document defines the mandatory engineering standards for all AI agents and developers. These rules are designed to ensure maximum code quality, architectural integrity, and to prevent rework.

## 1. Professional Documentation Standards
All code must be documented using professional XML Documentation (C#) or TSDoc (TypeScript). Comments should focus on **intent, invariants, and architectural decisions**.

### 1.1 C# Example (Backend)
**Bad:**
```csharp
// Saves the product
public async Task Save(Product p) => ...
```

**Good (Mandatory):**
```csharp
/// <summary>
/// Persists a new product within the inventory boundary.
/// </summary>
/// <param name="product">The product aggregate root to persist.</param>
/// <remarks>
/// Invariant: Ensures the Product SKU is unique across the tenant before persistence.
/// This implementation follows the Repository pattern to decouple the Domain from EF Core.
/// </remarks>
/// <exception cref="DuplicateSkuException">Thrown when the SKU already exists.</exception>
public async Task SaveAsync(Product product)
{
    // Implementation with early returns...
}
```

### 1.2 TypeScript Example (Frontend)
**Good (Mandatory):**
```typescript
/**
 * Renders a stylized KPI card for the dashboard.
 * @param title - The display label for the metric.
 * @param value - The numerical value to display.
 * @remarks
 * This component uses Tailwind "module-card" utility classes for visual consistency.
 * Accessibility: Includes aria-labels for screen readers.
 */
const KpiCard: React.FC<KpiProps> = ({ title, value }) => {
  if (!value) return null // Early return
  
  return (
    <div className="module-card p-4 shadow-sm" aria-label={`${title} metric`}>
      <h3 className="text-sm font-medium text-gray-500">{title}</h3>
      <p className="text-2xl font-bold">{value}</p>
    </div>
  )
}
```

## 2. Prevention of Rework & "First-Time Quality" (FTQ)
To avoid constant refactoring, follow these protocols:

- **Research First:** Before creating a new service or utility, `grep` the codebase. 
  *   *Example:* Do not create a new `DateTimeProvider` if `IClock` already exists in `BuildingBlocks`.
- **Architectural Freeze:** Once the `@architect` defines a Port (interface) in the Application layer, the `@backend` agent must implement it exactly. Changes to the contract require a new strategic review.
- **English-First:** All identifiers, comments, and logs must be in technical English to ensure model alignment and project consistency.

## 3. Sequential Specialist Workflow
Complex tasks must follow this chain:
1.  **@lead:** Designs the execution plan in `docs/PLANO_DE_TASKS.md`.
2.  **@architect:** Defines boundaries, contracts, and updates architectural docs.
3.  **@backend / @frontend:** Implements the logic with high-quality documentation.
4.  **@tester:** Provides empirical evidence of success via tests.

## 4. Code Style Guidelines
- **Early Returns:** Prefer `if (!valid) return;` over nested blocks.
- **Explicit over Implicit:** Avoid "magic" logic. Use explicit dependency injection and clear naming.
- **Hexagonal Integrity:** Domain must never reference Infrastructure or Web frameworks.

### 4.1 API Contract Policy (The Monorepo Rule)
To maintain strong decoupling (Hexagonal Integrity), microservices must never share DTO libraries. Instead, internal consumers (like Gateway or other APIs) must maintain their own `private sealed record` copies of the expected response.
**Mandatory Rule:** Because there is no compile-time checking across boundaries, any modification to an API's Response/Request shape (e.g., renaming a field, changing a type from `string` to an `object`) **requires a mandatory global sweep**. You must use `grep_search` to find and update all internal HTTP clients and Frontend types that consume that specific endpoint or DTO. No API shape change is complete without updating its consumers in the same PR.


## 5. Business Identity Integrity (Mandatory)
To prevent duplicates and "identity drift" across microservices, all business-critical identifiers MUST be implemented as **Value Objects** in `RailFactory.BuildingBlocks.Tenancy`.

### 5.1 Required Value Objects
- **MaterialCode:** For SKUs and product identifiers. Enforces `Uppercase` + `Trim`.
- **FiscalId:** For CNPJs, CPFs, and other tax IDs. Enforces `Digits Only`.
- **EmailAddress:** For user identifiers. Enforces `Lowercase` + `Trim`.

### 5.2 Usage Rule
Never use `string` for these identifiers in Domain Models or Application Ports. Always use the corresponding Value Object.
**Example (Mandatory):**
```csharp
// Bad: public string SKU { get; private set; }
// Good: public MaterialCode SKU { get; private set; }
```

## 6. Elite Prevention Protocols (The Audit Standard)
To maintain 100% technical integrity and prevent recurring integration bugs, all implementations MUST adhere to these protocols:

### 6.1 State Transition Hardening (Status Guards)
Every domain method that modifies a `Status` MUST include an explicit guard clause.
**Mandatory Pattern:** `if (Status != Expected) throw new InvalidOperationException(...)`.
Never allow implicit transitions. Critical entities (Receipts, Balances, Orders) must have a rigid state machine.

### 6.2 Outbox Checklist (Cross-Domain Consistency)
Every Use Case that concludes a business phase (e.g., resolving association, closing conference, finalizing OP, releasing shipment) MUST emit a corresponding integration event via the `Outbox` pattern. Master data updates across boundaries MUST NOT be silent.

### 6.3 Identity Propagation (Audit Chain)
The system must maintain a trusted identity chain for all mutation requests:
- **Forwarding:** The BFF/Gateway MUST inject `X-RF-User-Email` and `X-RF-User-Name` headers after validating the session.
- **Consumption:** Use cases MUST capture the real operator identity from the user context for auditing master data changes.

### 6.4 UI Unification & Standard Fidelity
- **Single Source of Status:** All statuses MUST be rendered using the shared `StatusChip.tsx` component. Hardcoding colors, icons, or labels in local feature components is strictly forbidden.
- **Localization:** Operator-facing strings (labels, prompts, error messages) MUST be in **Portuguese (Brazil)** to match the operational context. Technical English is reserved for code, identifiers, and logs.
- **Iconography:** Use `lucide-react` exclusively for all new UI components.

## 7. Dead Code Prevention & Modular Minimality
To keep the codebase lean and prevent "one-off" orphaned classes/functions:

- **Local-First Rule:** If a piece of logic is used in only one place, keep it as a private method or a local function. Do not create a new class or file for it.
- **The Power of Two:** Only promote a function or class to a shared "Utility" or "Service" if it is actually required by two or more different callers.
- **Just-In-Time Implementation:** Do not implement "future-proof" features. Only code what is strictly required for the current task.
- **Orphan Sweep:** At the end of every task, agents must check if any previously created logic became redundant and remove it immediately.

## 8. Agentic Integrity & Tool Usage
To ensure that AI-generated code is production-ready and does not introduce regressions:

- **Zero-Placeholder Policy:** NEVER use placeholders like `...`, `// rest of code`, or `[unchanged]` in tool calls (`replace`, `write_file`). Every code modification must result in syntactically complete and valid code.
- **Contract Verification:** Before implementing a Use Case or Service, the agent MUST explicitly read the definitions of all Ports (interfaces) it intends to use. Relying on memory or "hallucinated" signatures is forbidden.
- **Self-Correction & Build Validation:** If a build fails (`aspire run`, `dotnet build`, `npm run build`), the agent must analyze the exact error message, re-read the relevant files, and apply a surgical fix. Apologies are secondary to empirical resolution.
- **Surgical Edit over Rewrite:** Prefer the `replace` tool for targeted changes in large files to maintain context efficiency and reduce the risk of accidental logic deletion.
