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

Contrato canonico compartilhado (`RailFactory.BuildingBlocks/Auth/AuthSessionDto`):

- `authenticated: boolean`
- `user: { name?: string, email?: string } | null`

Tabela de erros de auth para UI (payload `AuthErrorDto` quando aplicavel):

| Codigo | Quando ocorre | HTTP |
|---|---|---|
| `unauthorized` | Sessao ausente/expirada no fluxo de auth/session | `401` |
| `oauth_error` | Falha controlada em callback/finalizacao OAuth | `302` para UI com `oauth=error` e `error=<code>` |
| `tenant_error` | Tenant ausente/invalido no edge auth | `400`/`404` |

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

## P2.6 - Contratos de Humanização e UX

### GET `/api/supply-chain/receipts/{id}`

Retorna os detalhes completos de um recebimento para o modal da Visão do Conferente.

Resposta `200`:

```json
{
  "id": "uuid",
  "receiptNumber": "NFE-12345678901234567890123456789012345678901234",
  "status": "Registered",
  "supplier": {
    "id": "uuid",
    "name": "Fornecedor Exemplo",
    "taxId": "00.000.000/0001-00"
  },
  "issuedAt": "2026-05-01T10:00:00Z",
  "audit": {
    "createdAt": "2026-05-01T12:00:00Z",
    "createdBy": "Sistema",
    "conferenceStartedAt": null,
    "conferenceStartedBy": null
  },
  "canStartConference": true,
  "items": [
    {
      "id": "uuid",
      "materialCode": "MAT-001",
      "expectedQuantity": 100,
      "countedQuantity": null,
      "unitOfMeasure": "KG",
      "lotNumber": "LOTE123",
      "expirationDate": "2026-12-31"
    }
  ],
  "timeline": [
    {
      "status": "Registered",
      "occurredAt": "2026-05-01T12:00:00Z"
    }
  ]
}
```

### GET `/api/inventory/balances/{id}`

Retorna os detalhes completos de um saldo para o modal da Visão da Produção.

Resposta `200`:

```json
{
  "id": "uuid",
  "materialCode": "MAT-001",
  "unitOfMeasure": "KG",
  "status": "Pending",
  "quantities": {
    "totalPhysical": 100,
    "available": 0,
    "blocked": 0,
    "quarantine": 0
  },
  "traceability": {
    "lotNumber": "LOTE123",
    "expirationDate": "2026-12-31",
    "sourceType": "MaterialReceipt",
    "sourceReference": "NFE-1234...",
    "sourceMetadata": {
      "receiptId": "uuid",
      "supplierName": "Fornecedor Exemplo"
    }
  },
  "ledger": [
    {
      "occurredAt": "2026-05-01T12:05:00Z",
      "quantityChange": 100,
      "newStatus": "Pending",
      "reason": "Recebimento de Material",
      "user": "Sistema"
    }
  ],
  "audit": {
    "lastBlockedAt": null,
    "lastBlockedBy": null,
    "releasedAt": null,
    "releasedBy": null
  }
}
```

## P2.10 - Association Workbench

Status: planned contract for implementation. This section defines the target API and frontend workflow contract for replacing the modal-based supplier SKU association flow.

Purpose: resolve invoice supplier items into Inventory materials before blind conference, while keeping the distinction between fiscal supplier data and internal stock master data explicit.

### Vocabulary

| Term | Meaning | Mutability |
|---|---|---|
| `supplierProductCode` | Supplier SKU from the invoice XML (`cProd`). | Source data. Override only through audited exception flow. |
| `internalMaterialCode` | Internal Inventory material SKU. | Normal association target. |
| `supplierUnit` | Unit from the fiscal document. | Source data. |
| `stockUnit` | Inventory base unit for the selected material. | Comes from Inventory material. |
| `conversionFactor` | Quantity multiplier from supplier unit to stock unit. | Required when units differ or mapping needs conversion. |

### Association Item States

