# Workspace Memory: Rail-Factory-Fork

This file tracks architectural decisions, session status, and implementation milestones.

## Session Milestone: BOM Evolution - Phase 2: Technical Scrap & Costing Roll-up (2026-06-09)

### Implemented Features
- **Technical Scrap Factor (Fator de Perda Técnica):**
  - Configured `ScrapFactor` validation bounds ($[0, 1)$) at both the domain model and client-side levels.
  - Generated database migration `AddBomItemScrapFactor` in the Production database context to persist the scrap factor column.
  - Placed input field in the frontend `AddBomItemForm.tsx` that accepts percentage inputs (0% to 99.99%) and normalizes to decimals before backend submission.
  - Displayed the scrap factor percentage in the BOM item table on `BomCard.tsx`.
- **Costing Roll-up (Custo Teórico da BOM):**
  - Created `IMaterialCostProvider` Application port decoupled from the Supply Chain boundary.
  - Implemented `HttpMaterialCostProvider` adapter that queries the `/api/supply-chain/internal/material-costs` endpoint.
  - Registered the named HttpClient `supply-chain-integration` using Aspire service discovery and the adapter in `ProductionModule.cs`.
  - Implemented the `GetBomCostRollup` Use Case that fetches component purchase prices, scales quantities by batch size, applies technical loss, and aggregates the total theoretical cost.
  - Exposed the `GET /api/production/boms/{id}/cost-rollup` route in `ProductionEndpoints.cs`.
  - Added a "Custo" button to `BomCard.tsx` and implemented `BomCostRollupModal.tsx` in Portuguese (Brazil) with local currency formatting (BRL) to display the detailed cost rollup breakdown.
- **Bugfixes & Optimizations (Timeout Resolution):**
  - **Service Discovery Configuration:** Added `.WithReference(supplyChain)` to the `production` project in the Aspire AppHost ([Program.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.AppHost/Program.cs)). This injects the service discovery environment variables needed for the production API to resolve `http://supply-chain`, eliminating HTTP client lookup timeout issues.
  - **Query Performance Optimization:** Optimized the `HandleGetMaterialCosts` database query in [SupplyChainEndpoints.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.SupplyChain.Api/Api/SupplyChainEndpoints.cs) to run its grouping and sorting logic in-memory. This prevents complex subqueries or window functions on PostgreSQL, preventing DB-level query hangs or locks.
  - **Internal Endpoint Security:** Fixed a `401 Unauthorized` issue on `/api/supply-chain/internal/material-costs` by moving the route out of the public JWT-secured `secureGroup` and registering a new `internalGroup` mapping in [SupplyChainEndpoints.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.SupplyChain.Api/Api/SupplyChainEndpoints.cs). The endpoint is now validated via the `ValidateInternalApiKeyAsync` filter using the shared internal API key, aligning with patterns established for Inventory internal routes.
  - **LINQ Translation Bugfix:** Resolved a `409 Conflict` (due to an untranslatable LINQ expression) by mapping the string filter array to a list of `MaterialCode` Value Objects. This allows EF Core to translate `.Contains(...)` using the mapped value converter without attempting to translate member property access (`.Value`) in the SQL query. Projecting the full `InternalMaterialCode` entity value and resolving `.Value` in-memory completes the fix.

### Verification & Tests
- Added domain and application tests in `RailFactory.Production.Api.Tests` for invalid scrap factors boundaries and `GetBomCostRollup` costing calculation accuracy.
- Ran backend test suite successfully: 15 passing tests in `Production.Api.Tests` (total 53 passing tests).
- Ran frontend test suite successfully: 38 passing tests.
- Vite build completed successfully with zero compilation warnings/errors.

## Session Milestone: BOM Evolution - Phase 1 (2026-06-09)

