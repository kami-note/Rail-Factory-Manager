# Plano De Tasks

Este documento transforma a arquitetura, os requisitos e as passadas em uma lista de trabalho executavel.

Regra de uso:

- cada task deve entregar algo verificavel;
- cada passada deve terminar com um fluxo funcionando;
- nao marcar uma task como concluida sem criterio de aceite atendido;
- quando uma task criar duvida de negocio, registrar decisao antes de implementar;
- evitar completar funcionalidades avancadas antes da passada correta.

## Legenda

- `[ ]` Pendente.
- `[x]` Concluido.
- `Aceite`: como saber que a task foi entregue.
- `Depende`: o que precisa existir antes.

## P0 - Base Tecnica

Objetivo: criar a base minima para todos os servicos nascerem do mesmo jeito.

Status atual: consultar `CONTEXTO_ATUAL.md`. Resumo: P0 foi concluido como base inicial; P1 foi iniciado pelo tenant `dev` persistido no Tenant Catalog.

### P0.1 - Estrutura Da Solucao

- [x] Criar solution principal do fork.
  - Aceite: solution abre e lista todos os projetos iniciais.
  - Depende: nenhuma.
  - Entregue: `RailFactory.Fork.sln` e `RailFactory.Fork.slnx`.

- [x] Criar projetos base: AppHost, ServiceDefaults, Gateway, Frontend BFF, Frontend UI, Tenancy, IAM, SupplyChain, Inventory e Production.
  - Aceite: todos os projetos compilam mesmo com implementacao minima.
  - Depende: solution criada.
  - Entregue: projetos criados em `src/` e build validado.

- [x] Criar BuildingBlocks compartilhado sem dependencia de infraestrutura.
  - Aceite: servicos conseguem reutilizar entidades, agregados, eventos de dominio, ports, Result/Error e TenantContext sem acoplar dominio a banco ou transporte.
  - Depende: estrutura inicial criada.
  - Entregue: `RailFactory.BuildingBlocks`.

- [x] Definir nomes padrao de projetos, namespaces e pastas.
  - Aceite: documento ou convencao aplicada nos projetos.
  - Depende: projetos criados.
  - Entregue: prefixo `RailFactory.*` aplicado aos projetos iniciais.

### P0.2 - Aspire E Infra Local

- [x] Configurar AppHost com PostgreSQL.
  - Aceite: Postgres sobe localmente pelo AppHost.
  - Depende: AppHost criado.
  - Entregue: container PostgreSQL sobe pelo Aspire.

- [x] Configurar Tenant Catalog DB separado.
  - Aceite: banco global de tenants existe separado dos bancos operacionais.
  - Depende: Postgres no AppHost.
  - Entregue: recurso `tenantcatalog` no AppHost.

- [x] Configurar bancos tenant `dev` para IAM, SupplyChain, Inventory e Production.
  - Aceite: bancos existem e os servicos conseguem receber suas connection strings.
  - Depende: Postgres no AppHost.
  - Entregue: `tenant-dev-iamdb`, `tenant-dev-supplychaindb`, `tenant-dev-inventorydb` e `tenant-dev-productiondb`.

- [x] Configurar Redis.
  - Aceite: Redis sobe no AppHost e pode ser referenciado pelos servicos.
  - Depende: AppHost criado.
  - Entregue: Redis sobe no AppHost e e referenciado pelo IAM.

- [x] Configurar RabbitMQ.
  - Aceite: RabbitMQ sobe no AppHost, mesmo que ainda nao seja usado por todos os fluxos.
  - Depende: AppHost criado.
  - Entregue: RabbitMQ sobe no AppHost e e referenciado pelo Production.

### P0.3 - Service Defaults

- [x] Configurar health checks padrao.
  - Aceite: cada servico inicial possui endpoint de health.
  - Depende: ServiceDefaults criado.
  - Entregue: endpoints `/health` e `/alive`.

- [x] Configurar logs estruturados.
  - Aceite: logs incluem servico, ambiente e correlation id quando existir.
  - Depende: ServiceDefaults criado.
  - Entregue: OpenTelemetry inclui nome do servico; scope de request inclui `CorrelationId` e `TraceId`. Campo de tenant entra depois do middleware tenant-aware. Implementacao refatorada em modulos de ServiceDefaults.

- [x] Configurar OpenTelemetry basico.
  - Aceite: traces basicos aparecem em execucao local.
  - Depende: ServiceDefaults criado.
  - Entregue: OpenTelemetry configurado no ServiceDefaults.

- [x] Definir contrato padrao de erro inicial.
  - Aceite: APIs retornam erros com formato consistente.
  - Depende: Gateway e servicos iniciais.
  - Entregue: `ProblemDetails` global no ServiceDefaults, excecao nao tratada padronizada e erro de dominio do Tenancy retornando `application/problem+json`.