| State | Meaning | Blocks release to conference |
|---|---|---|
| `Pending` | Item has no accepted association decision. | Yes |
| `Mapped` | Item was mapped to an existing internal material. | No |
| `CreatedAndMapped` | Internal material was created from the invoice item and associated. | No |
| `ReviewLater` | Operator intentionally postponed the decision with a reason. | Yes |
| `Ignored` | Item is intentionally excluded from stock intake with a reason. | Decided by business rule in implementation; default is Yes until explicitly allowed. |
| `Conflict` | System detected an ambiguous or stale decision requiring human review. | Yes |

### Frontend Route

Canonical UI route:

```text
/app/supply-chain/association
```

Expected frontend workflow:

1. Load a receipt queue containing receipts in `PendingAssociation` or with unresolved association items.
2. Select a receipt from the queue.
3. Display invoice items in an operational grid with supplier code, description, NCM, GTIN/EAN, supplier unit, quantity, internal material, conversion preview and association status.
4. Select one item and use a decision panel for material suggestions, manual material search, conversion factor validation and action buttons.
5. Save each decision immediately; do not keep a hidden batch of unsaved decisions.
6. Move focus to the next blocking item after a successful decision.
7. Show recoverable errors for conflict, authorization, CSRF and partial orchestration failures.
8. Enable `Release to conference` only when the backend read model reports the receipt can be released.

### Security And Browser Boundary

All browser-facing Workbench routes are called through the Frontend/BFF public edge under `/api/*`.

Read routes may continue through the BFF reverse proxy when they only read tenant-scoped data.

Mutable Workbench actions must have an explicit security policy before implementation:

- authenticated session required;
- `X-Tenant-Code` required;
- authorization deny-by-default when permissions are introduced;
- CSRF validation required for browser-originated mutation unless a route is proven not to rely on ambient cookies;
- stable `403` errors for CSRF/authorization failures.

Target CSRF behavior:

| Error code | HTTP | When |
|---|---|---|
| `csrf_error` | `403` | Missing or invalid `X-CSRF-TOKEN` on a protected browser mutation. |
| `csrf_https_required` | `400` | CSRF token flow cannot validate because effective HTTPS is absent. |
| `authorization.denied` | `403` | Authenticated user lacks permission for the action. |
| `unauthorized` | `401` | User session is missing or expired. |

### Concurrency

Every mutable item-level request must include the item version observed by the UI.

Suggested request field:

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z"
}
```

Alternative implementation may use a numeric version or rowversion token, but the read model and mutation requests must use the same token.

Conflict response:

```json
{
  "title": "Association item was modified",
  "status": 409,
  "detail": "The receipt item was changed by another operation. Reload the workbench before saving.",
  "code": "association.item_conflict",
  "currentItem": {}
}
```

### GET `/api/supply-chain/receipts/association-queue`

Returns receipts that need association work.

Response `200`:

```json
[
  {
    "receiptId": "uuid",
    "receiptNumber": "NFE-352605...",
    "supplierName": "Fornecedor Exemplo",
    "documentNumber": "12345",
    "issuedAt": "2026-05-09T12:00:00Z",
    "status": "PendingAssociation",
    "totalItems": 5,
    "resolvedItems": 2,
    "blockingItems": 3
  }
]
```

### GET `/api/supply-chain/receipts/{id}/association-workbench`

Returns the full Workbench read model for one receipt.

Response `200`:

```json
{
  "receipt": {
    "id": "uuid",
    "receiptNumber": "NFE-352605...",
    "supplierFiscalId": "00000000000100",
    "supplierName": "Fornecedor Exemplo",
    "status": "PendingAssociation",
    "canReleaseToConference": false,
    "releaseBlockers": [
      "3 items require association decisions."
    ]
  },
  "items": [
    {
      "itemId": "uuid",
      "version": "2026-05-09T13:45:12.345678Z",
      "associationStatus": "Pending",
      "supplierProductCode": "FORN-001",
      "description": "Parafuso zincado 10mm",
      "ncm": "73181500",
      "gtin": "7890000000000",
      "supplierUnit": "CX",
      "quantity": 10,
      "unitPrice": 120.5,
      "internalMaterialCode": null,
      "internalMaterialName": null,
      "stockUnit": null,
      "conversionFactor": null,
      "reviewReason": null,
      "suggestions": [
        {
          "materialCode": "MAT-001",
          "officialName": "Parafuso zincado 10mm",
          "stockUnit": "UN",
          "confidence": "High",
          "reason": "Exact GTIN match"
        }
      ]
    }
  ]
}
```

### POST `/api/supply-chain/receipts/{receiptId}/items/{itemId}/association`

Maps one supplier invoice item to an existing internal material.

Request:

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z",
  "internalMaterialCode": "MAT-001",
  "conversionFactor": 12.0
}
```

