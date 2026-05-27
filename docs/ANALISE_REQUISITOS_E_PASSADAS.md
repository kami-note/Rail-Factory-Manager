# Análise dos Requisitos e Passadas

Este documento mapeia os requisitos canônicos do PDF e os requisitos derivados para a **ordem de construção** (Passadas P0–P10). A lista de requisitos está em `REQUISITOS.md`; o backlog executável está em `PLANO_DE_TASKS.md`.

**Regra de priorização:**
- Primeiro o que desbloqueia outros fluxos.
- Depois o que usa dados reais já criados.
- Integrações externas completas entram após a regra interna estar estável.
- Recursos avançados não entram antes de existir dado operacional confiável.

---

## 1. Tabela de Passadas

| Passada | Nome | Objetivo |
|---|---|---|
| P0 | Base técnica | Estrutura da solução, AppHost, infra, Gateway, BFF, defaults, logs e health checks |
| P1 | IAM e tenant dev | OAuth Google, usuário, sessão simples, tenant `dev`, tenant resolver |
| P2 | Entrada de materiais | Recebimento por upload/XML/manual, Inventory próprio e saldo pendente |
| P3 | Conferência e saldo | Conferência cega, divergências, saldo bloqueado/disponível |
| P4 | Produção inicial | Produtos/materiais, dimensões, BOM, Work Centers e OP básica |
| P5 | Execução de OP | Reserva, consumo, scrap, paradas, qualidade, lotes e ledger |
| P6 | Dashboard inicial | Indicadores simples com dados reais e inventário global |
| P7 | Pessoas e frota base | Pessoas, horas, veículos, capacidade e alocação |
| P8 | Expedição base | Picking/packing, embarque, transportadoras, frete e status B2B simples |
| P9 | Integrações e recursos avançados | PlugNotas real, API keys, MFA, tracking, webhooks, OEE completo, relatórios |
| P10 | Endurecimento final | Outbox amplo, observabilidade, segurança, performance, docs e deploy |

*Status atual: consultar `CONTEXTO_ATUAL.md`.*

---

## 2. Matriz Requisito → Passada

### IAM

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-01 | Frontend/API mínimos, credenciais OAuth | Login Google funcional com sessão | P1 |
| RF-02 | Modelo de tenant e banco inicial | Tenant `dev` seedado; gestão completa depois | P1, volta P9 |
| RF-03 | Usuário autenticado | Autorização mínima; granular depois | P1, volta P5/P9 |
| RF-04 | Login funcionando | Sessão simples; revogação/timeout depois | P1, volta P9 |
| RF-05 | Usuário, tenant e recursos sensíveis | Auditoria básica em entradas/OP; append-only completo depois | P2, volta P5/P10 |
| RF-06 | Integrações externas reais | Adiar até webhooks/API B2B | P9 |
| RF-07 | Login estável e usuários reais | Adiar até hardening de segurança | P9 |
| RD-IAM-01 | RF-02 e RN-01 | `X-Tenant-Code: dev` obrigatório nas APIs tenant-aware | P1 |

### Supply Chain

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-16 | IAM, tenant, recebimento | Upload/XML/manual de NF-e; PlugNotas real depois | P2, volta P9 |
| RF-17 | Recebimento e itens | Conferente registra contagem sem ver quantidade esperada | P3 |
| RF-18 | Divergência detectada | Devolução simples vinculada ao recebimento/divergência | P3, volta P8/P9 |
| RD-SUP-01 | RF-16 | Fallback manual/upload como caminho oficial inicial | P2 |
| RD-SUP-02 | RF-16 | Interface interna de provider; PlugNotas/SEFAZ ficam substituíveis | P2, volta P9 |

### Inventory

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RD-INV-01 | RF-16 | API Inventory e banco próprio por tenant; saldo pendente/disponível/bloqueado | P2/P3 |
| RD-INV-02 | RD-INV-01, RF-11, RF-12 | Ledger de entrada, liberação, bloqueio, reserva, consumo, scrap e ajuste | P3/P5 |
| RD-INV-03 | RD-INV-01 | Consulta consolidada para Admin Matriz | P6 |

### Production

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-08 | Produto/material básico | BOM com versão ativa | P4 |
| RF-09 | Cadastro operacional | CRUD de Work Center | P4 |
| RF-10 | RF-08, RF-09 | OP com estados principais | P4, volta P5 |
| RF-11 | RF-10, RD-INV-01 | Reserva no Inventory ao liberar OP | P5 |
| RF-12 | RF-11, OP em execução | Scrap com motivo e impacto no ledger | P5 |
| RF-13 | RF-09, OP opcional | Parada com causa, início e fim | P5 |
| RF-14 | RF-10 | Inspeção obrigatória antes de finalizar OP | P5 |
| RF-15 | RD-INV-01, RF-12 | Lote acabado ligado aos insumos | P5, volta P9 |
| RD-PRD-01 | Cadastro de produto | Dimensões para cubagem/frete/carga | P4, volta P8 |

### Dashboard e Reporting

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-34 | RF-10, RF-13, RF-14 | Indicadores simples; OEE completo depois | P6, volta P9 |
| RF-35 | RF-21, dados de entrega/localização | Adiar até tracking/logística avançada | P9 |
| RF-36 | Eventos reais de estoque/produção | Alertas simples; tempo real depois | P6, volta P9 |
| RF-37 | Consultas estáveis | Exportação depois dos dashboards básicos | P9 |
| RF-38 | RF-12, RF-15, RD-INV-02 | Custos depois de consumo/scrap/lotes | P9 |