- [x] Definir politica inicial de resiliencia HTTP.
  - Aceite: chamadas entre BFF, Gateway e servicos possuem timeout, retry simples onde for seguro e erro padronizado.
  - Depende: Gateway e BFF minimos.
  - Entregue: resiliencia HTTP inicial configurada no ServiceDefaults.

### P0.4 - Gateway E BFF Minimos

- [x] Configurar Gateway YARP.
  - Aceite: Gateway roteia para um endpoint de teste de cada servico inicial.
  - Depende: Gateway criado.
  - Entregue: rotas `/api/tenancy`, `/api/iam`, `/api/supply-chain`, `/api/inventory` e `/api/production` validadas.

- [x] Criar BFF .NET minimo.
  - Aceite: BFF responde health e consegue chamar Gateway.
  - Depende: Gateway minimo.
  - Entregue: endpoint `/api/status` chama o Gateway.

- [x] Criar UI React/Vite minima.
  - Aceite: tela inicial carrega via BFF.
  - Depende: BFF minimo.
  - Entregue: UI Vite sobe como `frontend-ui` no AppHost.

### P0.5 - Convencoes Event-Driven E Ports

- [x] Criar contrato de evento de dominio.
  - Aceite: agregados conseguem registrar eventos sem depender de RabbitMQ, MassTransit ou outro transporte.
  - Depende: BuildingBlocks.
  - Entregue: `IDomainEvent`, `AggregateRoot` e primeiro evento `TenantRegisteredDomainEvent`.

- [x] Criar envelope de evento de integracao.
  - Aceite: contrato possui campos minimos para rastreio, tenant e idempotencia.
  - Depende: BuildingBlocks.
  - Entregue: `EventEnvelope<TPayload>` com `EventId`, `EventType`, `OccurredAt`, `TenantCode`, `CorrelationId` e `Payload`.

- [x] Criar port de publicacao de eventos.
  - Aceite: aplicacao depende de uma interface, nao do broker.
  - Depende: BuildingBlocks.
  - Entregue: `IEventPublisher`.

- [x] Criar port de repositorio.
  - Aceite: casos de uso dependem de interface de persistencia.
  - Depende: BuildingBlocks.
  - Entregue: `IRepository<TEntity, TId>` e `ITenantRepository`.

### P0.6 - Contratos HTTP

- [x] Documentar convencoes HTTP iniciais.
  - Aceite: headers, erro padrao e endpoints existentes estao registrados.
  - Depende: ServiceDefaults e Gateway.
  - Entregue: `docs/CONTRATOS_API.md`.

## P1 - IAM E Tenant Dev

Objetivo: permitir login real com Google, sessao no BFF e tenant `dev` obrigatorio.

### P1.1 - Tenant Dev

- [x] Criar entidade de tenant persistida no Tenant Catalog.
  - Aceite: tenant `dev` existe com status ativo, locale `pt-BR` e timezone inicial definido.
  - Depende: Tenant Catalog DB.
  - Entregue: entidade de dominio `Tenant`, evento `TenantRegisteredDomainEvent`, tabela `tenants`, initializer idempotente, seed `dev` e repositorio PostgreSQL.

- [x] Criar leitura inicial do tenant `dev`.
  - Aceite: Gateway retorna dados do tenant `dev` e 404 padronizado para tenant inexistente.
  - Depende: entidade de tenant.
  - Entregue: `GET /api/tenancy/tenants/dev` e `GET /api/tenancy/tenants/{code}` via Gateway.

- [x] Criar resolver de tenant por `X-Tenant-Code`.
  - Aceite: requisicao sem tenant falha; requisicao com `dev` resolve conexao correta.
  - Depende: tenant `dev`.

- [x] Criar middleware de tenant nos servicos tenant-aware.
  - Aceite: IAM, SupplyChain, Inventory e Production recebem tenant resolvido.
  - Depende: resolver de tenant.

- [x] Garantir que jobs/eventos nao dependem de contexto HTTP.
  - Aceite: envelope de evento/job exige `tenantCode`.
  - Depende: convencao de eventos.

- [x] Propagar locale/timezone do tenant.
  - Aceite: APIs tenant-aware conseguem acessar locale/timezone do tenant resolvido.
  - Depende: resolver de tenant.

### P1.2 - OAuth Google

- [ ] Configurar credenciais OAuth Google.
  - Aceite: ambiente local possui configuracao externa, sem segredo hardcoded.
  - Depende: IAM API e BFF.

- [ ] Implementar inicio de login.
  - Aceite: usuario consegue iniciar login pela UI.
  - Depende: OAuth configurado.

- [ ] Implementar callback OAuth.
  - Aceite: Google retorna identidade validada para IAM/BFF.
  - Depende: inicio de login.

- [ ] Criar ou atualizar usuario local apos login.
  - Aceite: usuario autenticado existe no IAM DB do tenant `dev`.
  - Depende: callback OAuth.

### P1.3 - Sessao E Autorizacao Minima