Response `200`:

```json
{
  "itemId": "uuid",
  "version": "2026-05-09T13:46:01.000000Z",
  "associationStatus": "Mapped",
  "internalMaterialCode": "MAT-001",
  "conversionFactor": 12.0,
  "canReleaseReceiptToConference": false
}
```

Validation errors:

| Error code | HTTP | When |
|---|---|---|
| `association.material_required` | `400` | Internal material code is missing. |
| `association.material_not_found` | `404` | Internal material does not exist or is inactive. |
| `association.invalid_conversion_factor` | `400` | Conversion factor is missing, non-finite or <= 0. |
| `association.invalid_receipt_status` | `409` | Receipt cannot be changed in its current status. |
| `association.item_conflict` | `409` | Expected version does not match current item version. |

### POST `/api/supply-chain/receipts/{receiptId}/items/{itemId}/review-later`

Marks one item for later review.

Request:

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z",
  "reason": "Need purchasing confirmation before creating a new SKU."
}
```

Response `200` returns the updated item summary with `associationStatus: "ReviewLater"`.

### POST `/api/supply-chain/receipts/{receiptId}/items/{itemId}/ignored`

Marks one item as intentionally ignored. This action remains blocked until business rules confirm that fiscal items may be excluded from stock intake.

Request:

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z",
  "reason": "Service line, not a stocked material."
}
```

Response `200` returns the updated item summary with `associationStatus: "Ignored"`.

### POST `/api/supply-chain/receipts/{receiptId}/items/{itemId}/supplier-code-override`

Audited exception for correcting the supplier code used for future mappings. It must not modify the original XML.

