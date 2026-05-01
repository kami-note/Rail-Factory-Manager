# Analise Dos Requisitos E Passadas

Este documento transforma os requisitos canonicos do PDF e os requisitos derivados em uma ordem de construcao.

Regra usada na analise:

- primeiro vem o que desbloqueia outros fluxos;
- depois vem o que usa dados reais ja criados;
- integracoes externas completas entram depois da regra interna estar estavel;
- recursos avancados nao entram antes de existir dado operacional confiavel.

## 1. Passadas

| Passada | Nome | Objetivo |
|---|---|---|
| P0 | Base tecnica | Estrutura da solucao, AppHost, infra, Gateway, BFF, defaults, logs e health checks |
| P1 | IAM e tenant dev | OAuth Google, usuario, sessao simples, tenant `dev`, tenant resolver |
| P2 | Entrada de materiais | Recebimento por upload/XML/manual, Inventory proprio e saldo pendente |
| P3 | Conferencia e saldo | Conferencia cega, divergencias, saldo bloqueado/disponivel |
| P4 | Producao inicial | Produtos/materiais, dimensoes, BOM, Work Centers e OP basica |
| P5 | Execucao de OP | Reserva, consumo, scrap, paradas, qualidade, lotes e ledger |
| P6 | Dashboard inicial | Indicadores simples com dados reais e inventario global |
| P7 | Pessoas e frota base | Pessoas, horas, veiculos, capacidade e alocacao |
| P8 | Expedicao base | Picking/packing, embarque, transportadoras, frete e status B2B simples |
| P9 | Integracoes e recursos avancados | PlugNotas real, API keys, MFA, tracking, webhooks, OEE completo, relatorios |
| P10 | Endurecimento final | Outbox amplo, observabilidade, seguranca, performance, escalabilidade, docs e deploy |

Status atual: consultar `CONTEXTO_ATUAL.md`. Em 2026-05-01, P0 foi concluido como base inicial. P1 foi iniciado pelo Tenancy: tenant `dev` persistido no Tenant Catalog, leitura via Gateway validada e proxima task definida como resolver `X-Tenant-Code`.

## 2. Matriz Requisito Por Requisito

### IAM

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-01 | Frontend/API minimos, credenciais OAuth | Login Google funcional com sessao | P1 |
| RF-02 | Modelo de tenant e banco inicial | Tenant `dev` seedado; gestao completa depois | P1, volta P9 |
| RF-03 | Usuario autenticado | Autorizacao minima; granular depois | P1, volta P5/P9 |
| RF-04 | Login funcionando | Sessao simples; revogacao/timeout depois | P1, volta P9 |
| RF-05 | Usuario, tenant e recursos sensiveis | Auditoria basica em entradas/OP; append-only completo depois | P2, volta P5/P10 |
| RF-06 | Integracoes externas reais | Adiar ate webhooks/API B2B | P9 |
| RF-07 | Login estavel e usuarios reais | Adiar ate hardening de seguranca | P9 |
| RD-IAM-01 | RF-02 e RN-01 | `X-Tenant-Code: dev` obrigatorio nas APIs tenant-aware | P1 |

### Supply Chain

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-16 | IAM, tenant, recebimento | Upload/XML/manual de NF-e; PlugNotas real depois | P2, volta P9 |
| RF-17 | Recebimento e itens | Conferente registra contagem sem ver quantidade esperada | P3 |
| RF-18 | Divergencia detectada | Devolucao simples vinculada ao recebimento/divergencia | P3, volta P8/P9 |
| RD-SUP-01 | RF-16 | Fallback manual/upload como caminho oficial inicial | P2 |
| RD-SUP-02 | RF-16 | Interface interna de provider; PlugNotas/SEFAZ ficam substituiveis | P2, volta P9 |

### Inventory

Inventory e requisito derivado porque o PDF fala de estoque, movimentacao, reserva e inventario global, mas nao cria um RF proprio para esse dominio.

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RD-INV-01 | RF-16 | API Inventory e banco proprio por tenant; saldo pendente/disponivel/bloqueado por tenant, material, UoM | P2/P3 |
| RD-INV-02 | RD-INV-01, RF-11, RF-12 | Ledger de entrada, liberacao, bloqueio, reserva, consumo, scrap e ajuste | P3/P5 |
| RD-INV-03 | RD-INV-01 | Consulta consolidada para Admin Matriz | P6 |