- [ ] Criar sessao no BFF por cookie.
  - Aceite: usuario autenticado permanece logado entre requests.
  - Depende: login Google.

- [ ] Proteger chamadas autenticadas contra CSRF.
  - Aceite: chamadas mutaveis pelo BFF exigem protecao CSRF ou mecanismo equivalente.
  - Depende: sessao no BFF.

- [ ] Criar endpoint de usuario atual.
  - Aceite: UI mostra usuario, tenant e permissoes minimas.
  - Depende: sessao no BFF.

- [ ] Implementar logout.
  - Aceite: cookie/sessao sao encerrados e chamadas protegidas passam a falhar.
  - Depende: sessao no BFF.

- [ ] Implementar autorizacao minima deny by default.
  - Aceite: endpoint protegido falha sem usuario/permissao minima.
  - Depende: usuario atual.

### P1.4 - UI Inicial

- [ ] Criar tela de login.
  - Aceite: botao de login Google inicia fluxo real.
  - Depende: inicio de login.

- [ ] Criar layout autenticado inicial.
  - Aceite: usuario logado ve navegacao basica.
  - Depende: usuario atual.

- [ ] Criar tratamento de erro de autenticacao.
  - Aceite: falhas de login aparecem de forma clara na UI.
  - Depende: fluxo OAuth.

## P2 - Entrada De Materiais E Inventory Inicial

Objetivo: criar o primeiro fluxo de negocio real: recebimento de material e saldo pendente no Inventory.

### P2.1 - Supply Chain Inicial

- [ ] Criar cadastro minimo de fornecedor.
  - Aceite: recebimento pode referenciar fornecedor com identificacao fiscal/nome.
  - Depende: tenant `dev`.

- [ ] Criar modelo de recebimento.
  - Aceite: recebimento possui numero, fornecedor, documento, data, tenant e status.
  - Depende: cadastro minimo de fornecedor.

- [ ] Criar modelo de item de recebimento.
  - Aceite: item possui material, quantidade esperada, UoM e referencia ao recebimento.
  - Depende: modelo de recebimento.

- [ ] Criar entrada manual de recebimento.
  - Aceite: usuario cria recebimento sem integracao externa.
  - Depende: modelos Supply.

- [ ] Criar upload/importacao XML basica.
  - Aceite: XML valido cria recebimento e itens.
  - Depende: modelos Supply.

- [ ] Criar provider interno substituivel de NF-e.
  - Aceite: entrada manual/upload usam interface que depois pode receber PlugNotas/SEFAZ.
  - Depende: entrada manual/upload.

### P2.2 - Inventory Inicial

- [ ] Criar Inventory API.
  - Aceite: Inventory possui health e endpoint protegido.
  - Depende: P0/P1.

- [ ] Criar Inventory DB `dev`.
  - Aceite: migracao inicial cria tabelas de saldo e ledger.
  - Depende: AppHost com banco Inventory.

- [ ] Criar modelo de saldo.
  - Aceite: saldo identifica tenant, material, UoM, status, local de estoque e quantidade.
  - Depende: Inventory DB.

- [ ] Criar cadastro minimo de local de estoque.
  - Aceite: saldo pendente/disponivel pode ser associado a um local fisico ou logico.
  - Depende: Inventory DB.

- [ ] Criar ledger minimo.
  - Aceite: cada alteracao de saldo gera lancamento append-only.
  - Depende: modelo de saldo.

- [ ] Criar contrato `CreatePendingBalance`.
  - Aceite: Supply consegue solicitar saldo pendente para recebimento criado.
  - Depende: Inventory API.

### P2.3 - Integracao Supply -> Inventory

- [ ] Ao criar recebimento, criar saldo pendente.
  - Aceite: recebimento criado gera saldo `Pending` no Inventory.
  - Depende: Supply inicial e `CreatePendingBalance`.

- [ ] Garantir idempotencia basica por recebimento/item.
  - Aceite: retry nao duplica saldo pendente.
  - Depende: ledger minimo.

- [ ] Registrar auditoria basica da entrada.
  - Aceite: criacao de recebimento registra usuario, tenant, data e acao.
  - Depende: usuario autenticado.

### P2.4 - UI De Entrada

- [ ] Criar tela de lista de recebimentos.
  - Aceite: usuario ve recebimentos do tenant `dev`.
  - Depende: API de recebimentos.

- [ ] Criar tela/formulario de novo recebimento.
  - Aceite: usuario cria recebimento manual.
  - Depende: entrada manual.

- [ ] Criar upload de XML.
  - Aceite: usuario importa XML e revisa itens criados.
  - Depende: upload/importacao XML.

## P3 - Conferencia Cega E Saldo Disponivel

Objetivo: transformar recebimento pendente em saldo disponivel ou bloqueado.

### P3.1 - Conferencia Cega

