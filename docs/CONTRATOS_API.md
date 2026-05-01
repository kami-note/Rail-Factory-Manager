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

## Placeholders Atuais

As APIs abaixo ainda possuem apenas contratos operacionais minimos. Elas nao devem receber regras de negocio antes da passada correta.

| Servico | Endpoint atual via Gateway |
|---|---|
| IAM | `GET /api/iam/info` |
| SupplyChain | `GET /api/supply-chain/info` |
| Inventory | `GET /api/inventory/info` |
| Production | `GET /api/production/info` |

Para os endpoints `/info` tenant-aware, quando o header `X-Tenant-Code` e valido, o campo `tenant` retorna:

```json
{
  "code": "dev",
  "locale": "pt-BR",
  "timeZone": "America/Sao_Paulo"
}
```
