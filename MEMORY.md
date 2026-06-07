# Workspace Memory: Rail-Factory-Fork

This file tracks architectural decisions, session status, and implementation milestones.

## Session Milestone: Inventory Enhancements (2026-06-05)

### Implemented Features
- **Filter and Search Tools on Inventory Page (`/app/inventory`):**
  - Added a search bar for filtering materials by name, code, lot number, supplier name, or source reference.
  - Added a dropdown to sort inventory balances by Material Name (A-Z/Z-A), Quantity (highest/lowest first), and Creation Date (newest/oldest first).
  - Added a checkbox to hide zero or negative inventory stocks (`quantity <= 0`), which is crucial for reducing noise in warehouse logs.
  - Added a status chip filter to filter items dynamically by status (Todos, Pendente, Disponível, Bloqueado).
  - Added a "Limpar Filtros" (Clear Filters) action button for resetting all parameters back to default.
- **KPI Summary Cards:**
  - Added 3 top-level KPI metrics cards at the top of the inventory page: "Lotes em Exibição", "Quantidade Total", and "Lotes Bloqueados" using the shared `StatCard` component.
  - These metrics update dynamically as the search criteria and filters are adjusted, giving operators a quick summary of their current view.

### Verification & Tests
- Created a comprehensive unit test suite: [InventoryStocksPage.test.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/inventory/__tests__/InventoryStocksPage.test.tsx)
- Ran and passed both the new and existing inventory tests successfully (6 tests passed across 2 test files).

## Session Milestone: MDF-e and Fiscal Profile Implementation (2026-06-07)

### Implemented Features
- **RNTRC Support for Vehicles (`Fleet.Api`):**
  - Added `Rntrc` (Registro Nacional de Transportadores Rodoviários de Carga) property to the [Vehicle](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Fleet.Api/Domain/Vehicle.cs) domain entity and mapped it in EF Core.
  - Added vehicle creation support in frontend modal [CreateVehicleModal.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/fleet/components/CreateVehicleModal.tsx) and backend endpoints.
- **Tenant Fiscal Profile (`Logistics.Api`):**
  - Implemented [TenantFiscalProfile](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Domain/TenantFiscalProfile.cs) to store default CFOP rates, ICMS percentages, and origin settings.
  - Created a frontend management page [FiscalSettingsPage.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/logistics/components/FiscalSettingsPage.tsx) reachable via the new `CONFIG. FISCAL` navigation item.
- **MDF-e Outbox Integration and Webhook Validation (`Logistics.Api`):**
  - Extended [Dispatch](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Domain/Dispatch.cs) to capture vehicle and driver snapshots at creation time and track MDF-e statuses.
  - Configured [LogisticsFiscalDispatcher](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Integration/LogisticsFiscalDispatcher.cs) to automatically queue MDF-e outbox emission requests once an NF-e is authorized.
  - Implemented `EmitirMdfeAsync` and `EncerrarMdfeAsync` in [MockFiscalIssuerAdapter](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/MockFiscalIssuerAdapter.cs), [PlugNotasAdapter](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/PlugNotasAdapter.cs), and [FocusNfeAdapter](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/FocusNfeAdapter.cs).
  - Fixed a logical bug in [FiscalWebhookHandler](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/FiscalWebhookHandler.cs) by handling correlation matching of tracking codes and separating MDF-e status updates from NF-e status updates.
  - Fixed a dangerous fallback in [PlugNotasAdapter](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/PlugNotasAdapter.cs) where a missing document ID would silently fallback to `request.RefCode`. It now throws an exception since subsequent operations (cancellation, status, closure) strictly require PlugNotas's internal document ID.
  - Hardened NF-e item mapping in [LogisticsFiscalDispatcher](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Integration/LogisticsFiscalDispatcher.cs): removed the unsafe default NCM `"00000000"` fallback (now throws an exception on missing NCM) and replaced the hardcoded `"5102"` CFOP fallback with dynamic resolution from the `TenantFiscalProfile` based on shipping destination (intra-state vs. interstate).
- **DAMDFE Document Print View:**
  - Added a print view [DamdfePrintView.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/logistics/components/DamdfePrintView.tsx) to allow logistics operators to print the fiscal guide (A4 format).

### Verification & Tests
- Updated C# unit tests: [BasicXmlNfeProviderTests.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.SupplyChain.Api.Tests/BasicXmlNfeProviderTests.cs) (fixed EAN assertion to match sentinel logic).
- Ran all tests: 35 passing tests across `Tenancy.Api` and `SupplyChain.Api`.
- Clean compilation of both C# (0 errors) and TS modified files (0 errors).

## Session Milestone: Hardening Fallbacks and Integrations (2026-06-07)

### Implemented Features
- **Outbox Message Locking Optimization:**
  - Modified [LogisticsInventoryDispatcher.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Integration/LogisticsInventoryDispatcher.cs) to explicitly filter the outbox pending query by `"EventType" = 'logistics.shipment_dispatched'`. This stops the background worker from querying and locking unrelated outbox records like webhooks (`logistics.webhook_notification`), eliminating contention and false warning logs.
- **Hardened Logistics Validation Exceptions:**
  - Hardened [LogisticsFiscalDispatcher.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Integration/LogisticsFiscalDispatcher.cs) to throw a descriptive `InvalidOperationException` instead of logging a warning and returning silently when required NF-e recipient fields (CNPJ, State, City, Street, District, Zip Code) or MDF-e vehicle/driver data (Plate, Driver CPF) are missing. This marks the outbox message as failed in the DB with the exact error message, preventing it from being silently marked as dispatched and allowing retries after correction.
- **Freight Mode (modalidade_frete) Realignment:**
  - Set `modalidade_frete` to `0` (CIF - Contratação de Frete por conta do Emitter) in both [FocusNfeAdapter.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/FocusNfeAdapter.cs) and [PlugNotasAdapter.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Logistics.Api/Infrastructure/Adapters/Fiscal/PlugNotasAdapter.cs). This aligns with the third-party carrier transport flow and calculated freight values in dispatches, avoiding SEFAZ inconsistencies and resolving contradictions with linked MDF-e documents.
- **Frontend Test Suite & Configuration:**
  - Fixed [vitest.config.ts](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/vitest.config.ts) to exclude the `e2e` directory from Unit Testing, ensuring Playwright E2E files do not get picked up by Vitest.
  - Corrected [http.test.ts](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/shared/lib/__tests__/http.test.ts) assertion for `fetchJsonOrThrow` to correctly handle `Headers` instances.
- **Production Outbox Loop Prevention:**
  - Fixed a logical bug in [ProductionInventoryDispatcher.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Production.Api/Infrastructure/Integration/ProductionInventoryDispatcher.cs) where unknown/unhandled outbox event types were logged but never marked as dead-lettered, causing the worker to query and lock them in an infinite loop. They are now explicitly marked as dead-lettered on error.

### Verification & Tests
- Compiled the backend successfully (0 errors, 0 warnings).
- Executed backend tests successfully (35 passing tests).
- Executed frontend unit tests successfully (6 test files / 25 tests passing).

