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

## P4 - Produção Inicial ✅

Objetivo: Estruturar Work Centers, BOM e Ordens de Produção no `RailFactory.Production.Api`.

> **Escopo cortado intencionalmente:** sem capacity em WorkCenter, sem FinishedGood duplicado do Inventory, sem PlannedStartDate na OP, sem evento BomVersionSuperseded (nenhum consumidor em P4).

### P4.1 — Work Centers ✅

**Domínio:**
- `WorkCenter` — `Id`, `Code` (Value Object), `Name`, `Status` (`Active`/`Inactive`).
- Guard: não pode inativar se houver OP `Released` ou `InExecution` vinculada.

**Application:**
- `IWorkCenterRepository` — Save, GetById, List.
- Use cases: `CreateWorkCenter`, `DeactivateWorkCenter`, `ActivateWorkCenter`.

**API:**
- `POST /work-centers`
- `GET /work-centers`
- `GET /work-centers/{id}`
- `PUT /work-centers/{id}/deactivate`
- `PUT /work-centers/{id}/activate` ← *adicionado em P6*

**Aceite:** Work Center criado, listado, ativado e inativado. Guard impede inativação com OP ativa vinculada. Validado via frontend.

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

**Nota técnica:** `AddBomItem` usa raw SQL (`AddItemDirectAsync`) para contornar quirk de `ValueGeneratedOnAdd` no EF Core 10 + Npgsql 10. Ver `ISSUES_CONHECIDOS.md #3`.

**Aceite:** BOM criada com itens. Ativação troca versão anterior para `Draft` automaticamente. Validado via frontend.

---

### P4.3 — Ordem de Produção (OP) ✅

**Domínio:**
- `ProductionOrder` — `Id`, `OrderNumber`, `ProductCode` (`MaterialCode`), `BomId`, `PlannedQuantity`, `WorkCenterId`, `Status`.
- Estados: `Draft` → `Released` → `InExecution` → `Completed` | `Cancelled`.
- Guards:
  - `Release()`: exige BOM com status `Active` e WorkCenter `Active`.
  - `Cancel()`: permitido em `Draft` e `Released`; bloqueado em `InExecution` e `Completed`.
- `Release()` persiste evento `ProductionOrderReleased` no Outbox.

**Application:**
- `IProductionOrderRepository`.
- Use cases: `CreateProductionOrder`, `ReleaseProductionOrder`, `CancelProductionOrder`.

**API:**
- `POST /production-orders`
- `PUT /production-orders/{id}/release`
- `PUT /production-orders/{id}/cancel`
- `GET /production-orders` (filtros: `status`, `workCenterId`)
- `GET /production-orders/{id}`

**Aceite:** OP criada, liberada (valida BOM e WorkCenter ativos), cancelada (bloqueada se `InExecution`). Evento `ProductionOrderReleased` no Outbox ao liberar. Validado via frontend.

---

### P4 — Infraestrutura

| Item | Ação |
|---|---|
| `AppHost` | `production-db` para o tenant `dev`. ✅ |
| `Gateway` | Rotas `/production/*` com JWT interno. ✅ |
| `ProductionDbContext` | Tabelas: `work_centers`, `boms`, `bom_items`, `production_orders`, `production_outbox`. ✅ |
| `Frontend` | Work Centers, BOMs, Ordens de Produção. ✅ |

---

## P5 - Execução de OP ✅

Objetivo: Implementar fluxo completo de execução: Reserva de Estoque → Consumo → Scrap → Inspeção → Conclusão.

### P5.1 — Reserva de Estoque (via RabbitMQ)

- [x] Evento `stock_reservation_requested` emitido pelo Production Dispatcher ao liberar OP.
- [x] Inventory Consumer processa a reserva e altera saldo de `Available` → `Reserved`.
- [x] TopologyDeclarator declara exchanges, filas e bindings no startup do Inventory.
- [x] DLX configurado para mensagens rejeitadas (`railfactory.dead-letters`).

### P5.2 — Execução da OP

- [x] `PUT /production-orders/{id}/start-execution` — altera status para `InExecution`.
- [x] `POST /production-orders/{id}/executions` — registra consumo, scrap e inspeção de qualidade.
- [x] `PUT /production-orders/{id}/complete` — conclui a OP (requer inspeção aprovada).
- [x] `GET /production-orders/{id}/executions` — lista histórico de execuções.

### P5.3 — Frontend

- [x] `ProductionOrdersPage` — botões de Liberar, Iniciar, Concluir, Cancelar por status.
- [x] Modal de registro de consumo de material (`MaterialCodeAutocomplete`).
- [x] Modal de registro de inspeção de qualidade.
- [x] Histórico de execuções por OP.

**Aceite:** Fluxo completo validado end-to-end via frontend (2026-05-27):
1. Criar BOM → adicionar componente → ativar versão
2. Criar Ordem de Produção (Rascunho)
3. Liberar Ordem (validação de BOM Ativa + WorkCenter Ativo)
4. Iniciar Execução
5. Registrar Consumo de Material
6. Registrar Inspeção de Qualidade (Aprovado)
7. Concluir Ordem
8. Verificar Histórico de Execução

---

## P6 - Dashboards & KPIs ✅

Objetivo: KPIs reais de produção e inventário visíveis no painel principal.

### P6.1 — Dashboards Backend ✅

- [x] `GET /api/production/dashboard` — ordens ativas/concluídas, top scrap, taxa de inspeção.
- [x] `GET /api/inventory/dashboard` — totais de materiais, saldo disponível, saldo reservado.

### P6.2 — Dashboard Frontend (parcial)

- [x] `OverviewPanel` conectado com dados reais de produção e inventário.
- [x] Exibir taxa de aprovação em inspeção no painel (card KPI + painel de inspeções).
- [x] Exibir top scrap por material no painel (tabela com top 5).

### P6.3 — Métricas Avançadas (pendente)

- [x] Desempenho por Work Center — taxa de conclusão de OPs com barra de progresso.
- [x] Lead Time médio de OPs — card KPI no OverviewPanel (horas ou dias).
- [x] Acuracidade de Estoque — `Available / (Available + Blocked)` com barra visual e métricas.

---

## P7 a P10: Visão de Longo Prazo

- **P7 - Pessoas & Frota**: Gestão de operadores e veículos.
- **P8 - Expedição**: Picking, Packing e Despacho de Produto Acabado.
- **P9 - Integrações**: Sefaz real, Webhooks e APIs B2B.
- **P10 - Hardening**: Auditoria imutável, performance e segurança avançada.

---
*Nota: Este plano é atualizado dinamicamente conforme a evolução dos domínios.*
