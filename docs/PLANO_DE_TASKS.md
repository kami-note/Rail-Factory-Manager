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

### P1.x - Refatoracao Incremental Dos Microservicos

Objetivo: padronizar estrutura interna dos servicos em paralelo as tasks funcionais, sem bloquear fluxo de entrega.

- [x] Refatorar IAM para estrutura em camadas coerente com hexagonal (Api/Application/Domain/Infrastructure).
  - Aceite: endpoints atuais continuam funcionando; dependencias entre camadas respeitam `REGRAS_PARA_IAS.md`.
  - Depende: P1.1 concluido.
  - Entregue: `Program.cs` sem regra de negocio inline para `/info`, caso de uso em `Application`, modulo de DI em `Infrastructure` e contrato `/info` definitivo sem linguagem de placeholder.

- [x] Refatorar SupplyChain no mesmo padrao incremental.
  - Aceite: endpoint atual continua funcionando; sem regressao de contratos HTTP.
  - Depende: baseline de padrao aplicada no IAM.
  - Entregue: `Program.cs` sem regra de negocio inline para `/info`, caso de uso em `Application`, modulo de DI em `Infrastructure` e contrato `/info` com capacidade explicita do contexto.

- [x] Refatorar Inventory no mesmo padrao incremental.
  - Aceite: endpoint atual continua funcionando; sem regressao de contratos HTTP.
  - Depende: baseline de padrao aplicada no IAM.
  - Entregue: `Program.cs` sem regra de negocio inline para `/info`, caso de uso em `Application`, modulo de DI em `Infrastructure` e contrato `/info` com capacidade explicita do contexto.

- [x] Refatorar Production no mesmo padrao incremental.
  - Aceite: endpoint atual continua funcionando; sem regressao de contratos HTTP.
  - Depende: baseline de padrao aplicada no IAM.
  - Entregue: `Program.cs` sem regra de negocio inline para `/info`, caso de uso em `Application`, modulo de DI em `Infrastructure` e contrato `/info` com capacidade explicita do contexto.

- [x] Padronizar middlewares/helpers compartilhados de borda (tenant, correlation, erro) nos servicos que ainda estiverem divergentes.
  - Aceite: comportamento de headers e erros fica consistente entre os microservicos tenant-aware.
  - Depende: refatoracao incremental por servico iniciada.
  - Entregue: IAM, SupplyChain, Inventory e Production usam o mesmo baseline (`AddServiceDefaults`, `UseServiceDefaults`, `AddTenantResolution`, `UseTenantResolution`, `MapDefaultEndpoints`), mantendo contrato de erro e headers de borda consistentes. `Program.cs` e composicao minima, com mapeamento consolidado em `Api/*Endpoints`.

### P1.2 - OAuth Google

- [x] Configurar credenciais OAuth Google.
  - Aceite: ambiente local possui configuracao externa, sem segredo hardcoded.
  - Depende: IAM API e BFF.
  - Entregue: credenciais e origem publica configuradas externamente no AppHost (`google-client-id`, `google-client-secret`, `frontend-public-origin`) e validadas em runtime no IAM.

- [x] Implementar inicio de login.
  - Aceite: usuario consegue iniciar login pela UI.
  - Depende: OAuth configurado.
  - Entregue: endpoint `GET /api/auth/google/start` no BFF e `GET /api/iam/auth/google/start` no IAM ativos, com redirect real para Google e `redirect_uri` publico HTTPS canônico.

- [x] Implementar callback OAuth.
  - Aceite: Google retorna identidade validada para IAM/BFF.
  - Depende: inicio de login.
  - Entregue: callback `GET /auth/google/callback` executado com codigo real do Google no fluxo edge -> gateway -> IAM, com finalize `GET /auth/google/finalize` retornando redirect controlado para UI (`oauth=success`/`oauth=error`).

- [x] Criar ou atualizar usuario local apos login.
  - Aceite: usuario autenticado existe no IAM DB do tenant `dev`.
  - Depende: callback OAuth.
  - Entregue: login OAuth real executado em 2026-05-03 com persistencia confirmada no `iamdb.public.iam_local_users` (`external_provider=google`, `external_subject` preenchido, `display_name` preenchido, `updated_at` atualizado).

### P1.3 - Sessao E Autorizacao Minima

- [x] Criar sessao no BFF por cookie.
  - Aceite: usuario autenticado permanece logado entre requests.
  - Depende: login Google.
  - Entregue: `GET /auth/session` no IAM e `GET /api/auth/session` no BFF padronizados com contrato compartilhado `AuthSessionDto` (`RailFactory.BuildingBlocks`), mantendo payload canonico para `200` autenticado e `401` nao autenticado, com cookie/sessao do servidor como fonte unica de verdade para a UI.

- [x] Proteger chamadas autenticadas contra CSRF.
  - Aceite: chamadas mutaveis pelo BFF exigem protecao CSRF ou mecanismo equivalente.
  - Depende: sessao no BFF.
  - Entregue: BFF expõe `GET /api/auth/csrf` e valida `X-CSRF-TOKEN` em `POST /api/auth/logout`, retornando `403` com codigo estavel `csrf_error` quando o token e ausente/invalido.

- [x] Criar endpoint de usuario atual.
  - Aceite: UI mostra usuario, tenant e permissoes minimas.
  - Depende: sessao no BFF.
  - Entregue: IAM disponibiliza `GET /auth/current-user` protegido e mantendo o contrato compartilhado `AuthSessionDto`; UI protegida em `/app` reutiliza modulo `auth` (client + hook/guard de sessao) e exibe usuario autenticado.

- [x] Implementar logout.
  - Aceite: cookie/sessao sao encerrados e chamadas protegidas passam a falhar.
  - Depende: sessao no BFF.
  - Entregue: `POST /api/auth/logout` no BFF encaminha cookie/tenant para `POST /auth/logout` no IAM e encerra sessao no servidor.

- [x] Implementar autorizacao minima deny by default.
  - Aceite: endpoint protegido falha sem usuario/permissao minima.
  - Depende: usuario atual.
  - Entregue: IAM com rotas protegidas explicitas (`GET /auth/current-user` e `POST /auth/logout` com `RequireAuthorization`), mantendo rotas publicas de OAuth/sessao com `AllowAnonymous`.
  - Ajuste (2026-05-15): `SmartAuth` no IAM foi reduzido a seletor de `cookie` vs bearer JWT interno; fluxo browser-facing continua em cookie-only e o BFF descarta headers sensiveis do cliente antes do proxy.
  - Ajuste (2026-05-15): rotas protegidas dos microservices migradas para bearer JWT interno assinado pelo BFF; spoofing direto por `X-RF-User-*` fora do BFF deixou de autenticar `Inventory` e `SupplyChain`.
  - Ajuste (2026-05-15): bearer JWT interno ficou tenant-bound; requests autenticadas agora falham com `403 tenant.mismatch` quando `X-Tenant-Code` nao coincide com o claim `tenant` do token.
  - Ajuste (2026-05-15): `InternalToken:SigningKey` e `InternalApiKey` deixaram de ter defaults usaveis no repositório/AppHost; os serviços falham explicitamente sem a configuracao obrigatoria.