### HR

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-31 | Tenant | Pessoa básica sem acesso ao sistema | P7 |
| RF-32 | RF-31 | Competências simples por pessoa | P9 |
| RF-33 | Não definido no PDF | **Não implementar nem reutilizar sem decisão** | N/A |
| RD-HR-01 | RF-31, usuário/tenant | Apontamento simples de horas | P7 |
| RD-HR-02 | RD-HR-01, integração externa | Exportação/envio contábil depois | P9 |
| RD-HR-03 | RF-31 | Turnos simples; regras avançadas depois | P9 |

### Fleet

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-25 | Tenant | Veículo com placa, documentos e status | P7 |
| RF-26 | RF-25 | Plano simples por data/km | P8, volta P9 |
| RF-27 | RF-25, RF-31 | Registro simples de abastecimento | P8 |
| RF-28 | RF-25, RF-31 | Vínculo motorista-veículo por período | P7 |
| RF-29 | Logistics com entregas, RF-25 | Adiar até expedição/tracking existirem | P9 |
| RF-30 | RF-25, eventos externos/manual | Adiar até frota estar operacional | P9 |
| RD-FLE-01 | RF-25 | Capacidade de carga em peso/volume | P7, volta P8 |

### Logistics

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RF-19 | Produto acabado/saldo expedível | Separação e embalagem simples | P8 |
| RF-20 | Expedição básica | Transportadora e tabela simples | P8 |
| RF-21 | Despacho criado | Status manual/inicial; tracking externo depois | P8, volta P9 |
| RF-22 | RF-21, RF-06, RD-TEC-01 | Webhook com retry/outbox depois | P9 |
| RF-23 | RF-19 | Conferência de volumes antes da saída | P8 |
| RF-24 | RF-20, RD-PRD-01, RD-FLE-01 | Frete simples por tabela/cubagem | P8, volta P9 |
| RD-LOG-01 | RF-21 | Endpoint B2B simples de consulta de status | P8, volta P9 |

### Técnicos, UI e Documentação

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RD-TEC-01 | Fluxos com mudança de estado | Contratos de evento primeiro; RabbitMQ real nos fluxos críticos | P3/P5, volta P10 |
| RD-EDGE-01 | BFF, Gateway e IAM mínimos | UI no browser chama BFF; BFF encaminha pelo Gateway | P1 |
| RD-AUD-01 | RF-05, RN-08 | Política fail-closed/fail-open definida por tipo de ação | P2, volta P10 |
| RD-UI-01 | Frontend base | Responsivo desde as telas iniciais | P1, volta contínua |
| RD-DOC-01 | Fluxos implementados | Documentação técnica, manual e deploy final | P10 |

### Não Funcionais

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| NF-01 | APIs/integrações | Erros padronizados e retry simples | P1, volta P9/P10 |
| NF-02 | Eventos críticos | Outbox em recebimento/reserva/finalização/despacho | P3/P5/P8, volta P10 |
| NF-03 | AppHost/serviços | Logs, health checks e tracing básico | P0/P1, volta P10 |
| NF-04 | Endpoints e dados reais | Medição e ajustes depois dos fluxos | P10 |
| NF-05 | Auth e dados sensíveis | Segredos protegidos; TLS/criptografia formal depois | P1, volta P10 |
| NF-06 | Serviços stateless | Evitar estado local desde o início | P0, volta P10 |
| NF-07 | Tenant com locale/timezone | `pt-BR` inicial e campos preparados | P1, volta P9 |

### Regras de Negócio

| ID | Precisa antes | Primeira entrega útil | Passada |
|---|---|---|---|
| RN-01 | RF-02, RD-IAM-01 | Toda API tenant-aware exige tenant | P1 |
| RN-02 | RF-01, RF-03 | Autorização mínima; granular depois | P1, volta P5/P9 |
| RN-03 | RF-10, RF-11, RD-INV-01 | Reserva ao liberar OP | P5 |
| RN-04 | RF-14 | Bloquear finalização sem qualidade | P5 |
| RN-05 | RF-17 | Ocultar quantidades esperadas até fechar contagem | P3 |
| RN-06 | RF-17, RF-18 | Bloquear material divergente até decisão | P3 |
| RN-07 | RF-23 | Bloquear saída com divergência | P8 |
| RN-08 | RF-05, RF-06, RF-03 | Auditoria básica primeiro; correlação completa depois | P2, volta P10 |

---

## 3. Decisões de Correção Arquitetural

| Área | Problema a evitar | Decisão de implementação |
|---|---|---|
| Inventory | Saldo duplicado em Supply Chain e Production | Criar `Inventory` como bounded context próprio desde P2 |
| Inventory | Production recalcular saldo ou consumir direto | Production deve reservar/consumir por contrato do Inventory |
| Supply Chain | Entrada virar estoque disponível cedo demais | Recebimento cria saldo pendente; conferência libera ou bloqueia |
| Tenant | Eventos sem tenant ou dependentes de HTTP | Todo evento, outbox e job carrega `tenantCode` explicitamente |
| Borda | BFF e Gateway duplicando regra | UI → BFF → Gateway → Serviços |
| Auditoria | Falha silenciosa em ação sensível | Definir fail-closed para segurança e registro local para fluxo operacional |
| Dashboard | Relatório consultar vários bancos e recriar regra | Dashboard lê read model/consulta consolidada, sem calcular saldo |
