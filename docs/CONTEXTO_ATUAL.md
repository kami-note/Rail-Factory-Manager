# Contexto Atual de Implementação

**Última Atualização:** 2026-05-16
**Marco Atual:** P5 - Execução de OP (Concluído)

Este documento é a fonte da verdade sobre o estado real do código. Ele descreve o que está "de pé", validado e funcional no monorepo.

## 🏁 Estado Geral das Passadas

| Passada | Estado | Destaques |
|---|---|---|
| **P0 - Base Técnica** | ✅ Concluído | Aspire, Gateway YARP, ServiceDefaults, BuildingBlocks, Logs Estruturados. |
| **P1 - IAM & Tenancy** | ✅ Concluído | OAuth2 Google, Isolamento Multitenant (DB por Tenant), Sessão Segura via BFF. |
| **P2 - Entrada & Catálogo**| ✅ Concluído | Importação NF-e (XML/XSD), Associação de Materiais (Workbench), Cadastro de Materiais. |
| **P3 - Conferência & Saldo**| ✅ Concluído | CloseConference, Outbox `receipt_item_conferred`, ConfirmBalance → Available/Blocked + Ledger. |

## 🏗️ Projetos e Responsabilidades

- **`RailFactory.AppHost`**: Orquestração Aspire (PostgreSQL, Redis, RabbitMQ, Microserviços).
- **`RailFactory.Gateway`**: Ponto único de entrada (YARP) com roteamento tenant-aware.
- **`RailFactory.Frontend`**: BFF .NET + UI React. Gerencia Sessão (Cookies), CSRF e emite JWTs Internos.
- **`RailFactory.Iam.Api`**: Identidade e Acesso. Validação de Sessão, Usuários Locais e RBAC inicial.
- **`RailFactory.Tenancy.Api`**: Catálogo de Tenants. Resolução de connection strings e metadados.
- **`RailFactory.SupplyChain.Api`**: Recebimento, Importação de XML, Associação de Itens e Conferência.
- **`RailFactory.Inventory.Api`**: Gestão de Saldo (Disponível/Bloqueado), Lotes, Validades e Catálogo de Materiais.
- **`RailFactory.Production.Api`**: Work Centers, BOM (com versionamento), Ordens de Produção, Execução (Consumo, Scrap, Inspeção) e Outbox para reserva de estoque.

## 🛡️ Protocolos Ativos (Mandatos)

1. **BFF-Driven Status**: Nenhuma cor ou label de status é hardcoded no Frontend. O backend retorna `DisplayStatus` (Key, Label, Color).
2. **Internal JWT Auth**: Comunicação entre serviços usa Bearer JWT assinado pelo BFF, com bind de tenant (`tenant` claim).
3. **Value Objects**: Identificadores críticos (MaterialCode, FiscalId, EmailAddress) são Value Objects no Domain.
4. **Hexagonal Integrity**: Domínios são isolados de infraestrutura; Portas (Interfaces) definem contratos de persistência e integração.

## 🚀 Próximos Passos (Milestone P6)

P5 (Execução de OP) concluído. Fluxo completo: Reserva de Estoque → Consumo → Scrap → Inspeção → Conclusão.

**P6 — Dashboards & KPIs:**
1. OEE (Overall Equipment Effectiveness) por Work Center.
2. Acuracidade de Estoque (saldo real vs esperado).
3. Lead Time médio de OPs por produto.
4. Taxa de Scrap por material.

---
*Para histórico detalhado de mudanças, consulte o log do Git ou o arquivo privado `MEMORY.md`.*