Request:

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z",
  "correctedSupplierProductCode": "FORN-001-A",
  "reason": "Supplier sent legacy cProd; purchasing confirmed corrected code."
}
```

Response `200` returns the updated item summary and audit metadata.

### POST `/api/supply-chain/receipts/{receiptId}/release-to-conference`

Validates item states and releases a receipt to the blind conference flow.

Request:

```json
{
  "expectedReceiptVersion": "2026-05-09T13:45:12.345678Z"
}
```

Response `200`:

```json
{
  "receiptId": "uuid",
  "status": "Registered",
  "canStartConference": true
}
```

If blockers remain:

```json
{
  "title": "Receipt has unresolved association items",
  "status": 409,
  "detail": "Resolve blocking association items before releasing the receipt.",
  "code": "association.release_blocked",
  "blockers": [
    {
      "itemId": "uuid",
      "associationStatus": "Pending",
      "message": "Item requires an internal material."
    }
  ]
}
```

### POST `/api/inventory/materials`

Creates a minimal Inventory material.

Request:

```json
{
  "materialCode": "MAT-001",
  "officialName": "Parafuso zincado 10mm",
  "description": "Parafuso zincado 10mm",
  "unitOfMeasure": "UN",
  "procurementType": "Buy",
  "category": "RawMaterial",
  "gtin": "7890000000000",
  "ncm": "73181500"
}
```

Response `201`:

```json
{
  "materialCode": "MAT-001",
  "officialName": "Parafuso zincado 10mm",
  "description": "Parafuso zincado 10mm",
  "unitOfMeasure": "UN",
  "procurementType": "Buy",
  "category": "RawMaterial",
  "status": "Verified",
  "gtin": "7890000000000",
  "ncm": "73181500",
  "imageUrl": null,
  "supplierMappings": []
}
```

Validation errors:

| Error code | HTTP | When |
|---|---|---|
| `material.code_required` | `400` | Material code is missing. |
| `material.duplicate_code` | `409` | Material code already exists. |
| `material.duplicate_gtin` | `409` | GTIN already belongs to another material. |
| `material.invalid_category` | `400` | Material category is not valid. |
| `material.invalid_procurement_type` | `400` | Procurement type is not valid. |
| `material.unit_required` | `400` | Base unit is missing. |

### GET `/api/inventory/materials/{materialCode}`

Returns real material details for Inventory screens. The frontend must not use mock fallback after this endpoint is implemented.

Response `200`:

```json
{
  "materialCode": "MAT-001",
  "officialName": "Parafuso zincado 10mm",
  "description": "Parafuso zincado 10mm",
  "unitOfMeasure": "UN",
  "procurementType": "Buy",
  "category": "Hardware",
  "gtin": "7890000000000",
  "ncm": "73181500",
  "status": "Active",
  "supplierMappings": []
}
```

### GET `/api/inventory/materials/suggestions`

Returns material candidates for one invoice item.

Query parameters:

| Parameter | Required | Example |
|---|---|---|
| `description` | No | `Parafuso zincado 10mm` |
| `gtin` | No | `7890000000000` |
| `ncm` | No | `73181500` |
| `supplierFiscalId` | No | `00000000000100` |
| `supplierProductCode` | No | `FORN-001` |

Response `200`:

```json
[
  {
    "materialCode": "MAT-001",
    "officialName": "Parafuso zincado 10mm",
    "unitOfMeasure": "UN",
    "confidence": "High",
    "reason": "Exact GTIN match"
  }
]
```

### POST `/api/supply-chain/receipts/{receiptId}/items/{itemId}/create-material-and-associate`

Creates an Inventory material and associates the item in one operator action.

Request (Flattened):

```json
{
  "expectedVersion": "2026-05-09T13:45:12.345678Z",
  "materialCode": "MAT-001",
  "officialName": "Parafuso zincado 10mm",
  "description": "Parafuso zincado 10mm",
  "unitOfMeasure": "UN",
  "procurementType": "Buy",
  "category": "Hardware",
  "gtin": "7890000000000",
  "ncm": "73181500",
  "conversionFactor": 12.0
}
```

Response `200` returns the updated item summary with `associationStatus: "CreatedAndMapped"`.

Partial failure rule: if material creation succeeds but association fails, the response must expose a recoverable state and stable error code. The implementation must not return success while leaving the receipt item unresolved.

Target error:

| Error code | HTTP | When |
|---|---|---|
| `association.partial_create_material_failed` | `409` | Material may have been created but association did not complete; operator must reload and retry association. |

## P4 - Produção

Base via Gateway local:

```text
/api/production
```

Headers obrigatórios em todas as rotas (via Internal JWT emitido pelo BFF):

| Header | Obrigatorio | Observação |
|---|---|---|
| `X-Tenant-Code` | Sim | Tenant do contexto da sessão |
| `Authorization: Bearer <jwt>` | Sim (interno) | JWT Interno emitido pelo BFF com claim `tenant` |

### POST `/api/production/work-centers`

Cria um novo Work Center.

Request:

```json
{
  "code": "WC-FRESAMENTO",
  "name": "Fresamento CNC"
}
```

Response `201`:

```json
{
  "id": "uuid",
  "code": "WC-FRESAMENTO",
  "name": "Fresamento CNC",
  "status": "Active"
}
```

### GET `/api/production/work-centers`

Lista todos os Work Centers do tenant.

Response `200`:

```json
[
  {
    "id": "uuid",
    "code": "WC-FRESAMENTO",
    "name": "Fresamento CNC",
    "status": { "key": "Active", "label": "Ativo", "color": "success" }
  }
]
```

### PUT `/api/production/work-centers/{id}/deactivate`

Inativa um Work Center. Bloqueado se houver OP `Released` ou `InExecution` vinculada.

Response `204` — sem corpo.  
Response `404` — Work Center não encontrado.  
Response `409` — Work Center já inativo ou há OPs ativas vinculadas.

### PUT `/api/production/work-centers/{id}/activate`

Ativa um Work Center previamente inativo.

Response `204` — sem corpo.  
Response `404` — Work Center não encontrado.  
Response `409` — Work Center já está ativo.

---

### POST `/api/production/boms`

Cria uma nova BOM em rascunho para o produto.

Request:

```json
{
  "productCode": "PROD-TRILHO-50",
  "version": 1
}
```

Response `201`:

```json
{
  "id": "uuid",
  "productCode": "PROD-TRILHO-50",
  "version": 1,
  "status": "Draft",
  "items": [],
  "createdAt": "2026-05-27T10:00:00Z",
  "updatedAt": "2026-05-27T10:00:00Z"
}
```

### POST `/api/production/boms/{id}/items`

Adiciona um item de material à BOM (somente status `Draft`).

Request:

```json
{
  "materialCode": "MAT-ACO-4340",
  "quantity": 12.5,
  "unitOfMeasure": "KG"
}
```

Response `200` — BOM atualizada com item incluído.  
Response `404` — BOM não encontrada.  
Response `409` — Material já existe na BOM ou BOM não está em Draft.

**Nota técnica:** A persistência do `BomItem` usa raw SQL via `AddItemDirectAsync` para contornar o quirk de `ValueGeneratedOnAdd` do EF Core 10 + Npgsql 10. Veja `ISSUES_CONHECIDOS.md #3`.