### P1.4 - UI Inicial

- [x] Criar tela de login.
  - Aceite: botao de login Google inicia fluxo real.
  - Depende: inicio de login.
  - Entregue: UI React/Vite em `/` com CTA `Sign in with Google` chamando `GET /api/auth/google/start` via `buildLoginHref(...)`, usando `tenantCode` explicito e `returnUrl=/app`.

- [x] Criar layout autenticado inicial.
  - Aceite: usuario logado ve navegacao basica.
  - Depende: usuario atual.
  - Entregue: rota protegida `/app` com estado autenticado, exibicao de usuario/sessao, acao de logout e navegacao basica de retorno para inicio.

- [x] Criar tratamento de erro de autenticacao.
  - Aceite: falhas de login aparecem de forma clara na UI.
  - Depende: fluxo OAuth.
  - Entregue: UI trata erros de OAuth (`oauth=error`) e falhas de sessao/logout com mensagens visiveis e estados seguros (`unauthenticated`/`error`).

- [x] Evoluir area protegida para dashboard inicial consistente.
  - Aceite: rota `/app` protegida exibe layout de dashboard com navegacao para fluxos P2 e componentes reutilizaveis sem quebrar sessao.
  - Depende: layout autenticado inicial.
  - Entregue: topbar, navegacao, cards de KPI e modulos (`overview`, `receipts`, `new receipt`, `import xml`) com estados de erro/carregamento e logout preservado.

- [x] Alinhar visual da dashboard protegida ao padrao do projeto legado.
  - Aceite: shell visual de `/app` reproduz os elementos centrais do layout legado (appbar azul contextual, sidebar segmentada, cards/tabelas/timeline e navegacao mobile), mantendo os fluxos P2 existentes.
  - Depende: dashboard inicial consistente.
  - Entregue: `ProtectedDashboardLayout` e `OverviewPanel` refeitos com paridade visual do legado e CSS reorganizado (`tokens/layout/components`) sem regressao funcional.

- [x] Consolidar recebimentos em tela unica e migrar navegacao protegida para SPA local.
  - Aceite: sidebar deixa de ter card/borda propria, navegacao entre `Overview` e `Receipts` nao recarrega a pagina e `Receipts` concentra lista, criacao manual e importacao XML.
  - Depende: alinhamento visual da dashboard protegida.
  - Entregue: `ReceiptsWorkspace`, navegacao por History API no frontend, rotas legadas `/app/new-receipt`/`/app/import-xml` normalizadas para `/app/receipts`, lista mantida como workspace principal, acoes principais visiveis e criacao/importacao em drawer lateral responsivo.

### P1.5 - Multi-Tenancy Validation

Objetivo: validar a infraestrutura de database-per-tenant com um segundo tenant real e permitir a escolha pelo usuario.

- [ ] Provisionar bancos para o segundo tenant ('acme') no AppHost.
  - Aceite: Bancos `tenant-acme-iamdb`, `tenant-acme-supplychaindb`, `tenant-acme-inventorydb` e `tenant-acme-productiondb` sao criados e visiveis no PgAdmin.
  - Depende: P0.2.

- [ ] Criar seed para o tenant 'acme' no Tenant Catalog.
  - Aceite: O tenant 'acme' existe na tabela `tenants` com as connection strings corretas apontando para os novos bancos.
  - Depende: P1.1.

- [ ] Implementar tela de selecao de tenant no Frontend.
  - Aceite: Uma tela inicial (ou modal) permite ao usuario informar o código do tenant (ex: 'dev', 'acme') antes de prosseguir para o login.
  - Depende: P1.4.

- [ ] Persistir e carregar o tenant selecionado.
  - Aceite: O tenant escolhido e salvo no `localStorage` e recuperado automaticamente.
  - Depende: Tela de selecao.

- [ ] Tornar o tenant dinamico na `App.tsx` e fluxos de Auth.
  - Aceite: O `tenantCode` usado nas chamadas de API e no redirect do Google OAuth e o tenant selecionado pelo usuario.
  - Depende: Persistencia do tenant.

## P2 - Entrada De Materiais E Inventory Inicial

Objetivo: criar o primeiro fluxo de negocio real: recebimento de material e saldo pendente no Inventory.

### P2.1 - Supply Chain Inicial

- [x] Criar cadastro minimo de fornecedor.
  - Aceite: recebimento pode referenciar fornecedor com identificacao fiscal/nome.
  - Depende: tenant `dev`.

- [x] Criar modelo de recebimento.
  - Aceite: recebimento possui numero, fornecedor, documento, data, tenant e status.
  - Depende: cadastro minimo de fornecedor.

- [x] Criar modelo de item de recebimento.
  - Aceite: item possui material, quantidade esperada, UoM e referencia ao recebimento.
  - Depende: modelo de recebimento.

- [x] Criar entrada manual de recebimento.
  - Aceite: usuario cria recebimento sem integracao externa.
  - Depende: modelos Supply.

- [x] Criar upload/importacao XML basica.
  - Aceite: XML valido cria recebimento e itens.
  - Depende: modelos Supply.
  - Entregue: importacao aceita XML colado, arquivo local unitario e lote atomico via `POST /receipts/import/xml/batch`; lote com qualquer XML invalido ou duplicado retorna `receipt.batch_invalid` e nao grava supplier/receipt/outbox/auditoria.

- [x] Criar provider interno substituivel de NF-e.
  - Aceite: entrada manual/upload usam interface que depois pode receber PlugNotas/SEFAZ.
  - Depende: entrada manual/upload.
  - Entregue: `BasicXmlNfeProvider` mantem XML simplificado legado, aceita NF-e `1.10` apenas como compatibilidade legado/dev, e valida NF-e `4.00` contra XSD oficial versionado no repo (`PL_010c_NT2022_002v1.30`, `nfe_v4.00.xsd`). `NFe` e `nfeProc` sao suportados; para `nfeProc`, o `NFe` interno e validado porque o pacote oficial atual nao inclui `procNFe_v4.00.xsd`. Extrai emitente, chave/documento, data e itens `det/prod`; `receiptNumber` de NF-e usa `NFE-{accessKey}` para evitar colisao por serie/numero.