- [ ] Criar status de conferencia no recebimento.
  - Aceite: recebimento passa por estados claros ate aprovado/divergente.
  - Depende: recebimento P2.

- [ ] Criar tela de conferencia sem quantidade esperada.
  - Aceite: conferente nao ve quantidade da NF-e antes de fechar contagem.
  - Depende: lista de itens.

- [ ] Registrar contagem por item.
  - Aceite: usuario informa quantidade contada e UoM.
  - Depende: tela de conferencia.

- [ ] Fechar conferencia.
  - Aceite: sistema compara contagem com esperado apenas no fechamento.
  - Depende: contagem por item.

### P3.2 - Divergencia E Bloqueio

- [ ] Detectar divergencia simples.
  - Aceite: falta, sobra ou defeito muda status do item/recebimento.
  - Depende: fechamento de conferencia.

- [ ] Liberar saldo quando aprovado.
  - Aceite: Inventory muda saldo de `Pending` para `Available`.
  - Depende: Inventory P2.

- [ ] Bloquear saldo quando divergente.
  - Aceite: Inventory muda saldo de `Pending` para `Blocked`.
  - Depende: deteccao de divergencia.

- [ ] Criar devolucao simples.
  - Aceite: divergencia pode gerar registro de devolucao vinculado ao recebimento.
  - Depende: saldo bloqueado.

### P3.3 - Ledger E Eventos Criticos

- [ ] Registrar ledger de liberacao.
  - Aceite: mudanca para `Available` aparece no ledger.
  - Depende: liberacao de saldo.

- [ ] Registrar ledger de bloqueio.
  - Aceite: mudanca para `Blocked` aparece no ledger.
  - Depende: bloqueio de saldo.

- [ ] Definir eventos `MaterialReceiptApproved` e `InventoryBalanceReleased`.
  - Aceite: contratos documentados com envelope padrao.
  - Depende: fluxo de conferencia.

- [ ] Decidir se P3 usa chamada direta ou RabbitMQ.
  - Aceite: decisao registrada antes de implementar mensageria real.
  - Depende: eventos definidos.

## P4 - Producao Inicial

Objetivo: criar a estrutura de producao sem consumir estoque ainda.

### P4.1 - Cadastro Industrial

- [ ] Criar cadastro de material/produto.
  - Aceite: produto possui codigo, nome, tipo, UoM e status.
  - Depende: tenant `dev`.

- [ ] Criar dimensoes de produto.
  - Aceite: produto pode ter peso, volume e dimensoes fisicas.
  - Depende: cadastro de produto.

- [ ] Criar Work Centers.
  - Aceite: maquina/linha/bancada possui codigo, nome, status e capacidade basica.
  - Depende: Production DB.

### P4.2 - BOM

- [ ] Criar BOM.
  - Aceite: produto acabado pode ter lista de insumos.
  - Depende: cadastro de produto.

- [ ] Criar versionamento de BOM.
  - Aceite: BOM possui versao ativa e historico.
  - Depende: BOM criada.

- [ ] Validar BOM ativa.
  - Aceite: OP so usa BOM ativa e vigente.
  - Depende: versionamento.

### P4.3 - Ordem De Producao

- [ ] Criar modelo de OP.
  - Aceite: OP possui numero, produto, quantidade, BOM, tenant e status.
  - Depende: BOM e Work Center.

- [ ] Criar estados principais de OP.
  - Aceite: estados minimos existem: Draft, Planned, Released, InProgress, Completed, Cancelled.
  - Depende: modelo de OP.

- [ ] Criar transicoes validas.
  - Aceite: API bloqueia transicao invalida.
  - Depende: estados principais.

- [ ] Criar UI basica de OP.
  - Aceite: usuario cria e visualiza OP.
  - Depende: API de OP.

## P5 - Execucao De OP

Objetivo: fazer Production usar saldo real do Inventory.

### P5.1 - Reserva De Material

- [ ] Criar contrato `ReserveMaterial`.
  - Aceite: Production solicita reserva informando OP, material, quantidade, UoM e tenant.
  - Depende: Inventory P3 e OP P4.

- [ ] Validar saldo suficiente.
  - Aceite: Inventory recusa reserva quando nao ha saldo disponivel.
  - Depende: saldo `Available`.

- [ ] Criar reserva.
  - Aceite: saldo passa para `Reserved` sem duplicar quantidade.
  - Depende: validacao de saldo.

- [ ] Bloquear liberacao de OP sem reserva.
  - Aceite: OP nao muda para Released se Inventory recusar.
  - Depende: contrato `ReserveMaterial`.

### P5.2 - Execucao

- [ ] Iniciar OP.
  - Aceite: OP reservada muda para InProgress.
  - Depende: reserva confirmada.

- [ ] Consumir material reservado.
  - Aceite: consumo baixa reserva no Inventory.
  - Depende: OP InProgress.

- [ ] Registrar scrap.
  - Aceite: scrap possui motivo, quantidade e afeta ledger.
  - Depende: consumo/reserva.

