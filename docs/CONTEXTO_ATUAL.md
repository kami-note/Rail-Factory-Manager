# Contexto Atual de Implementação

**Última Atualização:** 2026-05-31
**Marco Atual:** P10 — Webhooks (RF-22) + Auditoria Imutável (RF-05/RN-08) ✅ implementado

Este documento é a fonte da verdade sobre o estado real do código. Ele descreve o que está "de pé", validado e funcional no monorepo.

---

## 🏁 Estado Geral das Passadas

| Passada | Estado | Destaques |
|---|---|---|
| **P0 - Base Técnica** | ✅ Concluído | Aspire, Gateway YARP, ServiceDefaults, BuildingBlocks, Logs Estruturados. |
| **P1 - IAM & Tenancy** | ✅ Concluído | OAuth2 Google, Isolamento Multitenant (DB por Tenant), Sessão Segura via BFF. |
| **P2 - Entrada & Catálogo**| ✅ Concluído | Importação NF-e (XML/XSD), Associação de Materiais (Workbench), Cadastro de Materiais. |
| **P3 - Conferência & Saldo**| ✅ Concluído | CloseConference, Outbox `receipt_item_conferred`, ConfirmBalance → Available/Blocked + Ledger. |
| **P4 - Produção Inicial** | ✅ Concluído | Work Centers, BOMs versionadas, Ordens de Produção com máquina de estado completa. |
| **P5 - Execução de OP** | ✅ Concluído | Reserva de Estoque → Consumo → Scrap → Inspeção → Conclusão. Fluxo validado end-to-end via frontend. |
| **P6 - Dashboards & Infra** | ✅ Concluído | Todos os KPIs do dashboard implementados: Lead Time, Acuracidade, Scrap, Inspeção, Work Centers. |
| **P7 - Pessoas & Frota** | ✅ Concluído | HR (RF-31, RD-HR-01): Cadastro de pessoas, apontamento de horas. Fleet (RF-25, RF-28, RD-FLE-01): Veículos, capacidade de carga, alocação de motoristas. Modais MUI + ConfirmDialog + 29 testes Playwright. |
| **P8 - Expedição + Fleet Ext.** | ✅ Concluído | Fleet: Manutenção de Veículos (RF-26), Controle de Abastecimento (RF-27). Logistics: Transportadoras (RF-20), Ordens de Expedição (RF-19), Despachos + rastreamento B2B público (RF-21/23/24/RD-LOG-01). Novo microserviço `RailFactory.Logistics.Api`. |
| **P9 - RabbitMQ Logistics** | ✅ Concluído | `logistics.shipment_dispatched` publicado por item via Outbox ao expedir despacho. Inventory Consumer debita saldo `Available` (FIFO, idempotente). Fix `AddShipmentItem` (EF Core 10 + Npgsql bug, raw SQL). |
| **P10 - Webhooks + Auditoria** | ✅ Concluído | RF-22: Carrier ganha `WebhookUrl`, `LogisticsWebhookDispatcher` notifica por HTTP com retry/dead-letter. RF-05/RN-08: `IamAuditEntry` com IP, CorrelationId; trilha de `role_assigned`, `role_revoked`, `session_created`. Frontend: AuditPage + campo webhook no modal de transportadora. |

---

## ✅ Validado End-to-End (2026-05-27)

O fluxo completo de produção foi testado via frontend:

1. Criar BOM → adicionar componente → ativar versão
2. Criar Ordem de Produção (Rascunho)
3. Liberar Ordem (validação de BOM Ativa + WorkCenter Ativo)
4. Iniciar Execução
5. Registrar Consumo de Material
6. Registrar Inspeção de Qualidade (Aprovado/Reprovado)
7. Concluir Ordem
8. Verificar Histórico de Execução

---

## 🏗️ Projetos e Responsabilidades

- **`RailFactory.AppHost`**: Orquestração Aspire (PostgreSQL, Redis, RabbitMQ, Microserviços).
- **`RailFactory.Gateway`**: Ponto único de entrada (YARP) com roteamento tenant-aware.
- **`RailFactory.Frontend`**: BFF .NET + UI React. Gerencia Sessão (Cookies), CSRF e emite JWTs Internos.
- **`RailFactory.Iam.Api`**: Identidade e Acesso. Validação de Sessão, Usuários Locais e RBAC.
- **`RailFactory.Tenancy.Api`**: Catálogo de Tenants. Resolução de connection strings e metadados.
- **`RailFactory.SupplyChain.Api`**: Recebimento, Importação de XML, Associação de Itens e Conferência.
- **`RailFactory.Inventory.Api`**: Gestão de Saldo (Disponível/Bloqueado), Lotes, Validades e Catálogo de Materiais.
- **`RailFactory.Production.Api`**: Work Centers, BOM (versionada), Ordens de Produção, Execução (Consumo, Scrap, Inspeção) e Outbox para reserva de estoque.
- **`RailFactory.HumanResources.Api`**: Cadastro de Pessoas (RF-31), Apontamento de Horas (RD-HR-01).
- **`RailFactory.Fleet.Api`**: Gestão de Veículos (RF-25, RF-28), Alocação de Motoristas (RD-FLE-01), Manutenção (RF-26), Abastecimento (RF-27).
- **`RailFactory.Logistics.Api`**: Transportadoras (RF-20), Ordens de Expedição Draft→Shipped (RF-19), Despachos com rastreamento B2B público (RF-21/23/24/RD-LOG-01).

