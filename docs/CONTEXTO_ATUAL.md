# Contexto Atual De Implementacao

Atualizado em: 2026-05-03.

Este arquivo e a fonte principal do estado real do fork. Os outros documentos descrevem arquitetura, requisitos e plano; este documento registra o que existe no codigo agora, o que foi validado e o que falta para a proxima etapa.

## Estado Atual

O fork possui uma base tecnica executavel em `src/`.

| Passada | Estado | Observacao |
|---|---|---|
| P0 - Base tecnica | Concluido inicial | Base operacional, defaults, contratos iniciais e grafo Aspire validados |
| P1 - IAM e tenant `dev` | Em andamento | Resolver/middleware por `X-Tenant-Code` implementados, baseline em camadas aplicado e fluxo OAuth Google iniciado no encadeamento Frontend -> BFF -> Gateway -> IAM |
| P2 - Entrada de materiais e inventory inicial | Concluido | Fluxo de recebimento manual/XML com criacao assincrona de saldo pendente implementado |
| P3+ | Nao iniciado | Fluxos de negocio avancados ainda nao foram implementados |

Foram criados:

- solution `RailFactory.Fork.sln`;
- solution `RailFactory.Fork.slnx`;
- AppHost Aspire;
- ServiceDefaults;
- BuildingBlocks compartilhado;
- Gateway YARP;
- Frontend BFF .NET;
- UI React/Vite;
- API de Tenancy com primeira regra real;
- APIs tenant-aware de IAM, SupplyChain, Inventory e Production com contrato `/info` definitivo;
- contratos HTTP iniciais em `docs/CONTRATOS_API.md`;
- regras operacionais para IAs em `AGENTS.md` e `docs/REGRAS_PARA_IAS.md`;
- skill local `.codex/skills/rail-factory-engineering`.

## Projetos Criados

| Projeto | Estado | Observacao |
|---|---|---|
| `RailFactory.AppHost` | Criado e executavel | Sobe o grafo Aspire local |
| `RailFactory.ServiceDefaults` | Criado | Health, OpenTelemetry, service discovery, resiliencia HTTP, correlation id, ProblemDetails e modulo tenant-aware reutilizavel |
| `RailFactory.BuildingBlocks` | Criado | Entidades, aggregate root, domain events, envelope de evento, ports de repositorio/publicador, Result/Error, TenantContext e Clock |
| `RailFactory.Gateway` | Criado | YARP roteando para os servicos iniciais |
| `RailFactory.Frontend` | Criado | BFF .NET com endpoint de status |
| `RailFactory.Frontend/App` | Criado | UI React/Vite inicial |
| `RailFactory.Tenancy.Api` | Iniciado | Tenant `dev` persistido no Tenant Catalog, caso de uso `GetTenantByCode`, repositorio EF Core com migrations, `/tenants/{code}`, `/info`, `/health` e `/alive` |
| `RailFactory.Iam.Api` | Baseline arquitetural aplicado | Estrutura em camadas (Api/Application/Domain/Infrastructure) com contrato `/info` definitivo |
| `RailFactory.SupplyChain.Api` | Baseline arquitetural aplicado | Estrutura em camadas (Api/Application/Domain/Infrastructure) com contrato `/info` definitivo |
| `RailFactory.Inventory.Api` | Baseline arquitetural aplicado | Estrutura em camadas (Api/Application/Domain/Infrastructure) com contrato `/info` definitivo |
| `RailFactory.Production.Api` | Baseline arquitetural aplicado | Estrutura em camadas (Api/Application/Domain/Infrastructure) com contrato `/info` definitivo |

## Infra Local No Aspire

O AppHost foi configurado com:

- PostgreSQL;
- PgAdmin;
- Tenant Catalog DB;
- bancos tenant `dev` para IAM, SupplyChain, Inventory e Production;
- Redis;
- RabbitMQ;
- Gateway;
- BFF;
- UI React/Vite como executable `frontend-ui`.