- [ ] Registrar paradas.
  - Aceite: parada possui causa, inicio, fim e Work Center.
  - Depende: OP InProgress.

### P5.3 - Qualidade E Lote

- [ ] Criar inspecao de qualidade.
  - Aceite: etapa obrigatoria pode ser aprovada/reprovada.
  - Depende: OP InProgress.

- [ ] Bloquear finalizacao sem qualidade aprovada.
  - Aceite: OP nao finaliza sem inspecao exigida.
  - Depende: inspecao.

- [ ] Criar lote de produto acabado.
  - Aceite: lote referencia OP e insumos consumidos.
  - Depende: consumo e qualidade.

- [ ] Criar saldo de produto acabado no Inventory.
  - Aceite: finalizacao da OP gera saldo `Available` do produto acabado com lote e ledger.
  - Depende: lote criado.

- [ ] Finalizar OP.
  - Aceite: OP muda para Completed e publica/gera evento operacional.
  - Depende: saldo de produto acabado criado.

## P6 - Dashboard Inicial

Objetivo: exibir indicadores simples com dados reais.

### P6.1 - Read Models

- [ ] Definir estrategia de leitura do Dashboard.
  - Aceite: documento define API/read model/eventos usados.
  - Depende: dados reais de Supply, Inventory e Production.

- [ ] Criar indicador de entradas.
  - Aceite: dashboard mostra recebimentos por status.
  - Depende: Supply P3.

- [ ] Criar indicador de estoque.
  - Aceite: dashboard mostra saldos por status.
  - Depende: Inventory P3/P5.

- [ ] Criar indicador de producao.
  - Aceite: dashboard mostra OPs por status.
  - Depende: Production P5.

### P6.2 - Alertas Simples

- [ ] Criar alerta de estoque critico.
  - Aceite: material abaixo de limite aparece como alerta.
  - Depende: saldos Inventory.

- [ ] Criar alerta de divergencia pendente.
  - Aceite: recebimentos divergentes aparecem no dashboard.
  - Depende: Supply P3.

- [ ] Criar visao global de inventario para matriz.
  - Aceite: estrutura suporta consolidar tenants, mesmo com apenas `dev`.
  - Depende: Inventory consolidavel.

## P7 - Pessoas E Frota Base

Objetivo: preparar dados necessarios para expedicao e operacao.

### P7.1 - HR Base

- [ ] Criar cadastro de pessoa.
  - Aceite: pessoa possui dados basicos, tipo e status.
  - Depende: tenant `dev`.

- [ ] Diferenciar pessoa de usuario IAM.
  - Aceite: pessoa pode existir sem acesso ao sistema.
  - Depende: cadastro de pessoa.

- [ ] Criar apontamento simples de horas.
  - Aceite: pessoa registra horas por data/atividade/tenant.
  - Depende: pessoa.

### P7.2 - Fleet Base

- [ ] Criar cadastro de veiculo.
  - Aceite: veiculo possui placa, chassi/Renavam quando aplicavel, status e tenant.
  - Depende: tenant `dev`.

- [ ] Criar capacidade de carga.
  - Aceite: veiculo possui peso e volume maximo.
  - Depende: cadastro de veiculo.

- [ ] Criar vinculo motorista/veiculo.
  - Aceite: pessoa motorista pode ser vinculada a veiculo por periodo.
  - Depende: HR pessoa e Fleet veiculo.

## P8 - Expedicao Base

Objetivo: criar saida de produto acabado com conferencia e status simples.

### P8.1 - Logistics Inicial

- [ ] Criar cadastro minimo de cliente/destinatario.
  - Aceite: expedicao possui destinatario e endereco de entrega.
  - Depende: tenant `dev`.

- [ ] Criar pedido/ordem de expedicao simples.
  - Aceite: expedicao referencia produto/lote/quantidade.
  - Depende: lote produto acabado e destinatario.

- [ ] Criar picking.
  - Aceite: separacao reserva produto acabado no Inventory.
  - Depende: Inventory com produto acabado.

- [ ] Criar packing/volumes.
  - Aceite: volumes possuem itens, peso/volume e identificacao.
  - Depende: picking.

- [ ] Criar conferencia de embarque.
  - Aceite: saida bloqueia se volume divergir.
  - Depende: volumes.

- [ ] Baixar produto acabado no Inventory ao despachar.
  - Aceite: despacho confirmado baixa/reserva final do produto acabado sem recalcular saldo em Logistics.
  - Depende: conferencia de embarque.

### P8.2 - Transporte E Status

- [ ] Criar cadastro de transportadora.
  - Aceite: transportadora possui dados basicos, status, prazos e servicos.
  - Depende: Logistics API.

- [ ] Criar frete simples.
  - Aceite: calculo usa tabela basica, peso, cubagem ou distancia quando houver dados.
  - Depende: produto com dimensoes e veiculo/carga quando aplicavel.