### Production

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-08 | Produto/material basico | BOM com versao ativa | P4 |
| RF-09 | Cadastro operacional | CRUD de Work Center | P4 |
| RF-10 | RF-08, RF-09 | OP com estados principais | P4, volta P5 |
| RF-11 | RF-10, RD-INV-01 | Reserva no Inventory ao liberar OP | P5 |
| RF-12 | RF-11, OP em execucao | Scrap com motivo e impacto no ledger | P5 |
| RF-13 | RF-09, OP opcional | Parada com causa, inicio e fim | P5 |
| RF-14 | RF-10 | Inspecao obrigatoria antes de finalizar OP | P5 |
| RF-15 | RD-INV-01, RF-12 | Lote acabado ligado aos insumos | P5, volta P9 |
| RD-PRD-01 | Cadastro de produto | Dimensoes para cubagem/frete/carga | P4, volta P8 |

### Dashboard E Reporting

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-34 | RF-10, RF-13, RF-14 | Indicadores simples; OEE completo depois | P6, volta P9 |
| RF-35 | RF-21, dados de entrega/localizacao | Adiar ate tracking/logistica avancada | P9 |
| RF-36 | Eventos reais de estoque/producao | Alertas simples; tempo real depois | P6, volta P9 |
| RF-37 | Consultas estaveis | Exportacao depois dos dashboards basicos | P9 |
| RF-38 | RF-12, RF-15, RD-INV-02 | Custos depois de consumo/scrap/lotes | P9 |

### HR

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-31 | Tenant | Pessoa basica sem acesso ao sistema | P7 |
| RF-32 | RF-31 | Competencias simples por pessoa | P9 |
| RF-33 | Nao definido no PDF | Nao implementar nem reutilizar sem decisao | N/A |
| RD-HR-01 | RF-31, usuario/tenant | Apontamento simples de horas | P7 |
| RD-HR-02 | RD-HR-01, integracao externa | Exportacao/envio contabil depois | P9 |
| RD-HR-03 | RF-31 | Turnos simples; regras avancadas depois | P9 |

### Fleet

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-25 | Tenant | Veiculo com placa, documentos e status | P7 |
| RF-26 | RF-25 | Plano simples por data/km | P8, volta P9 |
| RF-27 | RF-25, RF-31 | Registro simples de abastecimento | P8 |
| RF-28 | RF-25, RF-31 | Vinculo motorista-veiculo por periodo | P7 |
| RF-29 | Logistics com entregas, RF-25 | Adiar ate expedicao/tracking existirem | P9 |
| RF-30 | RF-25, eventos externos/manual | Adiar ate frota estar operacional | P9 |
| RD-FLE-01 | RF-25 | Capacidade de carga em peso/volume | P7, volta P8 |

### Logistics

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RF-19 | Produto acabado/saldo expedivel | Separacao e embalagem simples | P8 |
| RF-20 | Expedicao basica | Transportadora e tabela simples | P8 |
| RF-21 | Despacho criado | Status manual/inicial; tracking externo depois | P8, volta P9 |
| RF-22 | RF-21, RF-06, RD-TEC-01 | Webhook com retry/outbox depois | P9 |
| RF-23 | RF-19 | Conferencia de volumes antes da saida | P8 |
| RF-24 | RF-20, RD-PRD-01, RD-FLE-01 | Frete simples por tabela/cubagem | P8, volta P9 |
| RD-LOG-01 | RF-21 | Endpoint B2B simples de consulta de status | P8, volta P9 |

### Tecnicos, UI E Documentacao

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RD-TEC-01 | Fluxos com mudanca de estado | Contratos de evento primeiro; RabbitMQ real nos fluxos criticos | P3/P5, volta P10 |
| RD-EDGE-01 | BFF, Gateway e IAM minimos | UI no browser chama BFF; BFF encaminha pelo Gateway; Gateway normaliza tenant | P1 |
| RD-AUD-01 | RF-05, RN-08 | Politica fail-closed/fail-open definida por tipo de acao | P2, volta P10 |
| RD-UI-01 | Frontend base | Responsivo desde as telas iniciais | P1, volta continua |
| RD-DOC-01 | Fluxos implementados | Documentacao tecnica, manual e deploy final | P10 |