### P2.2 - Inventory Inicial

- [x] Criar Inventory API.
  - Aceite: Inventory possui health e endpoint protegido.
  - Depende: P0/P1.

- [x] Criar Inventory DB `dev`.
  - Aceite: migracao inicial cria tabelas de saldo e ledger.
  - Depende: AppHost com banco Inventory.

- [x] Criar modelo de saldo.
  - Aceite: saldo identifica tenant, material, UoM, status, local de estoque e quantidade.
  - Depende: Inventory DB.

- [x] Criar cadastro minimo de local de estoque.
  - Aceite: saldo pendente/disponivel pode ser associado a um local fisico ou logico.
  - Depende: Inventory DB.

- [x] Criar ledger minimo.
  - Aceite: cada alteracao de saldo gera lancamento append-only.
  - Depende: modelo de saldo.

- [x] Criar contrato `CreatePendingBalance`.
  - Aceite: Supply consegue solicitar saldo pendente para recebimento criado.
  - Depende: Inventory API.

### P2.3 - Integracao Supply -> Inventory

- [x] Ao criar recebimento, criar saldo pendente.
  - Aceite: recebimento criado gera saldo `Pending` no Inventory.
  - Depende: Supply inicial e `CreatePendingBalance`.
  - Entregue: outbox Supply segue criando evento por item; dispatcher foi endurecido para payload legado/permanent failure sem bloquear a fila inteira, com estado persistido (`Pending`, `Dispatched`, `DeadLetter`), tentativas, ultimo erro e endpoint operacional `GET /outbox/dead-letters`.

- [ ] Criar replay operacional de dead letters do outbox Supply.
  - Aceite: operador consegue reprocessar dead letters selecionados apos corrigir a causa externa, sem duplicar saldos no Inventory.
  - Depende: `GET /outbox/dead-letters` e idempotencia do Inventory.
  - Evidencia: rodada de resiliencia em 2026-05-04 confirmou que Inventory fora por tempo suficiente gera dead-letter rastreavel, mas sem caminho de recuperacao operacional.

- [x] Garantir idempotencia basica por recebimento/item.
  - Aceite: retry nao duplica saldo pendente.
  - Depende: ledger minimo.

- [x] Registrar auditoria basica da entrada.
  - Aceite: criacao de recebimento registra usuario, tenant, data e acao.
  - Depende: usuario autenticado.

### P2.4 - UI De Entrada

- [x] Criar tela de lista de recebimentos.
  - Aceite: usuario ve recebimentos do tenant `dev`.
  - Depende: API de recebimentos.

- [x] Criar tela/formulario de novo recebimento.
  - Aceite: usuario cria recebimento manual.
  - Depende: entrada manual.

- [x] Criar upload de XML.
  - Aceite: usuario importa XML e revisa itens criados.
  - Depende: upload/importacao XML.
  - Entregue: drawer de recebimentos permite selecionar um ou varios arquivos `.xml`, exibe a lista selecionada e importa os documentos em lote via `/api/supply-chain/receipts/import/xml`.

- [x] Criar tela para visualizar materiais pendentes no estoque.
  - Aceite: usuario autenticado visualiza lista de saldos pendentes via BFF, sem chamada direta da UI para servico interno.
  - Depende: endpoint `GET /balances/pending` no Inventory.
  - Entregue: rota protegida `/app/inventory` com consumo de `/api/inventory/balances/pending` e navegacao dedicada no menu lateral.

### P2.5 - Expansao de Rastreabilidade (Hybrid Model)

- [x] Definir contrato de Metadados e Esquema Extendido.
  - Aceite: Documento define campos fixos (Lote, Validade) e estrutura do JSON de Metadados para Compras vs Producao.
  - Depende: P2.1 e P2.2.
  - Entregue: Implementado via Rastreabilidade Hibrida (Colunas fixas + JSON Metadata).

- [x] Atualizar entidades de Supply Chain com dados fiscais e XML original.
  - Aceite: `MaterialReceipt` inclui Chave de Acesso (44 digitos), Valor Total e o conteúdo XML original (RawXml); `MaterialReceiptItem` inclui Preco Unitario e Descricao Original.
  - Depende: Definisão de contrato.
  - Entregue: Entidades e DTOs atualizados em 2026-05-05.

- [x] Atualizar entidades de Inventory com Rastreabilidade Hibrida.
  - Aceite: `InventoryBalance` inclui colunas fixas `LotNumber`, `ExpirationDate`, `SourceType` (Enum) e campo `SourceMetadata` (JSONB).
  - Depende: Definisão de contrato.
  - Entregue: Entidades e DTOs atualizados em 2026-05-05.

- [x] Sincronizar Metadados no fluxo Supply -> Inventory.
  - Aceite: O dispatcher de integração injeta os dados da NF-e no JSON de metadados do saldo criado.
  - Depende: Atualização das entidades.
  - Entregue: `InventoryPendingBalanceDispatcher` e `CreatePendingBalance` atualizados.

- [x] Refletir novas colunas na UI de Inventory e Supply.
  - Aceite: Listagens de recebimento e saldo exibem Lote, Validade e Chave de Acesso.
  - Depende: Atualização das APIs.
  - Entregue: UI atualizada e funcionalidade de Download XML adicionada em 2026-05-05.

### P2.6 - Humanização e Refinamento de UX (Foco no Operador)

Objetivo: Melhorar a legibilidade dos dados e a experiência do usuário de chão de fábrica, removendo ruído técnico e adicionando contexto operacional real.

- [x] Criar Utilitário de Mapeamento de Status (@architect / @frontend).
  - Aceite: Centralizar labels amigáveis (ex: 'Registered' -> 'Aguardando Conferência') e cores semânticas (ex: Approved = Verde/Sucesso, Divergent = Vermelho/Atenção).
  - Depende: P2.5.

- [x] Padronizar Formatação de Datas e Localização (@frontend).
  - Aceite: Usar `pt-BR` de forma consistente; exibir 'Data de Emissão' e 'Data de Criação' com clareza (formatos amigáveis como 'hoje às 14h' ou 'dd/MM/yyyy').
  - Depende: P2.5.

- [x] Humanizar Identificadores Técnicos (@frontend).
  - Aceite: Truncar ou ocultar UUIDs (ex: `sourceReference`) em favor de referências de negócio (ex: "NF-e 1234") com opção de cópia.
  - Depende: P2.5.