O PostgreSQL local usa senha de desenvolvimento estavel e volume `rail-factory-fork-postgres-data-v2`. Isso evita falha de autenticacao causada por volume persistente com senha aleatoria antiga.

O Tenancy inicializa de forma idempotente a tabela `tenants` no Tenant Catalog e garante o seed do tenant `dev`.

## Rotas Validadas

Com o AppHost rodando, foram validadas as rotas abaixo.

Observacao: as portas de projetos .NET podem mudar entre execucoes. Use o dashboard Aspire como fonte atual das portas. A porta da UI foi configurada como `5082`.

| Rota | Resultado |
|---|---|
| `http://localhost:5082` | UI Vite responde |
| `http://localhost:38307/api/status` | BFF chama o Gateway e retorna status |
| `http://localhost:36867/info` | Gateway responde |
| `http://localhost:36867/api/tenancy/info` | Tenancy via Gateway responde |
| `http://localhost:36867/api/tenancy/tenants/dev` | Retorna tenant `dev` |
| `http://localhost:36867/api/tenancy/tenants/missing` | Retorna `application/problem+json` 404 com `correlationId` |
| `http://localhost:36867/api/iam/info` | IAM via Gateway responde |
| `http://localhost:36867/api/supply-chain/info` | SupplyChain via Gateway responde |
| `http://localhost:36867/api/inventory/info` | Inventory via Gateway responde |
| `http://localhost:36867/api/production/info` | Production via Gateway responde |
| `http://localhost:<iam-port>/info` sem `X-Tenant-Code` | Retorna `400` com `tenant.code_required` |
| `http://localhost:<iam-port>/info` com `X-Tenant-Code: dev` | Retorna `200` com tenant resolvido (`code`, `locale`, `timeZone`) |
| `http://localhost:<iam-port>/info` com `X-Tenant-Code: missing` | Retorna `404` com `tenant.not_found` |

## Comandos De Verificacao

