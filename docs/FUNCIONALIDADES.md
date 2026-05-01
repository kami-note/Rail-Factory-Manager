# Funcionalidades Por Dominio

Este documento explica o que cada dominio faz. A lista de requisitos canonica esta em `REQUISITOS.md`; a ordem de implementacao esta em `ANALISE_REQUISITOS_E_PASSADAS.md`.

## 1. Dominios

| Dominio | Responsabilidade | Requisitos relacionados |
|---|---|---|
| Frontend | Operacao do usuario via BFF .NET + React/Vite | RD-UI-01, RD-EDGE-01 |
| Gateway | Entrada unica das APIs com YARP | RD-EDGE-01 |
| IAM | Identidade, acesso e sessao | RF-01 a RF-07, RD-IAM-01 |
| Tenancy | Resolucao e isolamento de tenant | RF-02, RD-IAM-01, RN-01 |
| Supply Chain | Entrada de materiais | RF-16 a RF-18, RD-SUP-01, RD-SUP-02 |
| Inventory | Estoque, saldo, reserva, bloqueio e movimentacao | RD-INV-01 a RD-INV-03, RN-03, RN-06 |
| Production | Chao de fabrica | RF-08 a RF-15, RD-PRD-01 |
| Dashboard | Indicadores e relatorios | RF-34 a RF-38, RD-INV-03 |
| HR | Pessoas, horas e apoio a alocacao | RF-31, RF-32, RD-HR-01 a RD-HR-03 |
| Fleet | Veiculos e frota | RF-25 a RF-30, RD-FLE-01 |
| Logistics | Expedicao e entrega | RF-19 a RF-24, RD-LOG-01 |

## 2. Funcionalidades Por Passada

### P0 - Base Tecnica

Status: concluido inicial. A base operacional foi implementada e validada; a fonte de estado atual e `CONTEXTO_ATUAL.md`.

Funcionalidades:

- [x] AppHost.
- [x] Projetos iniciais.
- [x] Banco PostgreSQL.
- [x] Tenant Catalog separado.
- [x] Bancos tenant `dev` para IAM, SupplyChain, Inventory e Production.
- [x] Redis.
- [x] RabbitMQ.
- [x] Gateway YARP.
- [x] Frontend BFF .NET + React/Vite.
- [x] Health checks basicos.
- [x] OpenTelemetry basico.
- [x] Resiliencia HTTP inicial.
- [x] Logs estruturados com `correlationId` explicito.
- [x] Padrao de erro com `ProblemDetails`.
- [x] Contratos HTTP iniciais.
- [x] BuildingBlocks com Domain, Result/Error, ports de repositorio/eventos e contexto de tenant.
- [x] Contrato padrao de evento com `tenantCode`, `correlationId`, `producer` e versao.

Objetivo: ter a base para implementar os dominios sem improvisar estrutura.

### P1 - IAM E Tenant Dev

Funcionalidades:

- [x] Tenant `dev` persistido no Tenant Catalog.
- [x] Endpoint de leitura do tenant via Gateway.
- [ ] `X-Tenant-Code`.
- [ ] Tenant resolver.
- [ ] Middleware tenant-aware.
- [ ] OAuth Google.
- [ ] Usuario autenticado.
- [ ] Sessao simples.
- [ ] Autorizacao minima.
- [ ] Frontend responsivo inicial.
- [ ] BFF mantendo cookie/sessao e repassando chamadas ao Gateway.

Responsabilidades fixas:

- UI no browser chama apenas BFF.
- BFF cuida de cookie, sessao, CSRF e usuario atual.
- Gateway roteia para os servicos e normaliza `X-Tenant-Code`.
- Servicos validam tenant/permissao antes da regra de negocio.

Nao entra ainda:

- MFA.
- API keys.
- RBAC granular completo.
- Gestao avancada de tenants.

### P2 - Entrada De Materiais

Funcionalidades:

- Criar recebimento.
- Upload/importacao XML ou entrada manual.
- Provider interno para NF-e, preparado para PlugNotas/SEFAZ depois.
- Registrar itens recebidos.
- Criar saldo pendente no Inventory.
- Inventory exposto como API propria, com banco proprio por tenant.
- Auditoria basica da entrada.

Nao entra ainda:

- Integracao PlugNotas real.
- Automacao fiscal completa.
- Polling/webhook externo.

### P3 - Conferencia E Saldo

Funcionalidades:

- Conferencia cega.
- Registro de contagem.
- Comparacao apenas apos fechar a contagem.
- Classificacao simples de divergencia.
- Saldo disponivel quando aprovado.
- Saldo bloqueado quando divergente.
- Devolucao simples vinculada ao recebimento/divergencia.
- Ledger minimo para entrada, bloqueio, liberacao e devolucao.