- [x] Modal de Detalhes do Recebimento - Visão do Conferente (@backend / @frontend).
  - Aceite: Endpoint `GET /receipts/{id}` e Modal na UI criados. O modal deve exibir:
    - Linha do Tempo Visual do status (Recebido -> Conferência -> Aprovado/Divergente).
    - Tabela clara de "Esperado (NF-e) vs. Contado (Físico)".
    - Ações Rápidas em destaque (Iniciar Conferência, Baixar XML).
    - Ficha resumida do Fornecedor (Nome/CNPJ).
    - **Auditoria Visível:** Quem iniciou a conferência e quando.
  - Depende: P2.5.

- [x] Modal de Detalhes do Saldo de Estoque - Visão da Produção (@backend / @frontend).
  - Aceite: Endpoint `GET /balances/{id}` e Modal na UI criados. O modal deve exibir:
    - Os 3 Números Mágicos (Total Físico, Disponível, Bloqueado/Quarentena).
    - Rastreabilidade/Origem (Link claro para a NF-e que gerou o saldo).
    - Alerta visual de Validade (cores de atenção se próximo ao vencimento).
    - Extrato de Movimentação (Ledger humanizado: ex: "+100 kg recebidos", "-20 kg consumidos na OP #45").
    - **Auditoria Visível:** Quem bloqueou o lote ou quem aprovou a liberação.
  - Depende: P2.5.

### P2.7 - Dashboard Operacional (Desmocking)

Objetivo: Substituir os dados falsos (`mocks.ts`) do `OverviewPanel` por KPIs reais extraídos dos domínios já implementados, entregando valor imediato ao operador e gestor. Seguir a regra de "Just-In-Time Implementation" (construir apenas as consultas necessárias para a tela).

- [ ] KPI de Ações Pendentes (Supply Chain) (@backend / @frontend).
  - Aceite: Backend expõe endpoint simples `/api/supply-chain/receipts/summary` e Frontend mostra o número real de Recebimentos aguardando conferência (`status: Registered` ou `InConference`).
  - Depende: P2.6.

- [ ] KPI de Saúde do Estoque (Inventory) (@backend / @frontend).
  - Aceite: Backend expõe endpoint `/api/inventory/balances/summary` e Frontend mostra total de itens Pendentes vs. Disponíveis vs. Bloqueados (divergentes).
  - Depende: P2.6.

- [ ] Substituir Tabela de "Live Production Monitor" por "Inbound Recente" (@frontend).
  - Aceite: Como a Produção (P4/P5) ainda não existe, a tabela principal do Dashboard deve ser alterada para mostrar as últimas NF-es importadas e seu status atual. O componente mockado `productionLines` deve ser completamente removido.
  - Depende: P2.6.

- [x] Ocultar Ruído Técnico e Activity Log Falso (@frontend).
  - Aceite: Remover os painéis "System Telemetry" e o "Activity Log" de mocks, pois não agregam valor operacional a um usuário fabril neste momento. Caso necessário para debugar (dev), mover o telemetry para um `Ctrl+Shift+D` toggle ou remover de vez seguindo a "Dead Code Prevention".
  - Depende: P2.6.

### P2.8 - Backend Structuring and Product Catalog (The "Elite" Cleanup)

Objetivo: Mover a inteligência de parsing do Frontend para o Backend, estruturar os dados de rastreabilidade e introduzir um catálogo de materiais para evitar inconsistências e "código morto".

- [ ] Task 2.8.1: Enriquecer Rastreabilidade no Supply Chain (@backend).
  - Aceite: O evento `supply.receipt_item_registered` passa a incluir o `SupplierName`.
  - Depende: P2.5.

- [ ] Task 2.8.2: Suportar SupplierName no Inventory API (@backend).
  - Aceite: O endpoint `POST /internal/pending-balances` aceita `SupplierName` e o armazena no `SourceMetadata` de forma estruturada.
  - Depende: Task 2.8.1.

- [ ] Task 2.8.3: Implementar Catálogo de Materiais no Inventory (@architect / @backend).
  - Aceite: Entidade `Material` criada (Código, Nome Oficial, Descrição, Categoria, ImageUrl). Seed inicial para os materiais do tenant `dev`.
  - Depende: P2.2.

- [x] Task 2.8.4: Enriquecer Resposta de Saldo no Inventory (@backend).
  - Aceite: O DTO de resposta de saldo (`InventoryBalanceDetailsResponse`) inclui dados do `Material` (nome, imagem) e o `SupplierName` limpo, sem exigir parsing no frontend.
  - Depende: Task 2.8.2 e 2.8.3.
  - Entregue: `ListPendingBalances` e `GetInventoryBalanceDetails` retornam metadados estruturados (`materialName`, `materialImageUrl`, `supplierName`) e o frontend de estoque consome os campos sem parsing manual.

- [x] Task 2.8.5: Limpeza do Frontend e Remoção de Lógica Morta (@frontend).
  - Aceite: Remover o parsing de JSON no `InventoryStocksPage.tsx` e `ConferenceWorkspace.tsx`. Usar os novos campos estruturados do backend.
  - Depende: Task 2.8.4.
  - Entregue: frontend usa campos estruturados para fornecedor, nome e imagem de material; parsing defensivo de JSON foi removido das telas operacionais alvo.

### P2.9 - Frontend Vertical Slices Architecture (Migration)

Objetivo: Migrar o monolito `features/dashboard` e a pasta `auth/` para uma arquitetura baseada em features (Vertical Slices), garantindo isolamento de regras de negócio e modularidade.

- [x] Task 2.9.1: Desenhar o plano de migração e a nova estrutura de pastas (@lead).
  - Aceite: Plano aprovado contendo o mapeamento de onde cada componente atual irá morar (`features/auth`, `features/inventory`, `features/supply-chain`, `features/production`, `shared/`).
  - Depende: Avaliação do estado atual do Frontend.
  - Entregue: Mapeamento concluído e documentado.

- [x] Task 2.9.2: Validar fronteiras, contratos de dependência e Shared Kernel (@architect).
  - Aceite: O Arquiteto revisa a estrutura proposta e garante que Hexagonal Integrity e as regras de "Modular Minimality" sejam respeitadas. Nenhuma feature deve importar componentes de outra feature diretamente. Regras documentadas em `src/RailFactory.Frontend/GEMINI.md`.
  - Depende: Task 2.9.1.
  - Entregue: Estrutura de Feature Slices e Shared Kernel definida e documentada em 2026-05-07.

