# Plano de Tasks: Rail-Factory Fork

Este documento é o backlog executável do projeto. Cada task deve entregar um valor verificável e aderir aos mandatos do **GEMINI.md**.

## Legenda
- `[ ]` Pendente.
- `[x]` Concluido.
- `Aceite`: Critério de sucesso técnico/funcional.

---

## P0 a P2: Marcos Concluídos ✅
*Para detalhes das implementações anteriores, consulte o histórico no Git.*

- **P0 - Base Técnica**: Aspire, Gateway, BFF, ServiceDefaults, BuildingBlocks.
- **P1 - IAM & Tenancy**: Google SSO, Isolamento de DB por Tenant, Sessão via BFF.
- **P2 - Entrada & Catálogo**: Importação de NF-e, Associação de SKU (Workbench), Cadastro de Materiais.

---

## P3 - Conferência Cega e Saldo Disponível ✅

Objetivo: Transformar o recebimento pendente em saldo real (Disponível ou Bloqueado) através de conferência cega.

### P3.1 - Conferência Cega (SupplyChain)
- [x] **Expandir estados do recebimento**: Adicionar `InConference`, `Approved`, `Divergent`.
- [x] **Comando `StartConference`**: Muda status e bloqueia edições fiscais.
- [x] **Workspace de Conferência na UI**: Operador registra contagem sem ver o esperado (RN-05).
- [x] **Comando `CloseConference`**: Compara contagem vs esperado e define status final (`Approved`/`Divergent`).
- [x] **Emissão de Evento `ReceiptItemConferred`**: Via Outbox (`supply.receipt_item_conferred`). Dispatcher HTTP em `InventoryPendingBalanceDispatcher`.

### P3.2 - Ativação de Saldo (Inventory)
- [x] **Processar `ReceiptItemConferred`**: `ConfirmInventoryBalance` use case no endpoint `/internal/confirmed-balances`.
- [x] **Liberar Saldo `Available`**: `balance.Confirm(isApproved: true)` → status `Available`.
- [x] **Bloquear Saldo `Blocked`**: `balance.Confirm(isApproved: false)` → status `Blocked`.
- [x] **Atualizar Ledger**: `InventoryLedgerEntry` com delta de quantidade e metadados do evento.

---

## P4 - Produção Inicial (PRÓXIMO) 🚀

Objetivo: Estruturar Work Centers, BOM e Ordens de Produção no `RailFactory.Production.Api`.

> **Escopo cortado intencionalmente:** sem capacity em WorkCenter, sem FinishedGood duplicado do Inventory, sem PlannedStartDate na OP, sem evento BomVersionSuperseded (nenhum consumidor em P4).

### P4.1 — Work Centers ✅

**Domínio:**
- `WorkCenter` — `Id`, `Code` (Value Object), `Name`, `Status` (`Active`/`Inactive`).
- Guard: não pode inativar se houver OP `Released` ou `InExecution` vinculada.

**Application:**
- `IWorkCenterRepository` — Save, GetById, List.
- Use cases: `CreateWorkCenter`, `DeactivateWorkCenter`.

**API:**
- `POST /work-centers`
- `GET /work-centers`
- `GET /work-centers/{id}`
- `PUT /work-centers/{id}/deactivate`

**Aceite:** Work Center criado, listado e inativado. Guard impede inativação com OP ativa vinculada.

---

### P4.2 — Bill of Materials (BOM) ✅

**Domínio:**
- `BillOfMaterials` — `Id`, `ProductCode` (`MaterialCode`), `Version` (int), `Status` (`Draft`/`Active`).
- `BomItem` — `MaterialCode`, `Quantity`, `UnitOfMeasure`.
- Invariante: ao ativar uma versão, a versão `Active` anterior volta para `Draft` (lógica interna do aggregate, sem evento).
- Guard: não pode ativar BOM sem ao menos um item.

**Application:**
- `IBomRepository`.
- Use cases: `CreateBom`, `AddBomItem`, `ActivateBomVersion`.

**API:**
- `POST /boms`
- `POST /boms/{id}/items`
- `PUT /boms/{id}/activate`
- `GET /boms?productCode=X`
- `GET /boms/{id}`

**Aceite:** BOM criada com itens. Ativação troca versão anterior para `Draft` automaticamente.

---

### P4.3 — Ordem de Produção (OP) ✅

**Domínio:**
- `ProductionOrder` — `Id`, `OrderNumber`, `ProductCode` (`MaterialCode`), `BomId`, `PlannedQuantity`, `WorkCenterId`, `Status`.
- Estados: `Draft` → `Released` → `InExecution` → `Completed` | `Cancelled`.
- Guards:
  - `Release()`: exige BOM com status `Active` e WorkCenter `Active`.
  - `Cancel()`: permitido em `Draft` e `Released`; bloqueado em `InExecution` e `Completed`.
- `Release()` persiste evento `ProductionOrderReleased` no Outbox (base para P5 reservar estoque).

**Application:**
- `IProductionOrderRepository`.
- Use cases: `CreateProductionOrder`, `ReleaseProductionOrder`, `CancelProductionOrder`.

**API:**
- `POST /production-orders`
- `PUT /production-orders/{id}/release`
- `PUT /production-orders/{id}/cancel`
- `GET /production-orders` (filtros: `status`, `workCenterId`)
- `GET /production-orders/{id}`

**Aceite:** OP criada, liberada (valida BOM e WorkCenter ativos), cancelada (bloqueada se `InExecution`). Evento `ProductionOrderReleased` no Outbox ao liberar.

---

### P4 — Infraestrutura

| Item | Ação |
|---|---|
| `AppHost` | Adicionar `production-db` para o tenant `dev`. |
| `Gateway` | Rotas `/production/*` com JWT interno. |
| `ProductionDbContext` | Tabelas: `work_centers`, `boms`, `bom_items`, `production_orders`, `production_outbox`. |
| `Frontend` | Navegação: Work Centers, BOMs, Ordens de Produção (esqueleto). |

**Sequência de execução:** Domínio → DbContext + Migrations → Repositories + Use Cases → Endpoints + AppHost/Gateway → Testes → UI.

---

## P5 a P10: Visão de Longo Prazo

- **P5 - Execução de OP**: Consumo real de estoque, scrap e apontamento de produção.
- **P6 - Dashboards**: KPIs reais (OEE, Acuracidade de Estoque, Lead Time).
- **P7 - Pessoas & Frota**: Gestão de operadores e veículos.
- **P8 - Expedição**: Picking, Packing e Despacho de Produto Acabado.
- **P9 - Integrações**: Sefaz real, Webhooks e APIs B2B.
- **P10 - Hardening**: Auditoria imutável, performance e segurança avançada.

---
*Nota: Este plano é atualizado dinamicamente conforme a evolução dos domínios.*