- [ ] Criar despacho.
  - Aceite: expedicao conferida muda para despachada.
  - Depende: conferencia de embarque.

- [ ] Criar API B2B simples de status.
  - Aceite: parceiro consulta status sem acessar dados internos.
  - Depende: despacho.

### P8.3 - Fleet Operacional Minimo

- [ ] Criar manutencao simples.
  - Aceite: veiculo possui plano por data/km.
  - Depende: Fleet base.

- [ ] Criar abastecimento simples.
  - Aceite: abastecimento registra litros, valor, motorista, veiculo e rota quando houver.
  - Depende: Fleet base e HR.

## P9 - Integracoes E Recursos Avancados

Objetivo: completar capacidades externas e recursos avancados depois dos fluxos internos estarem estaveis.

### P9.1 - IAM Avancado

- [ ] Implementar gestao completa de tenants.
  - Aceite: tenant pode ser criado, editado, bloqueado/desbloqueado e preparado para novo banco sem mudar contratos.
  - Depende: Tenant Catalog e tenant `dev`.

- [ ] Implementar RBAC granular.
  - Aceite: permissoes por recurso/acao protegem endpoints e UI.
  - Depende: autorizacao minima.

- [ ] Implementar API keys.
  - Aceite: chave pode ser criada, revogada e auditada.
  - Depende: IAM e auditoria.

- [ ] Implementar MFA.
  - Aceite: usuario configurado exige segundo fator.
  - Depende: login estavel.

- [ ] Implementar recuperacao de conta.
  - Aceite: usuario consegue recuperar acesso por fluxo auditado e seguro.
  - Depende: login estavel e auditoria.

- [ ] Implementar gestao completa de sessoes.
  - Aceite: usuario/admin visualiza, revoga sessoes e aplica timeout configurado.
  - Depende: sessao BFF/IAM.

### P9.2 - Integracoes Externas

- [ ] Implementar PlugNotas/SEFAZ real.
  - Aceite: provider externo cria/atualiza recebimentos sem mudar regra Supply.
  - Depende: provider substituivel.

- [ ] Implementar webhooks de Logistics.
  - Aceite: evento de despacho/status notifica parceiro com retry.
  - Depende: Logistics P8 e Outbox quando necessario.

- [ ] Implementar tracking externo.
  - Aceite: status de entrega pode vir de transportadora/provedor.
  - Depende: despacho P8.

- [ ] Implementar integracao contabil.
  - Aceite: horas/dados de HR podem ser exportados/enviados.
  - Depende: HR horas.

### P9.3 - Recursos Avancados De Operacao

- [ ] Implementar OEE completo.
  - Aceite: disponibilidade, performance e qualidade sao calculadas por periodo/maquina.
  - Depende: OP, paradas e qualidade.

- [ ] Implementar custos.
  - Aceite: custo considera consumo, scrap e lote.
  - Depende: ledger e Production P5.

- [ ] Implementar mapas de entrega.
  - Aceite: entregas aparecem por regiao/status quando houver dados.
  - Depende: Logistics tracking.

- [ ] Implementar exportacoes PDF/Excel/CSV.
  - Aceite: relatorios principais exportam formatos definidos.
  - Depende: dashboards estaveis.

- [ ] Implementar competencias, turnos e escalas.
  - Aceite: HR suporta alocacao mais precisa de pessoas.
  - Depende: HR base.

- [ ] Implementar roteirizacao inteligente.
  - Aceite: sistema sugere rota para multiplas paradas.
  - Depende: Logistics e Fleet maduros.

- [ ] Implementar telemetria basica.
  - Aceite: ocorrencias de veiculo/motorista sao registradas ou importadas.
  - Depende: Fleet operacional.

## P10 - Endurecimento Final

Objetivo: preparar o sistema para entrega final com seguranca, performance, observabilidade, testes e documentacao.

### P10.1 - Consistencia E Mensageria

- [ ] Implementar Outbox nos fluxos criticos.
  - Aceite: recebimento aprovado, reserva, consumo, finalizacao de OP e despacho nao perdem eventos.
  - Depende: eventos reais.

- [ ] Implementar idempotencia de consumidores.
  - Aceite: evento repetido nao duplica efeito.
  - Depende: Outbox/eventos.

- [ ] Implementar retry e dead-letter.
  - Aceite: falha temporaria reprocessa; falha permanente fica rastreavel.
  - Depende: RabbitMQ/eventos.

### P10.2 - Seguranca E Auditoria

- [ ] Completar auditoria imutavel.
  - Aceite: acoes sensiveis possuem trilha append-only com usuario, tenant, IP, data e correlacao.
  - Depende: RF-05/RN-08.

- [ ] Revisar fail-closed/fail-open de auditoria.
  - Aceite: comportamento esta documentado e testado por tipo de acao.
  - Depende: auditoria completa.

