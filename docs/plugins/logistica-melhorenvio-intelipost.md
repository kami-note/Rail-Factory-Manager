# Plugins Logística: Melhor Envio + Intelipost

**Categoria:** `shipping`
**Última Atualização:** 2026-06-01
**Microsserviço:** `Logistics.Api`

---

## 1. Melhor Envio

### Autenticação

**Tipo:** OAuth 2.0 — Authorization Code Flow

| Token | Validade |
|-------|----------|
| `access_token` | 30 dias |
| `refresh_token` | 45 dias |

**Fluxo de obtenção:**
1. Redirecionar usuário para `{base_url}/oauth/authorize?client_id=...&redirect_uri=...&response_type=code&scope=cart-read cart-write shipping-calculate shipping-generate shipping-checkout shipping-print shipping-tracking webhooks-write`
2. Usuário autoriza → recebe `code` no `redirect_uri`
3. Trocar código por token:

```
POST {base_url}/oauth/token
Content-Type: application/json

{
  "grant_type": "authorization_code",
  "client_id": 123,
  "client_secret": "...",
  "code": "...",
  "redirect_uri": "https://minha-plataforma/callback"
}
```

**Header obrigatório em todas as chamadas:**
```
Authorization: Bearer {access_token}
Accept: application/json
Content-Type: application/json
User-Agent: MinhaApp (suporte@empresa.com)
```

> `User-Agent` com nome da app + e-mail de contato técnico é obrigatório.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Produção | `https://melhorenvio.com.br` |
| Sandbox | `https://sandbox.melhorenvio.com.br` |

> Sandbox possui saldo fictício de R$ 10.000 e avança o status da etiqueta automaticamente **15 minutos** após geração/postagem.

Todos os endpoints de API: `{base_url}/api/v2/me/...`

### Endpoints Principais

| Operação | Método | Path |
|----------|--------|------|
| Cotação de frete | `POST` | `/api/v2/me/shipment/calculate` |
| Inserir no carrinho | `POST` | `/api/v2/me/cart` |
| Checkout (compra) | `POST` | `/api/v2/me/shipment/checkout` |
| Gerar etiqueta | `POST` | `/api/v2/me/shipment/generate` |
| Imprimir etiqueta | `POST` | `/api/v2/me/shipment/print` |
| Rastrear | `POST` | `/api/v2/me/shipment/tracking` |

### Campos Mínimos — Inserir no Carrinho

```json
{
  "service": 3,
  "from": {
    "name": "Remetente", "address": "Rua X", "number": "123",
    "district": "Centro", "city": "São Paulo",
    "postal_code": "01310100", "state_abbr": "SP",
    "country_id": "BR", "document": "..."
  },
  "to": {
    "name": "Destinatário", "address": "Rua Y", "number": "456",
    "district": "Centro", "city": "Rio de Janeiro",
    "postal_code": "20040020", "state_abbr": "RJ",
    "country_id": "BR", "document": "..."
  },
  "products": [{ "name": "Produto", "quantity": 1, "unitary_value": 150.00 }],
  "volumes": [{ "height": 10, "width": 15, "length": 20, "weight": 1.5 }],
  "options": {
    "insurance_value": 150.00, "receipt": false,
    "own_hand": false, "non_commercial": false,
    "invoice": { "key": "NFe..." }
  }
}
```

### Fluxo Mínimo para Emitir uma Etiqueta (7 passos)

```
1. OAuth2: GET /oauth/authorize → code
2. OAuth2: POST /oauth/token → access_token
3. Cotação: POST /api/v2/me/shipment/calculate → service_id + preço
4. Carrinho: POST /api/v2/me/cart → cart_item_id (UUID)
5. Checkout: POST /api/v2/me/shipment/checkout { orders: [uuid] }
6. Geração: POST /api/v2/me/shipment/generate { orders: [uuid] }
7. Impressão: POST /api/v2/me/shipment/print { orders: [uuid], mode: "public" } → URL do PDF
```

### Webhook de Rastreamento

**Configuração:** Painel → Integrações → Área Dev → selecionar aplicativo → Novo Webhook

**Payload:**
```json
{
  "event": "order.posted",
  "data": {
    "id": "0000aaaa-aa00-00aa-aa00-000000aaaaaa",
    "protocol": "ORD-2024XXXXXXXXXX",
    "status": "posted",
    "tracking": null
  }
}
```

| Evento | Descrição |
|--------|-----------|
| `order.created` | Etiqueta criada |
| `order.released` | Pagamento processado |
| `order.generated` | Etiqueta pronta |
| `order.posted` | Postado |
| `order.delivered` | Entregue |
| `order.undelivered` | Falha na entrega |
| `order.cancelled` | Cancelado |

**Validação:** header `X-ME-Signature` — HMAC-SHA256 do body usando o `client_secret`.

**ID de idempotência:** `data.id` (UUID).

### Rate Limits

Não documentados. Implementar throttling de 10 req/s com retry exponencial em 429/503.

---

## 2. Intelipost

### Autenticação

| Item | Valor |
|------|-------|
| Header | `api-key: <sua_api_key>` |
| Obtenção | Onboarding via `integracoes@intelipost.com.br` |