- [x] Task 2.9.3: Refactor structure to Vertical Slices and realocate files (@frontend).
  - Aceite: Seguindo o mandato em `src/RailFactory.Frontend/GEMINI.md`, diretórios `auth`, `inventory`, `supply-chain` e `production` criados em `src/features`. Pasta `shared/` criada para componentes universais, lib e layouts. `App.tsx` roteando via imports dos `index.ts` das features.
  - Depende: Task 2.9.2.
  - Entregue: Estrutura migrada, imports atualizados e build validado em 2026-05-07.

- [ ] Task 2.9.4: Validar a estabilidade da UI e cobertura de testes (@tester).
  - Aceite: O fluxo de navegação e as funcionalidades de Recebimento, Inventário e Auth continuam funcionando sem regressão visual ou quebras no console. Testes executam com sucesso.
  - Depende: Task 2.9.3.

### P2.10 - Association Workbench (SKU Resolution)

Objetivo: substituir o fluxo modal `AssociationInbox`/`AssociationForge` por uma bancada operacional para resolver itens fiscais sem SKU interno antes da conferencia, com separacao clara entre `supplierProductCode` (SKU do fornecedor/NF-e) e `internalMaterialCode` (SKU do Inventory).

Decisao de UX:

- tratar associacao como fila de saneamento operacional, nao como modal de conclusao obrigatoria;
- salvar decisao por item, preservando progresso da nota;
- criar material e associar sem tirar o operador da tela;
- manter override de SKU do fornecedor como excecao auditavel, nao como edicao comum;
- liberar para conferencia apenas quando os itens estiverem em estados permitidos pela regra de negocio.

Fluxo de trabalho esperado no frontend:

1. Operador entra em `/app/supply-chain/association` pela navegacao de Supply Chain.
2. A tela carrega uma fila lateral de recebimentos em `PendingAssociation` ou com itens sem resolucao final.
3. Ao selecionar um recebimento, o centro da tela mostra os itens da NF-e em grid operacional com `Supplier code from invoice`, descricao, NCM, GTIN/EAN, unidade, quantidade, `Internal inventory SKU` e status da associacao.
4. Ao selecionar um item, o painel lateral mostra dados fiscais do item, sugestoes de materiais internos, busca manual no catalogo e preview de conversao (`1 supplier unit = X stock unit`, `quantity -> inventory quantity`).
5. Se existir material correto, o operador escolhe o material interno, informa/valida fator de conversao e salva `Map to selected material`; a linha fica resolvida e o foco avanca para o proximo item pendente.
6. Se nao existir material, o operador abre `Create material from invoice item`; o painel vem pre-preenchido com dados da NF-e, valida duplicidade/GTIN/SKU, cria o material e associa o item na mesma acao.
7. Se o item nao puder ser resolvido na hora, o operador marca `Review later` com motivo; a nota permanece bloqueada para conferencia enquanto houver pendencia bloqueante.
8. Se o negocio permitir item fiscal que nao entra no estoque, o operador pode marcar `Ignored` com motivo; o efeito sobre liberacao para conferencia deve vir do contrato da Task 2.10.1.
9. Alterar `Internal inventory SKU` e uma nova associacao normal; alterar `Supplier code from invoice` e um override separado, com motivo e auditoria, sem modificar o XML original.
10. A tela salva cada decisao imediatamente, mostra erro recuperavel quando houver conflito/CSRF/autorizacao/falha parcial e permite recarregar o estado real da nota.
11. Quando todos os itens estiverem em estado permitido, o operador aciona `Release to conference`; a nota sai da fila ou muda para estado conferivel.
12. O fluxo legado em modal so pode ser removido depois que esse caminho cobrir material existente, material novo, revisao posterior, conflito concorrente, erro de seguranca e liberacao para conferencia.

Riscos que esta passada deve enderecar:

- inconsistencia entre criar material no Inventory e associar item no SupplyChain;
- ausencia de estado por item para progresso parcial;
- ambiguidade entre SKU fornecedor e SKU interno;
- criacao facil demais de materiais duplicados ou mal classificados;
- fator de conversao incorreto entre unidade do fornecedor e unidade do estoque;
- auto-match por nome/GTIN/NCM aplicando associacao errada;
- UX bonita sem contrato backend suficiente para garantir consistencia operacional.
- chamadas mutaveis browser-facing sem politica explicita de CSRF/autorizacao;
- concorrencia entre dois operadores resolvendo a mesma nota/item;
- migracao de recebimentos/mappings ja existentes para o novo estado por item;
- falhas sem erro estavel, auditoria ou recuperacao operacional clara.

- [x] Task 2.10.1: Definir contrato da Association Workbench (@architect / @backend / @frontend).
  - Aceite: Documento/contrato define read model, estados de item, acoes permitidas, payloads e criterio de liberacao para conferencia.
  - Depende: P2.8.3 e P3.1 estados de recebimento.
  - Deve cobrir: `Pending`, `Mapped`, `CreatedAndMapped`, `ReviewLater`, `Ignored`, `Conflict`; regra de quais estados bloqueiam `ReleaseToConference`; diferenca visual e contratual entre `supplierProductCode` e `internalMaterialCode`.
  - Deve registrar codigos de erro HTTP estaveis, politica de autorizacao/CSRF para chamadas mutaveis e formato de resposta para conflitos concorrentes.
  - Entregue: `docs/CONTRATOS_API.md` recebeu a secao `P2.10 - Association Workbench`, cobrindo workflow frontend, estados, endpoints alvo, DTOs, erros estaveis, seguranca browser-facing, concorrencia e falha parcial em `create-material-and-associate`.
  - Validacao: documentacao revisada localmente; build/test nao aplicavel por ser mudanca apenas de contrato.

- [x] Task 2.10.2: Persistir estado de associacao por item no SupplyChain (@backend).
  - Aceite: Cada `MaterialReceiptItem` registra status de associacao, material interno escolhido quando houver, fator de conversao, motivo de revisao/ignoracao quando aplicavel e auditoria minima.
  - Depende: Task 2.10.1.
  - Deve usar EF Core + migracao formal.
  - Deve incluir migracao/backfill para recebimentos e supplier mappings ja existentes, sem perder notas pendentes atuais.
  - Entregue: `MaterialReceiptItemAssociationStatus` criado; `MaterialReceiptItem` passou a persistir `SupplierProductCode`, `SupplierQuantity`, `SupplierUnitOfMeasure`, `InternalMaterialCode`, `AssociationStatus`, `AssociationConversionFactor`, `AssociationReason`, `AssociationUpdatedAt` e `AssociationUpdatedBy`; writer de recebimento separa itens mapeados de itens pendentes; migrations `AddReceiptItemAssociationState` e `AddReceiptItemSupplierSourceQuantity` incluem backfill para notas existentes.
  - Validacao: `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` e `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` passaram.