### PUT `/api/production/boms/{id}/activate`

Ativa a versão da BOM. A versão `Active` anterior do mesmo produto volta para `Draft`. Requer pelo menos um item.

Response `200` — BOM atualizada com `status: "Active"`.  
Response `404` — BOM não encontrada.  
Response `409` — BOM sem itens, ou BOM não está em Draft.

### GET `/api/production/boms`

Lista BOMs, opcionalmente filtradas por `productCode`.

Query params:

| Parâmetro | Obrigatório | Exemplo |
|---|---|---|
| `productCode` | Não | `PROD-TRILHO-50` |

Response `200`:

```json
[
  {
    "id": "uuid",
    "productCode": "PROD-TRILHO-50",
    "version": 1,
    "status": { "key": "Active", "label": "Ativo", "color": "success" },
    "itemCount": 3,
    "createdAt": "2026-05-27T10:00:00Z",
    "updatedAt": "2026-05-27T10:05:00Z"
  }
]
```

---

### POST `/api/production/production-orders`

Cria uma nova Ordem de Produção em rascunho.

Request:

```json
{
  "productCode": "PROD-TRILHO-50",
  "plannedQuantity": 100,
  "workCenterId": "uuid"
}
```

Response `201` — OP criada com `status: "Draft"`.

### PUT `/api/production/production-orders/{id}/release`

Libera a OP para execução. Valida que há uma BOM `Active` para o produto e que o WorkCenter está `Active`. Emite evento `stock_reservation_requested` via RabbitMQ.

Response `200` — OP com `status: "Released"`.  
Response `409` — BOM inativa, WorkCenter inativo, ou OP não está em Draft.

### PUT `/api/production/production-orders/{id}/cancel`

Cancela a OP. Bloqueado em `InExecution` e `Completed`.

Response `200` — OP com `status: "Cancelled"`.  
Response `409` — OP não pode ser cancelada no estado atual.

### PUT `/api/production/production-orders/{id}/start-execution`

Inicia a execução da OP. Altera para `InExecution`.

Response `200` — OP com `status: "InExecution"`.

### POST `/api/production/production-orders/{id}/executions`

Registra uma entrada de execução: consumo, scrap ou inspeção.

Request (consumo):

```json
{
  "type": "Consumption",
  "materialCode": "MAT-ACO-4340",
  "quantity": 12.5,
  "unitOfMeasure": "KG"
}
```

