# Contexto Atual de Implementação

**Última Atualização:** 2026-05-27
**Marco Atual:** P6 — Dashboards & KPIs (parcialmente implementado; fluxo completo de produção validado end-to-end)

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

---

## ✅ Validado End-to-End (2026-05-27)

O fluxo completo de produção foi testado via frontend (`https://apparent-driving-horse.ngrok-free.app`):

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

---

## 🐇 Mensageria (RabbitMQ — ativo desde P6)

Toda comunicação cross-service Outbox → Inventory passa por RabbitMQ.

| Publicador | Exchange | Routing Key | Consumidor |
|---|---|---|---|
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.receipt_item_registered` | Inventory Consumer |
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.receipt_item_conferred` | Inventory Consumer |
| SupplyChain Dispatcher | `railfactory.supply-chain` | `supply.supplier_material_mapping_created` | Inventory Consumer |
| Production Dispatcher | `railfactory.production` | `production.stock_reservation_requested` | Inventory Consumer |

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

## 🚀 Próximos Passos (Milestone P6)

**P6 — Dashboards & KPIs (estado atual):**

| Indicador | Backend | Frontend |
|---|---|---|
| Ordens ativas / concluídas | ✅ `GetProductionDashboard` | ✅ Conectado em OverviewPanel |
| Totais de saldo (materiais, disponível, reservado) | ✅ `GetInventoryDashboard` | ✅ Conectado em OverviewPanel |
| Top scrap por material | ✅ `GetProductionDashboard` | ✅ Exibido em tabela no OverviewPanel |
| Taxa de aprovação em inspeção | ✅ `GetProductionDashboard` | ✅ Card KPI + painel de inspeções no OverviewPanel |
| Desempenho por Work Center (taxa de conclusão) | ✅ `GetProductionDashboard` | ✅ Painel de barras no OverviewPanel |
| Lead Time médio de OPs | ✅ `GetProductionDashboard` | ✅ Card KPI no OverviewPanel |
| Acuracidade de Estoque | ✅ `GetInventoryDashboard` | ✅ Barra de progresso + métricas no OverviewPanel |

**Próximas tasks P6:**
1. ~~Exibir taxa de aprovação em inspeção e top scrap no OverviewPanel.~~ ✅ Concluído.
2. ~~Implementar OEE por Work Center no `GetProductionDashboard`.~~ ✅ Implementado como taxa de conclusão por Work Center.
3. ~~Implementar Lead Time médio de OPs.~~ ✅ Concluído.
4. ~~Implementar Acuracidade de Estoque no `GetInventoryDashboard`.~~ ✅ Concluído.

---

*Para histórico detalhado de mudanças, consulte o log do Git.*