### Base URLs

| Ambiente | URL |
|----------|-----|
| Produção | `https://api.intelipost.com.br/api/v1` |
| Homologação | Configurada via segundo parâmetro booleano no SDK |

### Endpoints Principais

| Operação | Método | Path |
|----------|--------|------|
| Cotação de frete | `POST` | `/api/v1/quote` |
| Criar pedido/etiqueta | `POST` | `/api/v1/shipment_order` |
| Consultar pedido | `GET` | `/api/v1/shipment_order/{order_number}` |
| Imprimir etiqueta | `GET` | `/api/v1/shipment_order/get_label/{order_number}/{volume_number}` |
| Rastrear (polling) | `GET` | `/api/v1/shipment_order/read_status/{order_number}` |
| Cancelar pedido | `POST` | `/api/v1/shipment_order/cancel/{order_number}` |
| Marcar como enviado | `POST` | `/api/v1/shipment_order/multi/shipped/with_date` |
| Definir NF-e após criação | `POST` | `/api/v1/shipment_order/set_invoice` |

### Campos Mínimos — Criar Pedido (ShipmentOrder)

```json
{
  "order_number": "PEDIDO-001",
  "sales_channel": "loja-virtual",
  "delivery_method_id": 67,
  "origin_zip_code": "01001000",
  "end_customer": {
    "first_name": "João", "last_name": "Silva",
    "email": "joao@email.com", "phone": "11999999999",
    "document": "123.456.789-00",
    "address": "Rua das Flores", "number": "123",
    "district": "Centro", "city": "Rio de Janeiro",
    "state": "RJ", "zip_code": "20000-000", "country": "BR"
  },
  "shipment_order_volume_array": [{
    "shipment_order_volume_number": 1,
    "weight": 1.5, "height": 10.0, "width": 15.0, "length": 20.0,
    "volume_type_code": "BOX",
    "cost_of_goods": 150.00,
    "shipment_order_volume_invoice": {
      "invoice_number": "1234", "invoice_series": "1",
      "invoice_key": "chave-NFe-44-digitos",
      "invoice_date": "2024-06-01",
      "invoice_total_value": 150.00,
      "invoice_products_value": 150.00
    }
  }]
}
```

### Fluxo Mínimo para Emitir uma Etiqueta (4 passos)

```
1. Cotação: POST /api/v1/quote → delivery_method_id
2. Criar pedido: POST /api/v1/shipment_order → order_number
3. (Opcional) Marcar pronto: POST /shipment_order/multi/ready_for_shipment/with_date
4. Etiqueta: GET /shipment_order/get_label/{order_number}/1
```

### Webhook de Rastreamento

**Configuração:** cadastrada no painel Intelipost — URL HTTPS pública.

**Payload:**
```json
{
  "order_number": "PEDIDO-001",
  "tracking_code": "BR123456789BR",
  "history": {
    "shipment_order_volume_state": "IN_TRANSIT",
    "shipment_volume_micro_state": {
      "default_name": "Em trânsito para unidade de entrega"
    }
  }
}
```

**Status:** `NEW` → `READY_FOR_SHIPPING` → `SHIPPED` → `IN_TRANSIT` → `TO_BE_DELIVERED` → `DELIVERED` / `DELIVERY_FAILED`

**Validação:** header `api-key` no request — comparar com API Key configurada localmente (sem HMAC).

**ID de idempotência:** `order_number`.

### Rate Limits

Não documentados. Throttling recomendado: 5–10 req/s com retry exponencial.

---

## Comparativo Rápido

| Aspecto | Melhor Envio | Intelipost |
|---------|-------------|------------|
| Autenticação | OAuth2 (Authorization Code + refresh) | API Key estática |
| Complexidade de auth | Alta (requer fluxo de usuário) | Baixa (uma chave) |
| Fluxo de etiqueta | 7 passos | 4 passos |
| Sandbox | URL separada pública (15 min auto-avanço) | Ambiente controlado por booleano |
| SDK .NET oficial | Não | Sim (arquivado 2022, usável como referência) |
| Webhook validação | HMAC-SHA256 (`X-ME-Signature`) | Comparação direta de API Key |
| ID único evento | `data.id` (UUID) | `order_number` |
| Modelo de precificação | Saldo em carteira pré-pago | Cobrança via transportadora |

## Notas para o Adapter .NET

1. **Melhor Envio OAuth2:** implementar `AuthorizationCodeHandler` com persistência de `refresh_token` e renovação proativa (renovar antes de expirar aos 40 dias).
2. **Melhor Envio webhook:** validar `X-ME-Signature` = HMAC-SHA256(body, client_secret).
3. **Intelipost:** SDK .NET oficial em [github.com/intelipost/sdk-dotnet](https://github.com/intelipost/sdk-dotnet) — usar como referência de modelos mesmo sendo arquivado.
4. **NF-e:** Intelipost aceita NF-e no body do pedido ou em chamada posterior via `/set_invoice`. Enviar na criação quando disponível.
5. **Rastreamento Melhor Envio:** prefer webhooks; polling via `POST /shipment/tracking` como fallback.
