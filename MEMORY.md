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