- [ ] Revisar segredos e configuracoes sensiveis.
  - Aceite: nenhum segredo fica em codigo ou commit.
  - Depende: configuracao de ambientes.

- [ ] Validar TLS/criptografia conforme ambiente.
  - Aceite: ambiente de entrega usa comunicacao segura.
  - Depende: deploy.

### P10.3 - Performance E Escalabilidade

- [ ] Criar testes de performance dos endpoints principais.
  - Aceite: endpoints criticos medidos contra meta P95 abaixo de 500ms quando aplicavel.
  - Depende: fluxos completos.

- [ ] Revisar indices de banco.
  - Aceite: consultas principais possuem indices e planos aceitaveis.
  - Depende: dados reais/seed.

- [ ] Validar escalabilidade horizontal.
  - Aceite: servicos rodam sem depender de estado local/sticky session obrigatoria.
  - Depende: sessao/cache configurados.

### P10.4 - Observabilidade

- [ ] Completar tracing distribuido.
  - Aceite: uma operacao pode ser rastreada da UI ao banco/evento.
  - Depende: OpenTelemetry.

- [ ] Criar dashboards tecnicos.
  - Aceite: saude, latencia, erro e consumo de recursos sao visiveis.
  - Depende: metricas.

- [ ] Criar alertas operacionais.
  - Aceite: falhas criticas geram alerta.
  - Depende: dashboards tecnicos.

### P10.5 - Testes E Qualidade

- [ ] Criar testes unitarios de dominio.
  - Aceite: regras criticas de IAM, Supply, Inventory, Production e Logistics cobertas.
  - Depende: dominios implementados.

- [ ] Criar testes de integracao por servico.
  - Aceite: APIs principais testam banco e tenant.
  - Depende: infraestrutura local.

- [ ] Criar testes end-to-end dos fluxos principais.
  - Aceite: login, entrada, conferencia, producao e expedicao rodam ponta a ponta.
  - Depende: UI e APIs completas.

- [ ] Revisar acessibilidade/responsividade da UI.
  - Aceite: fluxos principais funcionam em desktop, tablet e mobile.
  - Depende: UI completa.

### P10.6 - Documentacao E Entrega

- [ ] Criar manual do usuario.
  - Aceite: usuario consegue operar fluxos principais sem apoio tecnico.
  - Depende: UI final.

- [ ] Criar documentacao tecnica de APIs.
  - Aceite: endpoints, payloads, erros e auth estao documentados.
  - Depende: APIs finais.

- [ ] Criar guia de deploy.
  - Aceite: ambiente pode ser provisionado seguindo o documento.
  - Depende: perfil de deploy definido.

- [ ] Criar guia de operacao.
  - Aceite: suporte sabe verificar logs, health, filas e bancos.
  - Depende: observabilidade.

- [ ] Criar checklist de entrega final.
  - Aceite: requisitos RF/NF/RN/RD estao marcados como atendidos ou justificados.
  - Depende: todos os fluxos.

## Ordem Resumida De Execucao

| Passada | Estado | Criterio para avancar |
|---|---|---|
| P0 - Base tecnica | Concluido inicial | Expandir padroes conforme novos endpoints aparecerem |
| P1 - IAM e tenant `dev` | Iniciado | Resolver tenant, OAuth Google, sessao e autorizacao minima |
| P2 - Entrada de materiais e Inventory inicial | Pendente | P1 entregue |
| P3 - Conferencia cega e saldo disponivel/bloqueado | Pendente | P2 entregue |
| P4 - Producao inicial | Pendente | Saldo real disponivel no Inventory |
| P5 - Execucao de OP usando Inventory | Pendente | P4 entregue |
| P6 - Dashboard inicial | Pendente | Dados reais de Supply, Inventory e Production |
| P7 - Pessoas e frota base | Pendente | Dashboard inicial ou necessidade de Logistics |
| P8 - Expedicao base | Pendente | Produto acabado expedivel, pessoas e frota base |
| P9 - Integracoes e recursos avancados | Pendente | Fluxos principais estaveis |
| P10 - Endurecimento final | Pendente | Escopo funcional principal implementado |

## Cobertura Por Requisito

Esta tabela nao substitui `REQUISITOS.md`; ela mostra onde cada requisito aparece no plano de tasks.

