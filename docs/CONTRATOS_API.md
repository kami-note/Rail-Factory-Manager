# Contratos API

Este documento registra os contratos HTTP reais do fork. Ele deve ser atualizado junto com cada endpoint novo.

## Convencoes Globais

### Headers

| Header | Direcao | Obrigatorio | Uso |
|---|---|---|---|
| `X-Correlation-Id` | Request/response | Nao | Identificador de rastreio. Se nao vier na request, o servico gera um valor. |
| `X-Tenant-Code` | Request | Sim (tenant-aware) | Tenant obrigatorio para APIs tenant-aware. Ausencia retorna erro padronizado. |

### Erro Padrao

Erros de API devem retornar `application/problem+json`.

Campos obrigatorios:

| Campo | Uso |
|---|---|
| `type` | URI de referencia do status ou erro |
| `title` | Resumo curto |
| `status` | HTTP status |
| `detail` | Mensagem legivel |
| `code` | Codigo estavel da aplicacao |
| `correlationId` | Mesmo valor de `X-Correlation-Id` |
| `traceId` | Trace tecnico |
| `service` | Nome do servico que gerou o erro |

Exemplo:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Resource not found",
  "status": 404,
  "detail": "Tenant was not found.",
  "code": "tenant.not_found",
  "traceId": "00-...",
  "correlationId": "smoke-tenancy-missing-2",
  "service": "RailFactory.Tenancy.Api"
}
```

## Tenancy

Estado atual: tenant `dev` persistido no Tenant Catalog PostgreSQL. O servico inicializa a tabela `tenants` e o seed de forma idempotente.

Base via Gateway local:

```text
/api/tenancy
```

### GET `/api/tenancy/info`

Retorna informacoes operacionais do servico Tenancy.

Resposta `200`:

```json
{
  "service": "tenancy",
  "environment": "Development",
  "purpose": "Tenant catalog and tenant resolution",
  "defaultTenant": {
    "code": "dev",
    "displayName": "Tenant de desenvolvimento",
    "locale": "pt-BR",
    "timeZone": "America/Sao_Paulo",
    "status": "Active"
  }
}
```

### GET `/api/tenancy/tenants/{code}`

Busca um tenant pelo codigo.

Parametros:

| Nome | Origem | Obrigatorio | Exemplo |
|---|---|---|---|
| `code` | Path | Sim | `dev` |

Resposta `200`:

```json
{
  "code": "dev",
  "displayName": "Tenant de desenvolvimento",
  "locale": "pt-BR",
  "timeZone": "America/Sao_Paulo",
  "status": "Active"
}
```

Resposta `404`:

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Tenant was not found.",
  "code": "tenant.not_found",
  "correlationId": "request-correlation-id"
}
```

## Contratos Operacionais Dos Microservicos Tenant-Aware

| Servico | Endpoint atual via Gateway |
|---|---|
| IAM | `GET /api/iam/info` |
| SupplyChain | `GET /api/supply-chain/info` |
| Inventory | `GET /api/inventory/info` |
| Production | `GET /api/production/info` |

## Frontend BFF

Base publica (edge unico):

```text
/
```

Observacao de topologia de deploy (Aspire): somente o recurso `frontend` possui endpoint externo; Gateway e microservicos permanecem internos.

### GET `/api/status`

Retorna status agregado do BFF e do Gateway.

Headers:

| Header | Obrigatorio | Observacao |
|---|---|---|
| `X-Tenant-Code` | Sim | Ausencia retorna erro padronizado de tenant. |

Resposta `200`:

```json
{
  "service": "frontend-bff",
  "environment": "Development",
  "tenant": {
    "code": "dev"
  },
  "gateway": {}
}
```

### GET `/api/auth/google/start`

Inicia login OAuth Google no fluxo `Frontend -> BFF -> Gateway -> IAM`.

Query params:

| Parametro | Obrigatorio | Exemplo |
|---|---|---|
| `tenantCode` | Sim | `dev` |
| `returnUrl` | Nao | `/` |

### GET `/api/auth/session`

Fachada oficial de sessao da UI no BFF. Encaminha cookie + `X-Tenant-Code` para `GET /auth/session` do IAM.

Headers:

| Header | Obrigatorio | Observacao |
|---|---|---|
| `X-Tenant-Code` | Sim | Ausencia retorna erro padronizado de tenant. |

Resposta `200` (autenticado):

```json
{
  "authenticated": true,
  "user": {
    "name": "usuario",
    "email": "usuario@exemplo.com"
  }
}
```

Resposta `401` (nao autenticado):

```json
{
  "authenticated": false,
  "user": null
}
```