### Nao Funcionais

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| NF-01 | APIs/integracoes | Erros padronizados e retry simples | P1, volta P9/P10 |
| NF-02 | Eventos criticos | Outbox em recebimento/reserva/finalizacao/despacho | P3/P5/P8, volta P10 |
| NF-03 | AppHost/servicos | Logs, health checks e tracing basico | P0/P1, volta P10 |
| NF-04 | Endpoints e dados reais | Medicao e ajustes depois dos fluxos | P10 |
| NF-05 | Auth e dados sensiveis | Segredos protegidos; TLS/criptografia formal depois | P1, volta P10 |
| NF-06 | Servicos stateless | Evitar estado local desde o inicio | P0, volta P10 |
| NF-07 | Tenant com locale/timezone | `pt-BR` inicial e campos preparados | P1, volta P9 |

### Regras De Negocio

| ID | Precisa antes | Primeira entrega util | Passada |
|---|---|---|---|
| RN-01 | RF-02, RD-IAM-01 | Toda API tenant-aware exige tenant | P1 |
| RN-02 | RF-01, RF-03 | Autorizacao minima; granular depois | P1, volta P5/P9 |
| RN-03 | RF-10, RF-11, RD-INV-01 | Reserva ao liberar OP | P5 |
| RN-04 | RF-14 | Bloquear finalizacao sem qualidade | P5 |
| RN-05 | RF-17 | Ocultar quantidades esperadas ate fechar contagem | P3 |
| RN-06 | RF-17, RF-18 | Bloquear material divergente ate decisao | P3 |
| RN-07 | RF-23 | Bloquear saida com divergencia | P8 |
| RN-08 | RF-05, RF-06, RF-03 | Auditoria basica primeiro; correlacao completa depois | P2, volta P10 |

## 2.1. Decisoes De Correcao Arquitetural

Estas decisoes resolvem os riscos encontrados na arquitetura original.

| Area | Problema a evitar | Decisao de implementacao |
|---|---|---|
| Inventory | Saldo duplicado em Supply Chain e Production | Criar `Inventory` como bounded context proprio desde P2 |
| Inventory | Production recalcular saldo ou consumir direto | Production deve reservar/consumir por contrato do Inventory |
| Supply Chain | Entrada virar estoque disponivel cedo demais | Recebimento cria saldo pendente; conferencia libera ou bloqueia |
| Tenant | Eventos sem tenant ou dependentes de HTTP | Todo evento, outbox e job carrega `tenantCode` explicitamente |
| Borda | BFF e Gateway duplicando regra | UI no browser -> BFF; BFF -> Gateway; Gateway -> servicos |
| Auditoria | Falha silenciosa em acao sensivel | Definir fail-closed para seguranca e registro local para fluxo operacional |
| Dashboard | Relatorio consultar varios bancos e recriar regra | Dashboard le read model/consulta consolidada, sem calcular saldo |

## 3. Ordem Final Recomendada

### P0 - Base tecnica

Entregar:

- [x] solucao/projetos;
- [x] AppHost;
- [x] PostgreSQL;
- [x] Tenant Catalog separado;
- [x] bancos tenant `dev` por servico inicial: IAM, SupplyChain, Inventory e Production;
- [x] Redis para cache/sessao/estado de auth quando necessario;
- [x] RabbitMQ;
- [x] Gateway YARP;
- [x] Frontend BFF .NET + React/Vite;
- [x] service defaults;
- [x] health checks basicos;
- [x] OpenTelemetry basico;
- [x] resiliencia HTTP inicial;
- [x] logs estruturados com convencao explicita de `correlationId`;
- [x] convencoes de erro;
- [x] contratos HTTP iniciais;
- [x] convencao de evento com `eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantCode`, `correlationId`, `producer` e payload.

### P1 - IAM e tenant dev

Entregar:

- [x] tenant `dev` persistido no Tenant Catalog;
- [x] endpoint de leitura do tenant via Gateway;
- [ ] tenant resolver por `X-Tenant-Code`;
- [ ] middleware tenant-aware;
- [ ] OAuth Google;
- [ ] usuario autenticado;
- [ ] sessao basica;
- [ ] autorizacao minima;
- [ ] frontend responsivo inicial.

Cobre: RF-01, RF-02 minimo, RF-03 minimo, RF-04 minimo, RD-IAM-01, RD-EDGE-01, RN-01, RN-02 minimo, RD-UI-01.

### P2 - Entrada de materiais

Entregar:

- Supply Chain inicial;
- upload/importacao XML ou entrada manual;
- recebimento com itens;
- provider interno substituivel;
- Inventory como API propria;
- banco tenant `dev` de Inventory;
- saldo pendente no Inventory;
- auditoria basica da entrada.

Cobre: RF-16 minimo, RD-SUP-01, RD-SUP-02, RD-INV-01 inicial, RD-AUD-01 inicial, RN-08 minimo.

### P3 - Conferencia e saldo

Entregar:

- conferencia cega;
- comparacao apos contagem;
- divergencia simples;
- bloqueio/liberacao de saldo;
- ledger minimo de entrada, bloqueio, liberacao e devolucao;
- devolucao simples;
- primeiro uso critico de evento/outbox se necessario.

Cobre: RF-17, RF-18 minimo, RD-INV-01 completo, RN-05, RN-06, parte de NF-02.

### P4 - Producao inicial

Entregar:

- produto/material basico;
- dimensoes de produto;
- BOM versionada;
- Work Centers;
- OP com estados principais.

Cobre: RF-08, RF-09, RF-10 minimo, RD-PRD-01 inicial.

### P5 - Execucao de OP

Entregar:

- reserva de material;
- consumo;
- scrap;
- paradas;
- qualidade;
- lote/rastreabilidade inicial;
- ledger de estoque.

Regra: OP nunca baixa estoque diretamente em Production. Toda reserva, consumo e scrap passa por contrato do Inventory.

Cobre: RF-11 a RF-15, RD-INV-02, RN-03, RN-04.

### P6 - Dashboard inicial

Entregar:

- indicadores simples de entrada, estoque e producao;
- saldos por tenant;
- visao global de inventario para matriz;
- alertas simples, se ja houver evento critico confiavel.

Cobre: RF-34 minimo, RF-36 minimo, RD-INV-03.

### P7 - Pessoas e frota base

Entregar:

- cadastro de pessoas;
- apontamento simples de horas;
- cadastro de veiculos;
- capacidade de carga;
- vinculo motorista/veiculo.

Cobre: RF-31, RF-25, RF-28 minimo, RD-HR-01, RD-FLE-01.

### P8 - Expedicao base

Entregar:

- picking/packing;
- conferencia de embarque;
- transportadoras;
- frete simples;
- status de despacho;
- API B2B simples de status;
- manutencao/abastecimento basicos se necessarios para a demonstracao.

Cobre: RF-19, RF-20, RF-21 minimo, RF-23, RF-24 minimo, RD-LOG-01, RF-26 minimo, RF-27 minimo, RN-07.

### P9 - Integracoes e recursos avancados

Entregar:

- PlugNotas/SEFAZ real;
- API keys;
- MFA;
- RBAC granular;
- gestao completa de sessao;
- tracking externo;
- webhooks externos;
- roteirizacao;
- telemetria;
- competencias;
- turnos;
- integracao contabil;
- OEE completo;
- custos;
- mapas;
- exportacoes.

Cobre: complementos de RF-02, RF-03, RF-04, RF-06, RF-07, RF-16, RF-21, RF-22, RF-29, RF-30, RF-32, RF-34 a RF-38, RD-HR-02, RD-HR-03.

### P10 - Endurecimento final

Entregar:

- Outbox nos fluxos criticos;
- observabilidade completa;
- teste de performance;
- validacao de escalabilidade;
- seguranca formal;
- auditoria completa com correlacao;
- manual do usuario;
- documentacao tecnica;
- procedimentos de deploy.

Cobre: NF-01 a NF-07 no nivel final, RN-08 completo, RD-DOC-01.