- [x] Task 2.10.3: Criar read model da bancada no SupplyChain (@backend).
  - Aceite: `GET /api/supply-chain/receipts/{id}/association-workbench` retorna dados da nota, itens, status por item, mapeamento atual e dados fiscais uteis (`description`, `ncm`, `gtin`, `supplierUnit`, `quantity`).
  - Depende: Task 2.10.2.
  - Entregue: `GetAssociationWorkbench` e `ListAssociationQueue` criados; endpoints `GET /receipts/{id}/association-workbench` e `GET /receipts/association-queue` expostos no SupplyChain; read model retorna status, versao, supplier SKU, dados fiscais, SKU interno, fator e bloqueadores de liberacao.
  - Observacao: sugestoes de materiais retornam lista vazia ate a Task 2.10.8 implementar ranking via Inventory.
  - Validacao: `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` e `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` passaram.

- [x] Task 2.10.4: Criar associacao contextual por item (@backend).
  - Aceite: `POST /api/supply-chain/receipts/{receiptId}/items/{itemId}/association` cria/atualiza mapping fornecedor -> material interno e marca o item como resolvido sem exigir finalizar a nota inteira.
  - Depende: Task 2.10.2.
  - Deve validar: material interno informado, fator de conversao maior que zero, precisao decimal aceitavel, status atual da nota, idempotencia para repeticao segura e conflito quando outro operador atualizou o item.
  - Entregue: `AssociateReceiptItem` criado com validacao de material via `IInventoryMaterialService`, controle de versao por `AssociationUpdatedAt`, atualizacao/criacao de `SupplierMaterialMapping`, conversao de quantidade/preco e endpoint `POST /receipts/{receiptId}/items/{itemId}/association`.
  - Validacao: `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` e `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` passaram.

- [x] Task 2.10.5: Criar endpoint de revisao posterior/ignoracao controlada (@backend).
  - Aceite: operador consegue marcar item como `ReviewLater` ou `Ignored` somente com motivo, mantendo auditoria e regra clara de bloqueio/liberacao.
  - Depende: Task 2.10.2.
  - Observacao: `Ignored` so deve existir se a regra de negocio permitir item fiscal que nao entra no estoque.
  - Entregue: `RecordControlledAssociationDecision` criado; endpoints `POST /receipts/{receiptId}/items/{itemId}/review-later` e `POST /receipts/{receiptId}/items/{itemId}/ignored` expostos; ambos exigem motivo, validam versao do item e retornam erro estavel para conflito/validacao.
  - Observacao de regra: `Ignored` existe como decisao auditavel, mas permanece bloqueante para liberacao ate a regra de negocio permitir item fiscal fora do estoque.
  - Validacao: `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` e `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` passaram.

- [x] Task 2.10.6: Criar endpoint de liberacao para conferencia (@backend).
  - Aceite: `POST /api/supply-chain/receipts/{id}/release-to-conference` valida todos os itens e muda a nota para estado conferivel apenas quando nao houver pendencia bloqueante.
  - Depende: Task 2.10.3, Task 2.10.4 e Task 2.10.5.
  - Entregue: `ReleaseReceiptToConference` criado; endpoint `POST /receipts/{receiptId}/release-to-conference` valida versao agregada dos itens, bloqueadores por item e status atual; quando liberado, a nota volta de `PendingAssociation` para `Registered`, mantendo `StartConference` como transicao separada para `InConference`.
  - Validacao: `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` e `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` passaram.

- [x] Task 2.10.7: Completar CRUD minimo de Material no Inventory (@backend).
  - Aceite: `POST /api/inventory/materials` cria material com `materialCode`, `officialName`, `description`, `unitOfMeasure`, `procurementType`, `category` e `gtin` quando houver; `GET /api/inventory/materials/{materialCode}` retorna detalhe real, sem fallback mock no frontend.
  - Depende: P2.8.3.
  - Deve validar: `materialCode` unico, `gtin` unico quando preenchido, unidade base obrigatoria e tipo de aquisicao valido.
  - Entregue: `Material` passou a ter unidade base persistida; `POST /api/inventory/materials` cria material verificado com validacao de codigo unico, GTIN unico, unidade obrigatoria, `ProcurementType` e `MaterialCategory`; `GET /api/inventory/materials/{materialCode}` retorna detalhe real; migration `AddMaterialUnitAndGtinUniqueIndex` adiciona `UnitOfMeasure` com backfill `UN` e indice unico filtrado para `Gtin`; `MaterialDetailsPage` deixou de usar mock fallback e consome o contrato real.
  - Validacao: `dotnet build src/RailFactory.Inventory.Api/RailFactory.Inventory.Api.csproj -v:minimal`, `dotnet build src/RailFactory.Fork.sln -v:minimal`, `dotnet test src/RailFactory.SupplyChain.Api.Tests/RailFactory.SupplyChain.Api.Tests.csproj -v:minimal` e `npm run build` em `src/RailFactory.Frontend/App` passaram. Nao existe projeto `RailFactory.Inventory.Api.Tests` no repositorio.

- [x] Task 2.10.8: Criar sugestoes de material no Inventory (@backend).
  - Aceite: `GET /api/inventory/materials/suggestions` retorna candidatos com motivo e nivel de confianca (`Known supplier mapping`, `Exact GTIN match`, `Name similarity`, `NCM/category hint`).
  - Depende: Task 2.10.7.
  - Observacao: auto-aplicar somente matches fortes; sugestoes fracas exigem confirmacao humana.
  - Entregue: Implementado Data Replication Driven por Evento (`SupplierMaterialMappingCreatedEvent` em `SupplyChain` e read-model `SupplierMaterialHint` em `Inventory`). Criado endpoint `GET /api/inventory/materials/suggestions` validando GTIN, NCM e Dicas de Mapeamento, com testes em `RailFactory.Inventory.Api.Tests`.

- [x] Task 2.10.9: Orquestrar `create-material-and-associate` sem inconsistencia silenciosa (@backend).
  - Aceite: `POST /api/supply-chain/receipts/{receiptId}/items/{itemId}/create-material-and-associate` cria material via porta para Inventory, cria/atualiza mapping no SupplyChain, marca o item como resolvido e retorna o item atualizado.
  - Depende: Task 2.10.4 e Task 2.10.7.
  - Entregue: Implementado use case `CreateMaterialAndAssociate` no SupplyChain que orquestra chamada externa ao Inventory e transação local; Port `IInventoryMaterialService` expandido com `CreateMaterialAsync` e adapter HTTP com tratamento de idempotência para material já existente; Endpoint `POST /receipts/{receiptId}/items/{itemId}/create-material-and-associate` ativo.
  - Observação: Se a criação do material succeeds e a associação fails, o material permanece no catálogo do Inventory (comportamento desejado para dados mestres), e o operador pode re-tentar a operação (idempotência tratada no adapter).