Para os endpoints `/info` tenant-aware, quando o header `X-Tenant-Code` e valido, o campo `tenant` retorna:

```json
{
  "code": "dev",
  "locale": "pt-BR",
  "timeZone": "America/Sao_Paulo"
}
```

O endpoint `GET /api/iam/info` usa o contrato:

```json
{
  "service": "identity-access-management",
  "environment": "Development",
  "capability": "Identity, access, session and authorization boundary",
  "tenant": {
    "code": "dev",
    "locale": "pt-BR",
    "timeZone": "America/Sao_Paulo"
  }
}
```

OAuth Google via cadeia `Frontend -> BFF -> Gateway -> IAM`:

- `GET /api/auth/google/start?tenantCode=<code>&returnUrl=<path>` no BFF inicia o fluxo.
- UI envia `returnUrl` relativo. O BFF valida o caminho e monta a URL publica usando `Frontend:PublicOrigin`.
- BFF redireciona para `GET /api/iam/auth/google/start` no Gateway/IAM.
- Callback tecnico do provedor: `GET /auth/google/callback` no host publico (Frontend/BFF edge), com proxy interno para Gateway e IAM.
- Finalizacao tecnica: `GET /auth/google/finalize` no IAM, com redirecionamento de volta para `returnUrl`.
- Em falha OAuth de callback, IAM redireciona para a UI com `oauth=error` e `error=<codigo-normalizado>` para UX previsivel.
- `returnUrl` absoluto externo ao Frontend/BFF deve ser normalizado para `/`.

Configuracao obrigatoria para OAuth Google:

| Configuracao | Dono | Observacao |
|---|---|---|
| `Frontend:PublicOrigin` | Frontend/BFF | Origem publica HTTPS vista pelo navegador. Injetado pelo AppHost via parametro externo `frontend-public-origin`. |
| `Authentication:Google:ClientId` | IAM | Injetado pelo AppHost via parametro externo `google-client-id`. |
| `Authentication:Google:ClientSecret` | IAM | Injetado pelo AppHost via parametro externo `google-client-secret`. |
| `Authentication:Google:PublicOrigin` | IAM | Origem publica do Frontend/BFF edge. Injetado pelo AppHost via parametro externo `frontend-public-origin`. |
| `VITE_ALLOWED_HOST` | UI local Vite | Host publico permitido no dev server local. Definido no Vite local, com leitura de `.env.local`. |
| `Authentication:Google:CallbackPath` | IAM | Padrao definitivo: `/auth/google/callback`. |

O unico endpoint externo da aplicacao e o Frontend/BFF. Gateway e microservicos permanecem internos ao grafo Aspire.

O URI autorizado no Google Cloud Console deve ser formado por:

```text
{Authentication:Google:PublicOrigin}{Authentication:Google:CallbackPath}
```

Exemplo para ngrok:

```text
https://<origem-publica-do-frontend>/auth/google/callback
```

O `redirect_uri` enviado ao Google deve usar essa origem publica tanto na autorizacao quanto na troca do `code` pelo token. Nao deve apontar para `localhost`, porta interna do Aspire, Gateway ou microservico.

Configurar o parametro pelo Aspire CLI:

```bash
aspire secret set "Parameters:frontend-public-origin" "https://<origem-publica-do-frontend>" --apphost src/RailFactory.AppHost/RailFactory.AppHost.csproj
aspire secret set "Parameters:google-client-id" "<google-client-id>" --apphost src/RailFactory.AppHost/RailFactory.AppHost.csproj
aspire secret set "Parameters:google-client-secret" "<google-client-secret>" --apphost src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

Subir pelo Aspire CLI:

```bash
aspire run --apphost src/RailFactory.AppHost/RailFactory.AppHost.csproj
```

O endpoint `GET /api/supply-chain/info` usa o contrato:

```json
{
  "service": "supply-chain",
  "environment": "Development",
  "capability": "Receiving, supplier collaboration and inbound material boundary",
  "tenant": {
    "code": "dev",
    "locale": "pt-BR",
    "timeZone": "America/Sao_Paulo"
  }
}
```

O endpoint `GET /api/inventory/info` usa o contrato:

```json
{
  "service": "inventory",
  "environment": "Development",
  "capability": "Stock balance, reservation and ledger boundary",
  "tenant": {
    "code": "dev",
    "locale": "pt-BR",
    "timeZone": "America/Sao_Paulo"
  }
}
```

O endpoint `GET /api/production/info` usa o contrato:

```json
{
  "service": "production",
  "environment": "Development",
  "capability": "Manufacturing execution, work orders and quality boundary",
  "tenant": {
    "code": "dev",
    "locale": "pt-BR",
    "timeZone": "America/Sao_Paulo"
  }
}
```