| Requisito | Cobertura no plano |
|---|---|
| RF-01 | P1.2 OAuth Google |
| RF-02 | P1.1 tenant `dev`; P9.1 gestao completa de tenants |
| RF-03 | P1.3 autorizacao minima; P9.1 RBAC granular |
| RF-04 | P1.3 sessao/logout; P9.1 gestao completa de sessoes |
| RF-05 | P2.3 auditoria basica; P10.2 auditoria imutavel |
| RF-06 | P9.1 API keys |
| RF-07 | P9.1 MFA e recuperacao de conta |
| RF-08 | P4.2 BOM e versionamento |
| RF-09 | P4.1 Work Centers |
| RF-10 | P4.3 Ordem de Producao |
| RF-11 | P5.1 reserva de material |
| RF-12 | P5.2 scrap |
| RF-13 | P5.2 paradas |
| RF-14 | P5.3 qualidade |
| RF-15 | P5.3 lote e rastreabilidade |
| RF-16 | P2.1 XML/upload/provider; P9.2 PlugNotas/SEFAZ real |
| RF-17 | P3.1 conferencia cega |
| RF-18 | P3.2 divergencia e devolucao |
| RF-19 | P8.1 picking/packing |
| RF-20 | P8.2 transportadoras, prazos e servicos |
| RF-21 | P8.2 despacho/status; P9.2 tracking externo |
| RF-22 | P9.2 webhooks de Logistics |
| RF-23 | P8.1 conferencia de embarque |
| RF-24 | P8.2 frete simples; P9.3 evolucao logistica |
| RF-25 | P7.2 cadastro de veiculo |
| RF-26 | P8.3 manutencao simples |
| RF-27 | P8.3 abastecimento simples |
| RF-28 | P7.2 vinculo motorista/veiculo |
| RF-29 | P9.3 roteirizacao inteligente |
| RF-30 | P9.3 telemetria basica |
| RF-31 | P7.1 cadastro de pessoa |
| RF-32 | P9.3 competencias |
| RF-33 | Nao implementar; buraco preservado no PDF |
| RF-34 | P6.1 indicadores; P9.3 OEE completo |
| RF-35 | P9.3 mapas de entrega |
| RF-36 | P6.2 alertas simples; P10.4 alertas operacionais |
| RF-37 | P9.3 exportacoes PDF/Excel/CSV |
| RF-38 | P9.3 custos |
| RD-IAM-01 | P1.1 tenant resolver |
| RD-INV-01 | P2.2 Inventory inicial; P3.2 saldo disponivel/bloqueado |
| RD-INV-02 | P2.2 ledger minimo; P3.3/P5.2 ledger operacional |
| RD-INV-03 | P6.2 visao global de inventario |
| RD-SUP-01 | P2.1 entrada manual/upload |
| RD-SUP-02 | P2.1 provider substituivel; P9.2 provider real |
| RD-LOG-01 | P8.2 API B2B simples de status |
| RD-PRD-01 | P4.1 dimensoes de produto |
| RD-FLE-01 | P7.2 capacidade de carga |
| RD-HR-01 | P7.1 apontamento de horas |
| RD-HR-02 | P9.2 integracao contabil |
| RD-HR-03 | P9.3 turnos e escalas |
| RD-TEC-01 | P3.3 eventos; P10.1 Outbox/retry/dead-letter |
| RD-EDGE-01 | P0.4 Gateway/BFF; P1.3 BFF/cookie/CSRF |
| RD-AUD-01 | P2.3 auditoria basica; P10.2 politica final |
| RD-UI-01 | P1.4 UI inicial; P10.5 responsividade/acessibilidade |
| RD-DOC-01 | P10.6 documentacao e entrega |
| NF-01 | P0.3 resiliencia HTTP; P10.1 retry/dead-letter |
| NF-02 | P10.1 Outbox e idempotencia |
| NF-03 | P0.3 logs, health checks e OpenTelemetry; P10.4 observabilidade |
| NF-04 | P10.3 performance P95 |
| NF-05 | P1.2 segredos OAuth; P10.2 seguranca/TLS |
| NF-06 | P10.3 escalabilidade horizontal |
| NF-07 | P1.1 locale/timezone; P9.1 gestao completa de tenants |
| RN-01 | P1.1 tenant obrigatorio |
| RN-02 | P1.3 autorizacao minima; P9.1 RBAC granular |
| RN-03 | P5.1 reserva ao liberar OP |
| RN-04 | P5.3 bloqueio por qualidade |
| RN-05 | P3.1 conferencia cega |
| RN-06 | P3.2 divergencia e bloqueio |
| RN-07 | P8.1 conferencia de embarque |
| RN-08 | P2.3 auditoria basica; P10.2 auditoria imutavel |

## Proximos Documentos Necessarios

- [x] `REGRAS_PARA_IAS.md`: regras para agentes, SOLID, arquitetura hexagonal e atualizacao de contexto.
- [ ] `MODELO_DOMINIO.md`: entidades, estados e regras por dominio.
- [x] `CONTRATOS_API.md`: convencoes HTTP iniciais, Tenancy, headers e erros.
- [ ] `MODELO_DADOS.md`: tabelas, indices, migrations e seeds.
- [ ] `EVENTOS_E_OUTBOX.md`: eventos, payloads, idempotencia e retry.
- [ ] `SEGURANCA_E_IAM.md`: OAuth, sessao, RBAC, API keys, MFA e auditoria.
- [ ] `CRITERIOS_ACEITE.md`: aceite formal por requisito RF/NF/RN/RD.