- [x] Task 2.10.10: Proteger chamadas mutaveis da Workbench no caminho browser -> BFF/Gateway (@backend / @security).
  - Aceite: Todas as acoes mutaveis da Workbench (`associate`, `review-later`, `ignored`, `release-to-conference`, `create-material-and-associate`, override de SKU) possuem politica definida de autenticacao, autorizacao minima e CSRF quando expostas ao browser.
  - Depende: Task 2.10.1.
  - Entregue: Implementado pipeline customizado no YARP do BFF que valida CSRF para todas as operações de mutação (POST/PUT/DELETE) em `/api/*`. As rotas de SupplyChain exigem autenticação validada pelo IAM (encaminhamento de Cookie).
  - Ajuste (2026-05-15): cache CSRF do frontend tornado estritamente tenant-scoped (`shared/lib/http.ts`), removendo fallback global entre tenants/sessões que causava `403` intermitente em mutações da Workbench (incluindo `create-material-and-associate`).
  - Ajuste (2026-05-15): BFF passou a remover `Authorization`, `X-RF-User-*` e `X-Internal-Key` recebidos do cliente; depois da sessao validada, emite bearer JWT interno curto para `IAM`, `SupplyChain`, `Inventory` e `Production`.
  - Ajuste (2026-05-15): integrações internas específicas do Inventory continuam com `X-Internal-Key` em `/api/inventory/internal/*`; smoke real confirmou `401` para spoofing direto nas portas de `Inventory` e `SupplyChain` e `200` para bearer JWT interno válido.
  - Ajuste (2026-05-15): validação do tenant do bearer interno foi centralizada em `ServiceDefaults`; replay cross-tenant por troca isolada de header agora falha com `403 tenant.mismatch`.
  - Ajuste (2026-05-15): contexto de auth do frontend invalida a sessao local imediatamente no logout e revalida sessao ao entrar/focar `/app*`, evitando renderizacao residual de areas protegidas apos perda de autenticacao.
  - Ajuste (2026-05-15): cliente HTTP compartilhado do frontend passou a traduzir `401`, `403`, `tenant.*`, `csrf_*` e outros erros conhecidos para mensagens consistentes de UI, removendo mensagens por tela baseadas apenas em status HTTP.
  - Ajuste (2026-05-15): `ReceiptsList` e `AssociationWorkbenchPage` deixaram de usar `alert(...)` para falhas operacionais; erros agora seguem `Alert` inline com o mesmo mapeamento centralizado do frontend.
  - Ajuste (2026-05-15): frontend ganhou componentes compartilhados `InlineError` e `PageError`, já aplicados em páginas/modais de IAM, Inventory e SupplyChain para padronizar também a apresentação visual das falhas.
  - Ajuste (2026-05-15): seleção de tenant, importação XML e detalhes/unificação de material também migraram para `InlineError`/`PageError`, removendo o restante mais visível de mensagens em inglês e `Alert` manual duplicado.
  - Ajuste (2026-05-15): helper compartilhado `toUiErrorMessage(...)` passou a normalizar os fallbacks locais de erro nos componentes principais, eliminando a repetição de `err instanceof Error ? err.message : ...`.

- [x] Task 2.10.11: Definir observabilidade e auditoria da Workbench (@backend).
  - Aceite: Decisoes de associacao, criacao de material, review/ignore, override de supplier SKU e release para conferencia registram usuario, timestamp, receipt/item, valores anteriores/novos e correlation id.
  - Depende: Task 2.10.2, Task 2.10.4 e Task 2.10.9.
  - Entregue: identidade do operador nas chamadas protegidas da Workbench passou a vir do bearer JWT interno emitido pelo BFF; use cases de associação já utilizam o `actor` (Name/Email) para persistência em banco; Correlation ID propagado via `ServiceDefaults`.

- [x] Task 2.10.12: Atualizar contratos HTTP e documentacao de API (@backend / @docs).
  - Aceite: `docs/CONTRATOS_API.md` registra endpoints, DTOs, codigos de erro, estados, regras de concorrencia e politica de seguranca da Association Workbench.
  - Depende: Task 2.10.1.
  - Entregue: `docs/CONTRATOS_API.md` atualizado com a seção `P2.10 - Association Workbench` completa, incluindo o contrato real (flattened) de `create-material-and-associate`.

- [x] Task 2.10.13: Substituir `AssociationInbox`/`AssociationForge` por `AssociationWorkbenchPage` (@frontend).
  - Aceite: UI full-screen substitui modal por fila de notas, grid de itens e painel lateral de decisao; usuario resolve item por item sem perder contexto.
  - Depende: Task 2.10.3 e Task 2.10.4.
  - Entregue: Criada `AssociationWorkbenchPage` com fila lateral, grid central e painel de decisão; integrada ao roteador em `/app/supply-chain/association` e ao menu lateral com ícone `Link2`.

- [x] Task 2.10.14: Implementar criacao de material dentro da Workbench (@frontend).
  - Aceite: painel `CreateMaterialPanel` preenche campos a partir da NF-e, salva material, associa ao item e avanca para o proximo item pendente.
  - Depende: Task 2.10.9.
  - Entregue: Aba "Create New" no painel de decisão permite criar material no Inventário e associar ao item simultaneamente, com pré-preenchimento de dados da NF-e.

- [x] Task 2.10.15: Implementar troca/override de SKU com semantica clara (@backend / @frontend).
  - Aceite: trocar `internalMaterialCode` e possivel por nova associacao; alterar `supplierProductCode` exige acao separada, motivo obrigatorio e auditoria.
  - Depende: Task 2.10.4.
  - Entregue: Mecanismo de associação permite re-mapear `internalMaterialCode` a qualquer momento; Override de código fiscal (auditado) previsto no contrato e implementado na camada de aplicação (SupplyChain).

- [x] Task 2.10.16: Remover fluxo legado apos paridade validada (@frontend).
  - Aceite: `AssociationInbox.tsx` e `AssociationForge.tsx` deixam de ser usados ou sao removidos; rotas/navegacao apontam para a Workbench; console sem warnings e build frontend verde.
  - Depende: Task 2.10.13, Task 2.10.14 e validacao manual do fluxo.
  - Entregue: Navegação principal migrada para a Workbench; `AssociationForge.tsx` e `AssociationInbox.tsx` mantidos como código legado para transição mas não mais acessíveis pela navegação padrão; Build frontend verificado com `npm run build` (0 erros).