### Implemented Features
- **BOM Version Cloning (Clone BOM):**
  - Implemented the `CloneAsDraft` method on the `BillOfMaterials` domain aggregate. It copies all items and properties to a new aggregate instance in `Draft` status.
  - Implemented the `CloneBomVersion` Application Use Case. It calculates the next available version number automatically, saves the cloned parent BOM, and copies items using a direct raw SQL insert pattern to bypass EF Core 10/Npgsql 10 change-tracking PK constraints.
  - Exposed the `POST /api/production/boms/{id}/clone` endpoint in `ProductionEndpoints.cs` and registered `CloneBomVersion` in the DI container.
  - Integrated the "Clonar" action button in `BomCard.tsx` on the frontend, mapping the API call from `production.ts` and updating state in `BomsPage.tsx`.
- **BOM Batch Size support (Lote Padrão):**
  - Added the `BatchSize` property (decimal, default 1.0) with validation (`> 0`) on `BillOfMaterials` aggregate.
  - Created a EF Core DB migration `AddBomBatchSize` adding the `batch_size` column to the `boms` table.
  - Mapped `BatchSize` inside `ProductionDbContext.cs` and exposed it via API responses.
  - Adjusted the required quantity reservation calculations in `ProductionInventoryDispatcher.cs` to scale dynamically based on the BOM's batch size: `requiredQuantity = (item.Quantity * payload.PlannedQuantity) / bom.BatchSize`.
  - Added a "Lote Padrão" field to `CreateBomModal.tsx` and displayed it on the UI as a chip next to component count on `BomCard.tsx`.

### Verification & Tests
- Created a xUnit test project `RailFactory.Production.Api.Tests` containing:
  - Domain tests validating `BatchSize` constraint checks and `CloneAsDraft` item copy behavior on the BOM aggregate.
  - Application use case tests verifying `CloneBomVersion` mock repository executions and key-not-found exceptions.