```bash
dotnet build Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

Resultado validado: build com `0 Warning(s)` e `0 Error(s)`.

Observacao: usar o projeto AppHost como alvo de validacao por enquanto. O build por solution precisa ser revisado antes de voltar a ser criterio oficial.

```bash
npm run build
```

Executar em:

```bash
Rail-Factory-Fork/src/RailFactory.Frontend/App
```

Resultado validado: build Vite concluido.

```bash
dotnet test src/RailFactory.Iam.Api.Tests/RailFactory.Iam.Api.Tests.csproj -v:minimal
dotnet test src/RailFactory.Frontend.Tests/RailFactory.Frontend.Tests.csproj -v:minimal
```

Resultado validado: suites unitarias de auth/session verdes (IAM: 5 testes; Frontend/BFF: 6 testes).

```bash
npm test
```

Executar em:

```bash
Rail-Factory-Fork/src/RailFactory.Frontend/App
```

Resultado validado: testes de UI auth (`useAuthSession`) verdes (2 testes).

Estado funcional validado:

- middleware tenant-aware ativo e erros padronizados para tenant ausente/inexistente;
- propagacao de `locale/timezone` do tenant `dev` funcionando;
- `ServiceDefaults` modularizado (correlation, problem details, health, telemetry, tenancy);
- IAM, SupplyChain, Inventory e Production em baseline arquitetural definitivo (Api/Application/Domain/Infrastructure), com `Program.cs` enxuto e endpoints em `Api/*Endpoints`;
- Gateway e Frontend/BFF alinhados ao mesmo padrao de composicao minima (`Infrastructure` + `Api`);
- fluxo OAuth iniciado com endpoints `GET /api/auth/google/start`, `GET /auth/google/callback` e `GET /auth/google/finalize`;
- Frontend/BFF como edge unico com proxy interno de `/api/*` e `/auth/google/*` para o Gateway;
- Frontend/BFF centraliza a origem publica em `Frontend:PublicOrigin`, normaliza `returnUrl` recebido da UI e repassa ao IAM apenas URL publica segura;
- UI React/Vite chama somente `/api/*` e envia `returnUrl` relativo; a origem publica nao e mais montada no browser;
- UI React/Vite possui rota protegida inicial em `/app`, liberada apos retorno OAuth com `oauth=success` e bloqueada por sessao real do servidor; enquanto valida sessao, a UI exibe estado de carregamento e evita flicker de redirecionamento;
- validacao da rota protegida da UI migrada para sessao real via BFF (`GET /api/auth/session`), que encaminha cookie + `X-Tenant-Code` ao IAM (`GET /auth/session`) e usa resposta autenticada do servidor como fonte de verdade (sem `localStorage` para auth);
- contrato de sessao endurecido no IAM e no BFF: `200` autenticado e `401` nao autenticado com payload consistente para consumo previsivel da UI;
- contrato de auth/session consolidado em tipo compartilhado (`RailFactory.BuildingBlocks/Auth/AuthSessionDto`) reutilizado por IAM e BFF, removendo DTO local duplicado;
- IAM com portas de aplicacao para auth externo (`IExternalIdentityProvider`, `StartExternalLogin`, `FinalizeExternalLogin`) e adapter Google em infraestrutura, mantendo endpoints publicos atuais;
- IAM com provisionamento local de usuario no `finalize` OAuth: upsert em `iam_local_users` no banco `iamdb` com chave (`external_provider + external_subject`) via EF Core (`IamAuthDbContext` + repositorio), com migrations formais aplicadas no startup (`Database.Migrate`);
- BFF com mapeamento consistente de erro para UI (`unauthorized`, `oauth_error`, `tenant_error`) nos endpoints de auth/session;
- BFF com CSRF ativo para rotas mutaveis de auth (`GET /api/auth/csrf` + validacao em `POST /api/auth/logout`) e erro padronizado `csrf_error` em falha de token;
- BFF com validacao CSRF de logout robusta para edge com proxy HTTPS (`X-Forwarded-Proto`): normaliza scheme antes do `ValidateRequestAsync` e retorna erro controlado `csrf_https_required` quando a request chega sem HTTPS efetivo;
- logout ponta a ponta implementado (`POST /api/auth/logout` no BFF -> `POST /auth/logout` no IAM), com encerramento de cookie/sessao no servidor;
- endpoint de usuario atual protegido no IAM (`GET /auth/current-user`) com contrato compartilhado `AuthSessionDto`;
- autorizacao minima aplicada no IAM com rotas protegidas explicitas (`GET /auth/current-user` e `POST /auth/logout` com `RequireAuthorization`) e rotas publicas de OAuth/sessao em `AllowAnonymous`;
- UI com modulo de auth reutilizavel (`src/RailFactory.Frontend/App/src/auth`) e estados padronizados (`loading`, `authenticated`, `unauthenticated`, `error`) para rotas protegidas;
- Vite local recebe `VITE_ALLOWED_HOST` definido no proprio Vite via leitura de `.env.local`, mantendo o host ngrok permitido sem depender do AppHost;
- hardening de OAuth aplicado no IAM (`UseForwardedHeaders`, cookie `Secure`/`HttpOnly`/`SameSite=Lax`, normalizacao de `returnUrl`, validacao de configuracao Google quando OAuth esta ativo e origem publica explicita para o `redirect_uri` do Google na autorizacao);
- falhas de OAuth no callback agora retornam fluxo controlado para a UI (`oauth=error`) com log estruturado e sem vazamento de stack trace para UX;
- registro de autenticacao Google no IAM ajustado para `AddOAuth<GoogleOptions, GoogleOAuthPublicOriginHandler>(...)` com defaults do provider Google + handler custom no token exchange (`ExchangeCodeAsync`) para manter o mesmo `redirect_uri` publico no authorize e no token endpoint, evitando `NullReferenceException` no `Challenge` e `redirect_uri_mismatch` no callback.
- ajuste no registro de eventos OAuth para preservar simultaneamente `OnRedirectToAuthorizationEndpoint` (origem publica canônica) e `OnRemoteFailure` (erro controlado), evitando regressao de `redirect_uri` para localhost;
- AppHost pronto para deploy Aspire Docker Compose (`AddDockerComposeEnvironment`) com exposicao externa apenas do `frontend`;
- credenciais Google e origem publica injetadas por parametros externos do AppHost (`google-client-id`, `google-client-secret`, `frontend-public-origin`) no IAM e no Frontend/BFF, enquanto o Vite dev server usa a configuracao local do proprio frontend para o host allowlist.
- parametro `frontend-public-origin` ajustado no AppHost para `secret: true`, alinhando persistencia via `aspire secret set` com os demais parametros OAuth.
- persistencia local do IAM simplificada para identificacao por provedor externo (`external_provider + external_subject`) sem coluna `tenant_code` em `iam_local_users`, com migracao automatica no initializer para remover coluna/indice/chave antigos.
- persistencia do IAM local users migrada de SQL direto (`NpgsqlDataSource`/command text) para ORM (EF Core + provider PostgreSQL), mantendo a mesma tabela/contrato funcional;
- migrations EF adicionadas em `Infrastructure/Auth/Persistence/Migrations` (`InitialIamLocalUsers` e `LegacyTenantCodeCleanup`) para versionar criacao do schema e limpeza legada de `tenant_code`.
- persistencia do Tenancy migrada de SQL direto para ORM (EF Core + provider PostgreSQL), com `TenancyDbContext`, repositorio EF e migration `InitialTenantCatalog` em `Infrastructure/Persistence/Migrations`;
- auditoria dos micros concluida em 2026-05-03: nao ha uso ativo de `NpgsqlDataSource/CreateCommand` em `src/`; persistencia relacional nesta passada esta padronizada em ORM nos servicos com banco (IAM e Tenancy).
- validacao de entrada padronizada em duas camadas nos fluxos HTTP de IAM/Tenancy: DTO na borda (DataAnnotations + `ValidationProblem`) e validacao de regra mantida na Application/Domain.
- organizacao de borda refinada para SRP em IAM/Tenancy: DTOs de request e validadores extraidos para arquivos dedicados (`Api/Requests` + `Api/Validation`), mantendo endpoints/program focados em orquestracao HTTP.
- AI agent rule documents consolidated in English: `docs/REGRAS_PARA_IAS.md` is the detailed engineering-quality source, `AGENTS.md` is the short entry guide, and the local skill reference is aligned with the established standards.
- P2 implementado em SupplyChain/Inventory/UI:
  - SupplyChain com persistencia EF Core + migrations (`suppliers`, `material_receipts`, `material_receipt_items`, `supply_audit_entries`, `supply_outbox_messages`);
  - endpoints novos em SupplyChain: `POST /suppliers`, `POST /receipts`, `GET /receipts`, `POST /receipts/import/xml`;
  - provider XML interno substituivel (`INfeProvider` + `BasicXmlNfeProvider`) para importacao inicial;
  - auditoria basica de entrada registrada em `supply_audit_entries` com usuario/tenant/acao/metadata;
  - integracao assincrona Supply -> Inventory via outbox + dispatcher HTTP para `POST /internal/pending-balances`;
  - Inventory com persistencia EF Core + migrations (`stock_locations`, `inventory_balances`, `inventory_ledger_entries`, `inventory_processed_integration_messages`);
  - Inventory implementa `CreatePendingBalance` com idempotencia por `eventId` e gera ledger append-only em criacao de saldo `Pending`;
  - endpoint funcional para consulta de pendencias: `GET /balances/pending`;
  - UI React/Vite com rotas protegidas P2 (`/app/receipts`, `/app/new-receipt`, `/app/import-xml`) consumindo BFF/Gateway para lista, criacao manual e importacao XML.

P1.2 validado operacionalmente em 2026-05-02:

- credenciais reais configuradas externamente no AppHost;
- `frontend-public-origin` aplicado no IAM/BFF;
- redirect real para Google com `redirect_uri=https://unarticulative-unelectrical-shavon.ngrok-free.dev/auth/google/callback`;
- callback OAuth real observado em log estruturado (`/auth/google/callback` com `code=` do Google) seguido de finalize `302` para UI.

Validacao operacional adicional executada em 2026-05-03 (UTC-3):

- build e testes reexecutados com sucesso:
  - `dotnet build src/RailFactory.AppHost/RailFactory.AppHost.csproj -v:minimal`
  - `dotnet test src/RailFactory.Iam.Api.Tests/RailFactory.Iam.Api.Tests.csproj -v:minimal`
  - `dotnet test src/RailFactory.Frontend.Tests/RailFactory.Frontend.Tests.csproj -v:minimal`
  - `npm test` e `npm run build` em `src/RailFactory.Frontend/App`
- smoke de sessao e CSRF no BFF (`http://localhost:36991`) com `X-Tenant-Code: dev`:
  - `GET /api/auth/session` sem login -> `401` (`{"authenticated":false,"user":null}`).
  - `GET /api/auth/csrf` sem HTTPS efetivo -> `400` com `code=csrf_https_required`.
  - `GET /api/auth/csrf` com `X-Forwarded-Proto: https` -> `200` com token.
  - `POST /api/auth/logout` com cookie + `X-CSRF-TOKEN` valido + `X-Forwarded-Proto: https` -> `204`.
  - `POST /api/auth/logout` com token invalido + `X-Forwarded-Proto: https` -> `403` com `code=csrf_error`.
  - `GET /api/auth/session` apos logout -> `401`.
- confirmacao de borda OAuth/proxy:
  - `GET /api/iam/auth/google/start?...` retorna `302` para Google com `redirect_uri=https://unarticulative-unelectrical-shavon.ngrok-free.dev/auth/google/callback` (sem regressao para localhost).
- evidencias de callback/finalize OAuth real continuam visiveis em logs estruturados do Frontend/BFF (`/auth/google/callback` seguido de `/auth/google/finalize`, ambos `302`).
- correcao aplicada no IAM para persistencia local real:
  - `Infrastructure/IamModule` resolve connection string por `iamdb` e fallback para `tenant-dev-iamdb`; quando ausente, falha de forma explicita na inicializacao (sem fallback in-memory).
  - apos restart do recurso IAM, schema `iam_local_users` foi criado no `iamdb` (`public.iam_local_users` presente).

Validacao tecnica adicional executada em 2026-05-03 (UTC-3), apos reorganizacao SRP de requests/validacao:

- `dotnet test src/RailFactory.Iam.Api.Tests/RailFactory.Iam.Api.Tests.csproj -v:minimal` -> `Passed` (5/5);
- `dotnet build src/RailFactory.Tenancy.Api/RailFactory.Tenancy.Api.csproj -v:minimal` -> `Build succeeded` (0 erros);
- `dotnet build src/RailFactory.AppHost/RailFactory.AppHost.csproj -v:minimal` -> `Build succeeded` (0 erros).

Validacao tecnica adicional executada em 2026-05-03 (UTC-3), apos implementacao P2:

- `dotnet build src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj -v:minimal` -> `Build succeeded` (0 erros);
- `dotnet build src/RailFactory.Inventory.Api/RailFactory.Inventory.Api.csproj -v:minimal` -> `Build succeeded` (0 erros);
- `dotnet ef migrations add InitialSupplyChainP2 --project src/RailFactory.SupplyChain.Api/RailFactory.SupplyChain.Api.csproj --context RailFactory.SupplyChain.Api.Infrastructure.Persistence.SupplyChainDbContext --output-dir Infrastructure/Persistence/Migrations` -> migration gerada com sucesso;
- `dotnet ef migrations add InitialInventoryP2 --project src/RailFactory.Inventory.Api/RailFactory.Inventory.Api.csproj --context RailFactory.Inventory.Api.Infrastructure.Persistence.InventoryDbContext --output-dir Infrastructure/Persistence/Migrations` -> migration gerada com sucesso;
- `dotnet build src/RailFactory.AppHost/RailFactory.AppHost.csproj -v:minimal` -> `Build succeeded` (0 erros; warnings de pacote preview/vulnerability advisory);
- `dotnet test src/RailFactory.Iam.Api.Tests/RailFactory.Iam.Api.Tests.csproj -v:minimal` -> `Passed` (5/5);
- `dotnet test src/RailFactory.Frontend.Tests/RailFactory.Frontend.Tests.csproj -v:minimal` -> `Passed` (6/6);
- `npm test` e `npm run build` em `src/RailFactory.Frontend/App` -> verdes.

Status final de fechamento P1.2/P1.3:

- nenhuma pendencia operacional aberta para P1.2/P1.3 apos evidencia final de persistencia em banco.

Evidencia final coletada em 2026-05-03 (UTC-3), apos novo login OAuth real:

- consulta em `iamdb.public.iam_local_users` retornou `total_users = 1`;
- registro persistido com `external_provider=google`, `external_subject` preenchido e `display_name` preenchido;
- timestamps de `first_login_at`/`last_login_at`/`updated_at` preenchidos no momento do finalize autenticado.

## Como Subir Localmente

Configurar a origem publica atual do Frontend/BFF edge pelo Aspire CLI:

```bash
aspire secret set "Parameters:frontend-public-origin" "https://<origem-publica-do-frontend>" --apphost Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

Para validar OAuth Google, configurar tambem as credenciais reais:

```bash
aspire secret set "Parameters:google-client-id" "<google-client-id>" --apphost Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
aspire secret set "Parameters:google-client-secret" "<google-client-secret>" --apphost Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

Subir pelo Aspire CLI:

```bash
aspire run --apphost Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

Alternativa direta pelo .NET CLI:

```bash
dotnet run --project Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

O dashboard Aspire usa a porta configurada abaixo:

```text
http://localhost:15179
```

O link completo de login inclui um token temporario impresso no console a cada execucao. Nao registrar esse token como valor fixo.

## Fechamento De P0

A base tecnica inicial esta fechada para permitir avancar em P1. Os padroes abaixo existem e devem ser ampliados quando novos endpoints de dominio forem criados.

| Item | Estado | Criterio para fechar |
|---|---|---|
| Contrato padrao de erro | Concluido inicial | `ProblemDetails`, excecao global e erro de dominio do Tenancy validados |
| `correlationId` | Concluido inicial | Middleware aplica/propaga `X-Correlation-Id`; validado via Gateway |
| Logs estruturados | Concluido inicial | Scope de log inclui `CorrelationId` e `TraceId`; tenant entra depois do resolver |
| Contratos HTTP iniciais | Concluido inicial | `CONTRATOS_API.md` documenta convencoes, Tenancy e contratos `/info` tenant-aware definitivos |

## Proxima Passada Recomendada

Iniciar P2 (entrada de materiais e Inventory inicial), mantendo os contratos de auth/tenant estabilizados em P1.

Execucao paralela recomendada:

- manter refatoracoes pequenas e orientadas aos fluxos de P1.2/P1.3, sem quebra de contrato publico;
- usar `docs/PLANO_DE_TASKS.md` para abrir novas tasks de refino somente quando houver necessidade real do fluxo funcional.

## Retomada Do Proximo Chat

Comecar por:

1. ler este arquivo;
2. ler `docs/PLANO_DE_TASKS.md`, secao `P1.2 - OAuth Google`;
3. configurar credenciais OAuth Google externas e `frontend-public-origin`;
4. implementar inicio/callback de login;
5. executar smoke OAuth/CSRF/logout com evidencia.

Comando de validacao principal:

```bash
dotnet build Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj -v:minimal
```

Smoke test esperado depois de subir o AppHost:

```bash
curl -sS -D - -H 'X-Correlation-Id: smoke-tenancy' http://localhost:<gateway-port>/api/tenancy/tenants/dev
```

Usar o dashboard Aspire para descobrir a porta atual do Gateway.

## AI Agent Rules

Every AI agent working on this fork must follow:

- `AGENTS.md`;
- `docs/REGRAS_PARA_IAS.md`;
- `.codex/skills/rail-factory-engineering/SKILL.md`.