---

## 🐇 Mensageria (RabbitMQ — ativo desde P6)

Toda comunicação cross-service Outbox → Inventory passa por RabbitMQ.

| Publicador | Exchange | Routing Key | Consumidor |
|---|---|---|---|
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.receipt_item_registered` | Inventory Consumer |
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.receipt_item_conferred` | Inventory Consumer |
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.supplier_material_mapping_created` | Inventory Consumer |
| Production Dispatcher | `railfactory.production` | `production.stock_reservation_requested` | Inventory Consumer |
| Logistics Dispatcher | `railfactory.logistics` | `logistics.shipment_dispatched` | Inventory Consumer |

- **TopologyDeclarator** (Inventory startup): declara exchanges, filas e bindings idempotentemente.
- **DLX**: mensagens com nack sem requeue vão para `railfactory.dead-letters`.
- Endpoints `/api/inventory/internal/*` continuam ativos para compatibilidade; serão removidos no futuro.

---

## 🔐 Dev Bypass (Desenvolvimento apenas)

Para testes automatizados e desenvolvimento sem Google OAuth, o BFF suporta o header `X-Dev-User` quando `IsDevelopment() == true`.

**Como funciona:**
- `GET /api/iam/auth/session` com `X-Dev-User: email@exemplo.com` retorna sessão autenticada fake com `SystemPermissions.All()`.
- O reverse proxy do BFF detecta `X-Dev-User` antes de limpar os headers, emite um Internal JWT assinado diretamente e o injeta na requisição downstream — sem chamar o IAM.
- O header é removido antes de chegar a qualquer microserviço downstream (`StripSensitiveProxyHeaders`).

**Arquivos relevantes:**
- `src/RailFactory.Frontend/Api/FrontendAuthSessionEndpoint.cs`
- `src/RailFactory.Frontend/Api/FrontendEndpoints.cs`

---

## 🪝 Frontend Hooks (padrão desde P6)

Padrão `UseQueryResult<T>` com `{ data, loading, error, reload }` implementado em:

| Hook | Feature | Uso |
|---|---|---|
| `useWorkCenters` | production/hooks | WorkCentersPage |
| `useBoms` | production/hooks | BomsPage |
| `useProductionOrders` | production/hooks | ProductionOrdersPage |
| `useInventoryBalances` | inventory/hooks | InventoryStocksPage |
| `useProductionDashboard` | dashboard/hooks | OverviewPanel |
| `useInventoryDashboard` | dashboard/hooks | OverviewPanel |

Helper base: `src/shared/lib/useQuery.ts` — AbortController para cleanup, revision counter para `reload()`.

---

## 🛡️ Protocolos Ativos (Mandatos)

1. **BFF-Driven Status**: Nenhuma cor ou label de status é hardcoded no Frontend. O backend retorna `DisplayStatus` (Key, Label, Color).
2. **Internal JWT Auth**: Comunicação entre serviços usa Bearer JWT assinado pelo BFF, com bind de tenant (`tenant` claim).
3. **Value Objects**: Identificadores críticos (MaterialCode, FiscalId, EmailAddress) são Value Objects no Domain.
4. **Hexagonal Integrity**: Domínios são isolados de infraestrutura; Portas (Interfaces) definem contratos de persistência e integração.

---

## ✅ Validado End-to-End P7 (2026-05-27)

Testes Playwright (29/29 ✅) cobrem:
- Navegação sidebar PESSOAS / FROTA
- Modais de criação: Centro de Trabalho, Pessoa, Veículo
- ConfirmDialog para inativar/ativar nos três módulos
- Criação real via API e verificação na tabela
- Validação de campos obrigatórios e conversão automática para maiúsculo

Requisitos atendidos em P7: RF-25, RF-28, RF-31, RD-HR-01, RD-FLE-01

---

## ✅ P8 — Expedição + Fleet Extensions (2026-05-28)

**Backend:**
- `RailFactory.Fleet.Api` estendido: `VehicleMaintenancePlan`, `FuelingRecord` + migration `AddMaintenanceAndFueling` + 8 novos endpoints
- `RailFactory.Logistics.Api` criado do zero: `Carrier`, `ShipmentOrder` (Draft→Shipped), `Dispatch` (Pending→Delivered) + B2B público `GET /api/logistics/public/dispatches/{trackingCode}` sem auth
- Freight calculado como `max(totalKg × RatePerKg, totalCbm × RatePerCbm)`
- Migration EF Core `InitialLogisticsP8` gerada

**Infra:**
- AppHost: 2 novos bancos (`tenant-dev-logisticsdb`, `tenant-acme-logisticsdb`) + serviço `logistics`
- Gateway: rota `/api/logistics/{**catch-all}` + cluster `logistics`
- Tenancy: seeds de connection string `logisticsdb` para ambos os tenants
- `SystemPermissions.Logistics` (`logistics.read`, `logistics.write`) + permissões no frontend

**Frontend:**
- Fleet: `MaintenancePage`, `FuelingPage` (selector de veículo + tabela + modal inline)
- Logistics: `CarriersPage`, `ShipmentOrdersPage`, `DispatchPage` (ciclo completo)
- Sidebar: seção EXPEDIÇÃO (Transportadoras, Ordens, Despachos) + extensões FROTA (Manutenção, Abastecimento)
- TypeScript: 0 erros nos arquivos P8 (erros pré-existentes em testes não alterados)

Requisitos atendidos em P8: RF-19, RF-20, RF-21, RF-23, RF-24, RF-26, RF-27, RD-LOG-01

---

## ✅ P9 — RabbitMQ Logistics (2026-05-28)

**Logistics (publisher):**
- `LogisticsOutboxMessage` domain entity + migration `AddLogisticsOutbox`
- `ShipDispatch` persiste outbox com payload completo (dispatchId, trackingCode, items com itemId)
- `LogisticsInventoryDispatcher` (BackgroundService): SKIP LOCKED, EventId determinístico (MD5 de outboxId+itemId), max 10 tentativas antes de dead-letter

**Inventory (consumer):**
- `InventoryBalance.Debit(qty)` — novo método para debitar saldo `Available`
- `DebitInventoryForDispatch` use case — FIFO, idempotente via `IntegrationMessage`, ledger `stock_dispatched`
- `TopologyDeclarator` + `InventoryIntegrationConsumer` extendidos com canal de logistics

**Bug fixes:**
- `AddShipmentItem` corrigido com `AddItemDirectAsync` (raw SQL) — mesmo quirk EF Core 10 + Npgsql 10 já corrigido no `AddBomItem`
- `CreateDispatch` chamava `order.MarkShipped()` incorretamente ao criar o despacho; removido — a transição de estado do pedido agora ocorre apenas em `ShipDispatch.ExecuteAsync` (quando o caminhão parte de facto)

---

## ✅ P10 — Webhooks (RF-22) + Auditoria Imutável (RF-05/RN-08) (2026-05-31)

**RF-22 Webhooks:**
- `Carrier` ganha campo `WebhookUrl` (string?, max 2000) + migration `AddCarrierWebhookUrl`
- `ShipDispatch` e `DeliverDispatch` criam outbox entry `"logistics.webhook_notification"` na mesma transação (se carrier tem webhook URL; URL capturada no payload — imutável)
- `LogisticsWebhookDispatcher` (BackgroundService, poll 5s): lê entries do tipo `webhook_notification`, POST para URL externa com `X-Idempotency-Key`, retry até 10x, dead-letter em falha persistente
- `IntegrationConstants.LogisticsEvents.WebhookNotification` adicionado
- Frontend: campo "URL de Webhook (opcional)" no `CreateCarrierModal`

**RF-05/RN-08 Auditoria:**
- `IamAuditEntry` domain entity com: Action, ActorEmail, AffectedEmail, IpAddress (IPv6-safe, 45 chars), CorrelationId, MetadataJson (jsonb), OccurredAt
- Migration `AddIamAuditEntries` → tabela `iam_audit_entries` com índice em `occurred_at DESC`
- `AssignRoleToUser` registra `"role_assigned"` com IP extraído de X-Forwarded-For
- `RemoveRoleFromUser` registra `"role_revoked"` com mesmo padrão
- `HandleGetSession` registra `"session_created"` a cada login autenticado
- `GET /api/iam/admin/audit` com filtros `action`, `actorEmail`, `from`, `to` + paginação (requer `iam.read`)
- Frontend: `AuditPage` com tabela paginada + filtro de ação por ToggleButtonGroup; sidebar IAM item "TRILHA DE AUDITORIA"

## 🚀 Próximos Passos (P11+)

Candidatos restantes (todos ❌ P10 original):
- RF-06: API Key management
- RF-07: Recuperação de conta e MFA
- RF-29: Roteirização inteligente (Intelipost/RoutEasy)
- RF-30: Telemetria básica de frota (Cobli)
- RF-35: Mapas de calor de entrega
- RF-37: Exportação PDF/Excel/CSV
- RF-38: Dashboard de custos
- Integrações externas: PlugNotas (SEFAZ), Asaas (financeiro), Omie (contábil)

---

*Para histórico detalhado de mudanças, consulte o log do Git.*