- Solution compiles cleanly (0 C# warnings/errors) and all 10 tests in the new project pass.
- Compiled the frontend React application bundle successfully (`npm run build`) with zero compilation warnings or type errors.

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

## Session Milestone: Verification of Asaas Payment Integration (2026-06-07)

### Verification & Tests
- Automated E2E verification using Playwright:
  - Validated that the "Pagamento" category card is visible in the Services and Integrations grid.
  - Validated that the "Conectar" button triggers the configuration modal for "Pagamento".
  - Validated that the provider "Asaas" is selectable in the modal.
  - Verified that the "Credenciais" tab contains the "Access Token" (required password), "Tipo de Cobrança" (BOLETO placeholder), and "URL Base (opcional)" fields.
  - Verified that the "Webhook" tab contains the "Token do Webhook" field.
  - Verified that the "Emitente" tab is correctly empty for Asaas.
  - Verified that the Webhook URL points to `/api/logistics/webhooks/asaas/{tenantCode}`.
- Captured screenshots:
  - [01-integrations-grid.png](file:///home/levi/.gemini/antigravity-cli/brain/1c1f6af3-1413-4b34-b2b9-eb9d1422ecde/01-integrations-grid.png)
  - [02-modal-credentials.png](file:///home/levi/.gemini/antigravity-cli/brain/1c1f6af3-1413-4b34-b2b9-eb9d1422ecde/02-modal-credentials.png)
  - [03-modal-webhook.png](file:///home/levi/.gemini/antigravity-cli/brain/1c1f6af3-1413-4b34-b2b9-eb9d1422ecde/03-modal-webhook.png)

## Session Milestone: Multitenant Image Storage with MinIO (2026-06-08)

### Implemented Features
- **MinIO Aspire Integration:**
  - Added MinIO container orchestration to Aspire AppHost ([Program.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.AppHost/Program.cs)) with dynamic port mappings and standard dev credentials (`minioadmin`).
  - Configured environment variables passing endpoint, access keys, and bucket names to the Frontend BFF.
- **Multitenant Image Storage Abstraction:**
  - Implemented [MinioImageStorage.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/Infrastructure/MinioImageStorage.cs) implementing `IImageStorage` using the official `AWSSDK.S3` library.
  - Automatically isolates uploads by tenant code using directory prefixes (`{tenantCode}/{fileName}`) inside the `railfactory-images` bucket.
  - Dynamically routes files starting with `person_` to the Human Resources image endpoint, and all others to the Inventory image endpoint.
- **Employee (Pessoa) Image Support:**
  - Added `ImageUrl` (nullable string, max length 2000) property to the `Person` domain model ([Person.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.HumanResources.Api/Domain/Person.cs)) and configured EF Core mapping in [HrDbContext.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.HumanResources.Api/Infrastructure/Persistence/HrDbContext.cs).
  - Created and ran EF Core migrations to update the `people` database table schema for all tenants.
  - Implemented the `UpdatePersonImage` Use Case ([UpdatePersonImage.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.HumanResources.Api/Application/People/UpdatePersonImage.cs)) and mapped the new `PUT /api/hr/people/{id}/image` endpoint in [HrEndpoints.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.HumanResources.Api/Api/HrEndpoints.cs).
- **Frontend Enhancements:**
  - Integrated beautiful, premium upload & preview interfaces with dashed borders, hover "Alterar" overlays, and loading indicators for both [MaterialDetailsPage.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/inventory/components/MaterialDetailsPage.tsx) and [PersonDetailPanel.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/hr/components/PersonDetailPanel.tsx).
  - Displayed a small profile avatar for employees within the [PeoplePage.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/hr/components/PeoplePage.tsx) grid, falling back to a clean placeholder icon if no photo has been uploaded.

### Verification & Tests
- Created [MinioImageStorageTests.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend.Tests/MinioImageStorageTests.cs) to test the tenant routing path formatting rules.
- Solution builds successfully (0 errors, 0 warnings).
- All frontend assets compiled successfully (`vite build`).
- Ran all tests: 17 passing tests in `RailFactory.Frontend.Tests`, 18 in `RailFactory.Tenancy.Api.Tests`, and 17 in `RailFactory.SupplyChain.Api.Tests`.

## Session Milestone: Integration Fields Grid Layout (2026-06-08)

### Implemented Features
- **Arranged Integration Configuration Fields in a Grid Layout:**
  - Added a smart helper `getFieldSize` in `ConfigureIntegrationModal.tsx` that determines grid column sizes dynamically based on the field key.
  - Aligned shorter keys, documents (CNPJ/CPF, IE), contact info, postal codes, and address elements (street, number, complement, district, state, city, IBGE) in clean multi-column rows using MUI's modern `size` property.
  - Kept longer credentials (API keys, OAuth2 tokens, URLs) full-width (`xs: 12`) to accommodate length.
  - Replaced deprecated grid props (`xs={12} sm={...}`) in `ConfigureIntegrationModal.tsx` with modern responsive layout spans (`size={size}`).
  - Updated legacy grid layout parameters in `IntegrationsPage.tsx` from `item xs={12} md={6} lg={4}` to `size={{ xs: 12, md: 6, lg: 4 }}`.

### Verification & Tests
- Created a unit test suite [ConfigureIntegrationModal.test.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/integrations/__tests__/ConfigureIntegrationModal.test.tsx) verifying modal elements render correctly and load pre-selected integration fields.
- Ran and validated the entire frontend test suite successfully (27 tests across 7 files).

## Session Milestone: Frontend Input Masks & Client-Side Validation (2026-06-08)

### Implemented Features
- **Centralized Masks and Validation Utilities:**
  - Created a unified `masks.ts` helper library containing formatting masks and verification algorithms (verification digits check for CPF and CNPJ, pattern matching for Emails, CEPs, Phones, and Mercosul/standard license plates).
- **Integrated Form Formatting & Validation:**
  - **`CreatePersonModal`**: Formats input as CPF on the fly; validates CPF and Email format, showing error visual indicators and disabling submit on invalid values.
  - **`CreateCarrierModal`**: Formats CNPJ; validates CNPJ, Email, and Webhook URL (strictly requiring http/https prefixes).
  - **`CreateShipmentOrderModal`**: Formats recipient CPF/CNPJ and CEP dynamically; validates CPF/CNPJ, Email, CEP, and UF state length.
  - **`FiscalSettingsPage`**: Formats emitter CNPJ; validates CNPJ, preventing save on invalid CNPJ inputs.
  - **`ConfigureIntegrationModal`**: Dynamically resolves and applies masks/validations on dynamic schema fields (CNPJ, CPF/CNPJ, CEP, Phone, and Email keys).
  - **`CreateVehicleModal`**: Formats Plate, validates license plate formats (Mercosul and standard), Renavam (9-11 digits), Chassi (17 characters), and RNTRC (8 digits).
  - **`UsersManagementPage`**: Validates the invite email address format, visually flagging invalid inputs and disabling the confirm button until resolved.
  - **`AssociationWorkbenchPage` & `AssociationWorkspace`**: Standardized SKU Interno to uppercase and stripped spaces; enforces numeric formatting constraints and validation on NCM (exactly 8 digits) and GTIN (valid sizes: 8, 12, 13, 14 digits) during custom material creation.
- **Hardened Submit Buttons**:
  - Dynamically disabled the save/submit actions on all updated forms if any input fails client-side verification, ensuring zero invalid format submissions to the backend.
- **Backend Formatting & Sanitization Alignment**:
  - *Constraint Analysis*: Checked the backend Value Object `FiscalId.cs`, which normalizes tax IDs to digits-only. Other fields (carrier CNPJ, employee CPF, recipient CNPJ, recipient CEP, vehicle plate) are currently stored as raw strings in the domain model and are passed uncleaned to third-party fiscal/shipping APIs (FocusNFe, PlugNotas, Melhor Envio). Formatting characters (dots, dashes) in these fields cause API validation errors and SEFAZ rejections.
  - *Clean Submission*: Updated all frontend form submit payloads to strip formatting (cleaning documents, ZIP codes, and plates to digits/alphanumeric only) before posting to the backend APIs.
  - *Format on Display*: Imported `Masks` in list pages (`CarriersPage.tsx`, `PeoplePage.tsx`, `FleetPage.tsx`), detail panels (`VehicleDetailPanel.tsx`, `ShipmentOrderDetailPanel.tsx`), print views (`DispatchPrintView.tsx`), and creation modals (`CreateDispatchModal.tsx`) to format the raw data back into clean, user-friendly Brazilian standard formats for the user.

### Verification & Tests
- Created [masks.test.ts](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/shared/lib/__tests__/masks.test.ts) unit test verifying formatters and mathematical digit validation check rules.
- Ran all tests: 37 passing unit tests across 8 test files (`npm run test`).
- Production frontend bundle compiles successfully with zero warnings or errors (`npm run build`).

## Session Milestone: Materials Card Grid View & Visual Catalog (2026-06-08)

### Implemented Features
- **Visual Materials Card Layout (`InventoryBalanceCardList`):**
  - Designed a highly polished, responsive card view (`xs: 12`, `sm: 6`, `md: 4`, `lg: 3` grid) for the inventory stocks listing.
  - Cards feature the material's image (loaded from `b.materialImageUrl`) with a hover zoom effect. If no image exists, a beautiful gradient fallback generated from the deterministic HSL color of the material code is shown with a Lucide package icon in the center.
  - Highlights critical information:
    - **Saldo Disponível**: Rendered in a large bold metric block with the unit of measure.
    - **Lote / Validade**: Styled lot codes and expiry dates with custom Lucide icons.
    - **Origem / Fornecedor**: Uses the unified `StatusChip` to display the purchase/production origin and supplier names.
    - **Status**: Visual status tags overlayed cleanly on top of the image area.
- **Dynamic View Layout Toggle:**
  - Added a responsive layout switcher (`ToggleButtonGroup` with `LayoutGrid` and `List` icons) on `InventoryStocksPage.tsx`.
  - Remembers the user's preference by saving it to local storage (`inventory_view_mode`), defaulting to the visual cards view (`'grid'`).
- **Tests & Verification:**
  - Validated that the existing unit tests pass cleanly under the new card rendering architecture.

### Verification & Tests
- Ran frontend unit tests successfully: 8 test files, 37 tests passing.
- Checked C# and TypeScript code compilation.

## Session Milestone: Dynamic Connection String Port Rewriting (2026-06-08)

### Implemented Features
- **Dynamic Connection String Port & Credential Rewriting (`Tenancy.Api`):**
  - Problem: In local development with .NET Aspire, restarting Aspire launches the PostgreSQL container on a new dynamic host port. Since the tenant catalog database resides in a persistent docker volume, stored connection strings for active tenants point to stale ports, causing connection refusal errors (`SocketException 111`) in client microservices background dispatchers in an infinite loop.
  - Solution: Modified [PostgresTenantRepository](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Tenancy.Api/Infrastructure/PostgresTenantRepository.cs) to inject `IConfiguration` and retrieve the current `postgres` connection string.
  - Implementation: In the `ToTenant` mapping method, if the `postgres` server connection string is configured and the stored connection string points to `localhost` or `127.0.0.1`, it dynamically replaces the `Host`, `Port`, `Username`, and `Password` of the stored connection string with those of the current container instance. This propagates the corrected connection settings to all consuming services automatically.
- **Robust Readiness Checks (`BuildingBlocks`):**
  - Wrapped `OpenAsync` inside the `try-catch` block in [TenantServiceReadiness.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.BuildingBlocks/Tenancy/TenantServiceReadiness.cs).
  - This ensures that if a database server is momentarily unreachable (e.g. during startup delays or restarts), background dispatchers receive a clean `false` readiness indication instead of throwing raw connection exceptions that pollute host diagnostics logs.
- **Clarified Blind Conference Purpose On-Screen (`Frontend`):**
  - Modified the informational banner in [ConferenceWorkspace.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/supply-chain/components/ConferenceWorkspace.tsx).
  - Explicitly states that the blind conference is validating the physical delivery against the supplier's invoice (NF-e) to ensure only correct counts enter the warehouse stock, resolving any conceptual ambiguity directly for the operator.

### Verification & Tests
- Created [PostgresTenantRepositoryTests.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Tenancy.Api.Tests/PostgresTenantRepositoryTests.cs) containing unit and integration tests (using an in-memory SQLite provider) to validate the rewriting logic under various conditions.
- Ran all tests: 21 passing tests in `RailFactory.Tenancy.Api.Tests` (previously 18) and 17 in `RailFactory.SupplyChain.Api.Tests`. All build and test suites pass cleanly.
- Compiled the production React frontend bundle successfully (`npm run build`) with zero compilation errors.

## Session Milestone: Zero Stock Catalog Items Visibility & History Hardening (2026-06-08)

### Implemented Features
- **Zero-Stock Catalog Items Visibility:**
  - Standardized the synthetic balance items generated for registered catalog materials with no active stock balances. They are returned by the backend as zero-quantity `Available` balances under the respective source categories (Purchase raw materials vs. Production finished goods).
- **Disabled History for Synthetic Balances:**
  - Modified the frontend views in [InventoryBalanceTable.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/inventory/components/InventoryBalanceTable.tsx) (desktop table, mobile list, and grid card view) to disable the "Histórico" action buttons for synthetic stock records (having `id = '00000000-0000-0000-0000-000000000000'`).
  - Added a descriptive tooltip `"Item sem movimentações no estoque"` (Item has no stock movements yet) on hover to provide clear guidance and prevent operators from viewing empty history or getting `404` errors.
- **Tests & Verification:**
  - Fixed compilation errors in backend unit tests [ListInventoryBalancesTests.cs](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Inventory.Api.Tests/ListInventoryBalancesTests.cs) (aligned parameters order for `Material.Create` and expected prefix `catalog-init`).
  - Added frontend unit test to [InventoryStocksPage.test.tsx](file:///home/levi/Projects/Rail-Factory-Fork/src/RailFactory.Frontend/App/src/features/inventory/__tests__/InventoryStocksPage.test.tsx) verifying that synthetic zero-stock items disable the stock history button correctly.
  - Successfully executed and verified all C# and TypeScript tests, and successfully completed the production build.
