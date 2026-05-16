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

## P3 - Conferência Cega e Saldo Disponível (ATUAL) 🚧

Objetivo: Transformar o recebimento pendente em saldo real (Disponível ou Bloqueado) através de conferência cega.

### P3.1 - Conferência Cega (SupplyChain)
- [x] **Expandir estados do recebimento**: Adicionar `InConference`, `Approved`, `Divergent`.
- [x] **Comando `StartConference`**: Muda status e bloqueia edições fiscais.
- [x] **Workspace de Conferência na UI**: Operador registra contagem sem ver o esperado (RN-05).
- [ ] **Comando `CloseConference`**: Compara contagem vs esperado e define status final.
  - Aceite: Se bater -> `Approved`; se divergir -> `Divergent`.
- [ ] **Emissão de Evento `ReceiptItemConferred`**: Via Outbox para sincronização com Inventory.

### P3.2 - Ativação de Saldo (Inventory)
- [ ] **Processar `ReceiptItemConferred`**: Use case no Inventory para ativar saldo pendente.
- [ ] **Liberar Saldo `Available`**: Se aprovado, saldo fica disponível para uso/produção.
- [ ] **Bloquear Saldo `Blocked`**: Se divergente, saldo fica retido para inspeção/devolução.
- [ ] **Atualizar Ledger**: Registrar a transição de status e correção de quantidades.

---

## P4 - Produção Inicial (PRÓXIMO) 🚀

Objetivo: Estruturar OPs, BOM e Work Centers.

- [ ] **P4.1 - Cadastro Industrial**: Materiais (tipo Produto Acabado) e Work Centers.
- [ ] **P4.2 - Bill of Materials (BOM)**: Lista de insumos por produto com versionamento.
- [ ] **P4.3 - Ordem de Produção (OP)**: Criação, planejamento e liberação de OPs.

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