### P4 - Producao Inicial

Funcionalidades:

- Produto/material basico.
- Dimensoes de produto.
- BOM versionada.
- Work Centers.
- OP com estados principais.

Motivo: Production so deve entrar depois de existir material/saldo real para consumir.

### P5 - Execucao De OP

Funcionalidades:

- Reserva de material no Inventory.
- Inicio de OP.
- Consumo.
- Scrap.
- Paradas.
- Qualidade.
- Lote/rastreabilidade inicial.
- Ledger de movimentacoes.

Regra: Production nao recalcula saldo. Production solicita reserva/consumo ao Inventory e reage ao resultado.

### P6 - Dashboard Inicial

Funcionalidades:

- Indicadores simples de entrada.
- Indicadores simples de estoque.
- Indicadores simples de producao.
- Visao global de inventario para matriz.
- Alertas simples se ja houver evento confiavel.

Nao entra ainda:

- OEE completo.
- Exportacao.
- Mapas.
- Custos completos.

### P7 - Pessoas E Frota Base

Funcionalidades:

- Cadastro de pessoas sem acesso ao sistema.
- Apontamento simples de horas.
- Cadastro de veiculos.
- Capacidade de carga do veiculo.
- Vinculo motorista/veiculo.

Motivo: Fleet depende de pessoas; Logistics depois usa veiculos e motoristas.

### P8 - Expedicao Base

Funcionalidades:

- Picking/packing.
- Volumes.
- Conferencia de embarque.
- Transportadoras.
- Calculo simples de frete.
- Status simples de despacho.
- API B2B simples de consulta de status.
- Manutencao e abastecimento basicos se forem necessarios para demonstracao.

### P9 - Integracoes E Recursos Avancados

Funcionalidades:

- PlugNotas/SEFAZ real.
- API keys.
- MFA.
- RBAC granular.
- Gestao completa de sessoes.
- Tracking externo.
- Webhooks externos.
- Roteirizacao.
- Telemetria.
- Competencias.
- Turnos e escalas.
- Integracao contabil.
- OEE completo.
- Custos.
- Mapas.
- Exportacoes.

### P10 - Endurecimento Final

Funcionalidades:

- Outbox nos fluxos criticos.
- Observabilidade completa.
- Testes de performance.
- Validacao de escalabilidade.
- Seguranca formal.
- Auditoria completa com correlacao.
- Manual do usuario.
- Documentacao tecnica.
- Procedimentos de deploy.

## 2.1. Contratos Entre Dominios

| Origem | Destino | Contrato inicial | Observacao |
|---|---|---|---|
| Supply Chain | Inventory | Criar saldo pendente | A entrada nao gera saldo disponivel antes da conferencia |
| Supply Chain | Inventory | Liberar ou bloquear saldo | Resultado da conferencia cega |
| Supply Chain | Inventory | Registrar devolucao | Usado quando ha divergencia/defeito |
| Production | Inventory | Reservar material | Obrigatorio antes de liberar OP |
| Production | Inventory | Consumir reserva | Baixa acontece contra reserva, nao contra saldo livre |
| Production | Inventory | Registrar scrap | Deve afetar ledger e rastreabilidade |
| Logistics | Inventory | Separar/baixar produto acabado | Entra na expedicao base |
| Dashboard | Inventory/Production/Supply | Ler read models ou consultas consolidadas | Nao deve duplicar regra de saldo |

Eventos podem ser definidos desde cedo, mas a publicacao por RabbitMQ/Outbox entra somente quando o fluxo precisar de retry, multiplos consumidores ou garantia de entrega.

## 3. Sistema De Medidas (UoM)

Comportamento inicial:

- A BOM define a UoM.
- A reserva usa a UoM da BOM.
- Consumo e scrap usam a UoM da reserva.
- Conversao automatica de UoM nao entra no primeiro ciclo de Production.

Evolucao:

1. Permitir UoM de entrada no consumo/scrap.
2. Resolver regra de conversao ativa.
3. Calcular quantidade canonica.
4. Registrar regra aplicada.
5. Auditar conversao.

## 4. Criterio Para Entrar Agora

Uma funcionalidade entra agora quando:

- desbloqueia o proximo fluxo;
- reduz retrabalho;
- respeita tenant;
- usa dados reais ou prepara dado que sera usado logo;
- pode ser entregue sem criar complexidade artificial.

Uma funcionalidade fica para depois quando:

- depende de dados que ainda nao existem;
- exige integracao externa ainda nao validada;
- so faz sentido com alto volume de uso;
- seria apenas demonstrativa neste momento.