Request (inspeção):

```json
{
  "type": "QualityInspection",
  "approved": true,
  "notes": "Aprovado na 1ª inspeção"
}
```

Response `200`.

### PUT `/api/production/production-orders/{id}/complete`

Conclui a OP. Requer pelo menos uma inspeção aprovada.

Response `200` — OP com `status: "Completed"`.

### GET `/api/production/production-orders/{id}/executions`

Lista o histórico de execuções de uma OP.

Response `200`:

```json
[
  {
    "id": "uuid",
    "type": "Consumption",
    "materialCode": "MAT-ACO-4340",
    "quantity": 12.5,
    "unitOfMeasure": "KG",
    "occurredAt": "2026-05-27T11:00:00Z"
  }
]
```

---

## P6 - Dashboards

### GET `/api/production/dashboard`

Retorna KPIs de produção agregados para o tenant.

Headers: `X-Tenant-Code` obrigatório.

Response `200`:

```json
{
  "ordersByStatus": { "Draft": 1, "Released": 2, "InExecution": 1, "Completed": 12, "Cancelled": 1 },
  "activeOrders": 3,
  "topScrap": [
    { "materialCode": "MAT-ACO-4340", "totalScrap": 8.5, "unitOfMeasure": "KG" }
  ],
  "inspectionSummary": { "passed": 11, "failed": 1, "passRate": 0.917 },
  "averageLeadTimeHours": 4.3,
  "workCenterSummary": [
    {
      "workCenterId": "uuid",
      "workCenterCode": "WC-FRESAMENTO",
      "workCenterName": "Fresamento CNC",
      "totalOrders": 8,
      "completedOrders": 6,
      "completionRate": 0.75
    }
  ]
}
```

Campos:
- `averageLeadTimeHours` — média de horas de `CreatedAt` até `Complete()` para ordens Completed. Null quando não há ordens concluídas.
- `workCenterSummary` — taxa de conclusão (`completedOrders / totalOrders`) por centro de trabalho. Exclui work centers sem ordens.

### GET `/api/inventory/dashboard`

Retorna KPIs de inventário agregados para o tenant.

Headers: `X-Tenant-Code` obrigatório.

Response `200`:

```json
{
  "totalMaterials": 47,
  "materialsWithStock": 32,
  "availableCount": 28,
  "reservedCount": 4,
  "blockedCount": 3,
  "stockAccuracy": 0.9032
}
```

Campos:
- `stockAccuracy` — `availableCount / (availableCount + blockedCount)`. Null quando nenhum saldo passou pela conferência. Saldos `Pending` e `Reserved` são excluídos do cálculo.

---

## Fronteiras de Utilitários UI (Utility Boundaries)

Utilitários de formatação devem ser globais no Frontend, encapsulando regras para consistência visual.

### HumanizedStatusMapping

Padroniza labels amigáveis e cores semânticas baseadas no status técnico.

- **Assinatura Base**: `(status: string, domain?: string) => { label: string, color: 'success' | 'warning' | 'error' | 'info' | 'default' }`
- **Exemplo**: `('Registered', 'SupplyChain')` -> `{ label: 'Aguardando Conferência', color: 'warning' }`

### RelativeDateFormatter

Padroniza formatação de datas via `Intl.DateTimeFormat` forçando a localidade para `pt-BR`.

- **Assinatura Base**: `(dateIso: string, format?: 'short' | 'long' | 'relative') => string`
- **Exemplo**: `('2026-05-01T10:00:00Z', 'long')` -> `01 de maio de 2026 às 07:00`

### TechnicalIdFormatter

Oculta ou trunca UUIDs, preferindo chaves de negócio com suporte a "Copy to Clipboard".

- **Assinatura Base**: `(id: string, businessKey?: string, truncateLength?: number) => { displayValue: string, copyValue: string }`
- **Exemplo**: `('uuid-1234...', 'NFE-999')` -> `{ displayValue: 'NFE-999', copyValue: 'uuid-1234...' }`