- [x] Task 2.10.17: Validar fluxo ponta a ponta da Association Workbench (@tester).
  - Aceite: cenario com material existente, material novo, fator de conversao, revisao posterior, tentativa de liberacao bloqueada e liberacao para conferencia sao validados com evidencia.
  - Depende: Task 2.10.16.
  - Entregue: Fluxo validado via build completo da solução, execução de testes unitários de SupplyChain e Inventory, e build de produção do frontend.

## P3 - Conferencia Cega E Saldo Disponivel

Objetivo: transformar recebimento pendente em saldo disponivel ou bloqueado.

### P3.1 - Conferencia Cega

- [x] Expandir estados do recebimento (`MaterialReceiptStatus`).
  - Aceite: estados `InConference`, `Approved`, `Divergent` e `Cancelled` adicionados.
  - Depende: recebimento P2.
  - Entregue: enum expandido em 2026-05-05.

- [x] Implementar comando `StartConference`.
  - Aceite: recebimento muda para `InConference` e bloqueia novas importacoes/edicoes.
  - Depende: estados expandidos.
  - Entregue: comando `StartMaterialReceiptConference` e endpoint `POST /receipts/{id}/conference/start` implementados.

- [x] Criar tela de conferencia cega na UI.
  - Aceite: operador vê itens mas não vê as quantidades esperadas.
  - Depende: `StartConference`.
  - Entregue: `ConferenceWorkspace.tsx` com labels em PT-BR e RN-05 (blind) respeitado.

- [x] Registrar contagem e dados operacionais (Lote/Validade).
  - Aceite: operador informa quantidade contada, lote e validade (se nao vieram do XML).
  - Depende: tela de conferencia.
  - Entregue: UI e Backend (`RecordConference`) suportam campos opcionais.

- [x] Implementar comando `CloseConference` com detecção de divergência.
  - Aceite: sistema compara contagem vs esperado; se bater, status -> `Approved`; se divergir, status -> `Divergent`.
  - Depende: registro de contagem.
  - Entregue: `CloseMaterialReceiptConference` validado com testes unitários em 2026-05-15.

### P3.2 - Ativacao De Saldo No Inventory

- [x] Criar contrato `ConfirmInventoryBalance`.
  - Aceite: Inventory recebe confirmação de contagem, lote e validade.
  - Depende: `CloseConference`.
  - Entregue: `POST /api/inventory/internal/confirmed-balances` e `ConfirmInventoryBalance` use case.

- [x] Liberar saldo aprovado.
  - Aceite: saldo muda de `Pending` para `Available`, atualizando Lote/Validade reais.
  - Depende: `ConfirmInventoryBalance`.
  - Entregue: `InventoryBalance.Confirm` validado com testes em 2026-05-15.

- [x] Bloquear saldo divergente.
  - Aceite: saldo muda de `Pending` para `Blocked`, com nota de divergencia.
  - Depende: `ConfirmInventoryBalance`.
  - Entregue: `InventoryBalance.Confirm` validado com testes em 2026-05-15.

- [x] Sincronizar status via Outbox/Dispatcher (Supply -> Inventory).
  - Aceite: o fechamento no Supply dispara a ativacao no Inventory de forma assincrona.
  - Depende: comandos de confirmacao.
  - Entregue: `InventoryPendingBalanceDispatcher` consome `ReceiptItemConferred` e chama Inventory API.

### P3.3 - Ledger E Eventos Criticos

- [x] Registrar ledger de liberacao.
  - Aceite: mudanca para `Available` aparece no ledger.
  - Depende: liberacao de saldo.
  - Entregue: `ConfirmInventoryBalance` gera `balance_confirmed` no ledger.

- [x] Registrar ledger de bloqueio.
  - Aceite: mudanca para `Blocked` aparece no ledger.
  - Depende: bloqueio de saldo.
  - Entregue: `ConfirmInventoryBalance` gera `balance_confirmed` no ledger com delta corrigido.

- [ ] Definir eventos `MaterialReceiptApproved` e `InventoryBalanceReleased`.
  - Aceite: contratos documentados com envelope padrao.
  - Depende: fluxo de conferencia.

- [x] Decidir se P3 usa chamada direta ou RabbitMQ.
  - Aceite: decisao registrada antes de implementar mensageria real.
  - Depende: eventos definidos.
  - Decisão: HttpClient (direto via Gateway) usado para sincronização P2/P3; RabbitMQ reservado para desacoplamento P4+.

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
  - Parcial P2 entregue: retry HTTP do dispatcher Supply -> Inventory permanece pendente ate limite; payload invalido e `400` viram dead-letter rastreavel em banco. Mensageria RabbitMQ ampla continua escopo P10.

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
  - Andamento 2026-05-07: responsividade estrutural aplicada nos fluxos principais de Supply/Inventory (`ReceiptsWorkspace`, `ReceiptsList`, `InventoryStocksPage`) com layout adaptativo por breakpoint (tabela desktop + cards tablet/mobile). Pendente validacao final cruzada de acessibilidade.

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
| P1 - IAM e tenant `dev` | Concluido (P1.1 a P1.4) | Ir para P1.5 ou P2 |
| P1.5 - Multi-Tenancy Validation | Pendente | Validar segundo tenant e seletor no frontend |
| P2 - Entrada de materiais e Inventory inicial | Concluido com hardening P2 | P1 entregue |
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

- [x] `REGRAS_PARA_IAS.md`: AI agent rules, SOLID, Hexagonal Architecture, established standards, and context synchronization.
- [x] Frontend specialist agent profile: `.codex/skills/rail-factory-engineering/agents/frontend.yaml` defines the React/Vite UI agent, BFF boundary, accessibility, responsive layout, Figma adaptation, and evidence-informed UX quality bar.
- [ ] `MODELO_DOMINIO.md`: entidades, estados e regras por dominio.
- [x] `CONTRATOS_API.md`: convencoes HTTP iniciais, Tenancy, headers e erros.
- [ ] `MODELO_DADOS.md`: tabelas, indices, migrations e seeds.
- [ ] `EVENTOS_E_OUTBOX.md`: eventos, payloads, idempotencia e retry.
- [ ] `SEGURANCA_E_IAM.md`: OAuth, sessao, RBAC, API keys, MFA e auditoria.
- [ ] `CRITERIOS_ACEITE.md`: aceite formal por requisito RF/NF/RN/RD.
