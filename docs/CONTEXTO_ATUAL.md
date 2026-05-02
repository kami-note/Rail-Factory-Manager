# Contexto Atual De Implementacao

Atualizado em: 2026-05-02.

Este arquivo e a fonte principal do estado real do fork. Os outros documentos descrevem arquitetura, requisitos e plano; este documento registra o que existe no codigo agora, o que foi validado e o que falta para a proxima etapa.

## Estado Atual

O fork possui uma base tecnica executavel em `src/`.

| Passada | Estado | Observacao |
|---|---|---|
| P0 - Base tecnica | Concluido inicial | Base operacional, defaults, contratos iniciais e grafo Aspire validados |
| P1 - IAM e tenant `dev` | Em andamento | Resolver/middleware por `X-Tenant-Code` implementados, baseline em camadas aplicado e fluxo OAuth Google iniciado no encadeamento Frontend -> BFF -> Gateway -> IAM |
| P2+ | Nao iniciado | Fluxos de negocio ainda nao foram implementados |

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
| `RailFactory.Tenancy.Api` | Iniciado | Tenant `dev` persistido no Tenant Catalog, caso de uso `GetTenantByCode`, repositorio PostgreSQL, `/tenants/{code}`, `/info`, `/health` e `/alive` |
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
- Vite local recebe `VITE_ALLOWED_HOST` definido no proprio Vite via leitura de `.env.local`, mantendo o host ngrok permitido sem depender do AppHost;
- hardening de OAuth aplicado no IAM (`UseForwardedHeaders`, cookie `Secure`/`HttpOnly`/`SameSite=Lax`, normalizacao de `returnUrl`, validacao de configuracao Google quando OAuth esta ativo e origem publica explicita para o `redirect_uri` do Google na autorizacao);
- falhas de OAuth no callback agora retornam fluxo controlado para a UI (`oauth=error`) com log estruturado e sem vazamento de stack trace para UX;
- registro de autenticacao Google no IAM ajustado para `AddOAuth<GoogleOptions, GoogleOAuthPublicOriginHandler>(...)` com defaults do provider Google + handler custom no token exchange (`ExchangeCodeAsync`) para manter o mesmo `redirect_uri` publico no authorize e no token endpoint, evitando `NullReferenceException` no `Challenge` e `redirect_uri_mismatch` no callback.
- AppHost pronto para deploy Aspire Docker Compose (`AddDockerComposeEnvironment`) com exposicao externa apenas do `frontend`;
- credenciais Google e origem publica injetadas por parametros externos do AppHost (`google-client-id`, `google-client-secret`, `frontend-public-origin`) no IAM e no Frontend/BFF, enquanto o Vite dev server usa a configuracao local do proprio frontend para o host allowlist.
- parametro `frontend-public-origin` ajustado no AppHost para `secret: true`, alinhando persistencia via `aspire secret set` com os demais parametros OAuth.

Pendencia ativa para fechar P1.2:

- configurar `Authentication:Google` com credenciais reais, definir `frontend-public-origin` com a URL HTTPS publica do Frontend/BFF e validar smoke real via ngrok no fluxo `Frontend -> BFF -> Gateway -> IAM`.

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

Continuar P1 no OAuth e sessao:

1. configuracao OAuth Google sem segredo hardcoded;
2. inicio e callback de login com `redirect_uri` usando a origem publica do Frontend/BFF;
3. sessao por cookie no BFF;
4. protecao CSRF e endpoint de usuario atual;
5. logout e autorizacao minima deny by default.

Execucao paralela recomendada:

- manter refatoracoes pequenas e orientadas aos fluxos de P1.2/P1.3, sem quebra de contrato publico;
- usar `docs/PLANO_DE_TASKS.md` para abrir novas tasks de refino somente quando houver necessidade real do fluxo funcional.

## Retomada Do Proximo Chat

Comecar por:

1. ler este arquivo;
2. ler `docs/PLANO_DE_TASKS.md`, secao `P1.2 - OAuth Google`;
3. configurar credenciais OAuth Google externas e `frontend-public-origin`;
4. implementar inicio/callback de login;
5. preparar sessao BFF para P1.3.

Comando de validacao principal:

```bash
dotnet build Rail-Factory-Fork/src/RailFactory.AppHost/RailFactory.AppHost.csproj -v:minimal
```

Smoke test esperado depois de subir o AppHost:

```bash
curl -sS -D - -H 'X-Correlation-Id: smoke-tenancy' http://localhost:<gateway-port>/api/tenancy/tenants/dev
```

Usar o dashboard Aspire para descobrir a porta atual do Gateway.

## Regras Para Agentes

Toda IA que trabalhar neste fork deve seguir:

- `AGENTS.md`;
- `docs/REGRAS_PARA_IAS.md`;
- `.codex/skills/rail-factory-engineering/SKILL.md`.
