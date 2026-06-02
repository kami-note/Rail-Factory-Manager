# Plugins Pagamento: Asaas + Iugu

**Categoria:** `payment`
**Última Atualização:** 2026-06-01
**Microsserviço:** Consumer RabbitMQ (`logistics.shipment_dispatched`)

---

## 1. Asaas

### Autenticação

| Item | Valor |
|------|-------|
| Header | `access_token: <chave>` |
| Header adicional obrigatório | `User-Agent: <nome-da-aplicacao>` (obrigatório para contas criadas após 13/06/2024) |
| Prefixo sandbox | `$aact_hmlg_...` |
| Prefixo produção | `$aact_prod_...` |

> Chaves são distintas entre ambientes — uma chave de produção não funciona no sandbox.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Produção | `https://api.asaas.com/v3` |
| Sandbox | `https://sandbox.asaas.com/api/v3` |

### Endpoints Principais

| Operação | Método | Path |
|----------|--------|------|
| Criar cobrança | `POST` | `/v3/payments` |
| Obter QR Code Pix | `GET` | `/v3/payments/{id}/pixQrCode` |
| Consultar status | `GET` | `/v3/payments/{id}/status` |
| Buscar cobrança | `GET` | `/v3/payments/{id}` |
| Cancelar cobrança | `DELETE` | `/v3/payments/{id}` |
| Listar cobranças | `GET` | `/v3/payments` |
| Confirmar pagamento (sandbox) | `POST` | `/v3/payments/{id}/receiveInCash` |

### Campos Mínimos — Cobrança Pix

```json
{
  "customer": "cus_000005219613",
  "billingType": "PIX",
  "value": 100.90,
  "dueDate": "2025-08-01"
}
```

Após criar, chamar `GET /v3/payments/{id}/pixQrCode` para obter `encodedImage`, `payload` e `expirationDate`.

**Campos opcionais relevantes para B2B:** `externalReference` (chave de correlação), `description`, `daysAfterDueDateToRegistrationCancellation`, `split`.

### Webhook Inbound

**Payload:**
```json
{
  "id": "evt_05b708f961d739ea7eba7e4db318f621&368604920",
  "event": "PAYMENT_RECEIVED",
  "dateCreated": "2024-06-12 16:45:03",
  "payment": {
    "id": "pay_...",
    "customer": "cus_...",
    "value": 100.90,
    "billingType": "PIX",
    "status": "RECEIVED"
  }
}
```

| Evento | Significado |
|--------|-------------|
| `PAYMENT_CREATED` | Cobrança criada |
| `PAYMENT_CONFIRMED` | Boleto confirmado (aguarda compensação) |
| `PAYMENT_RECEIVED` | Pagamento compensado (Pix: imediato) |
| `PAYMENT_OVERDUE` | Vencida |
| `PAYMENT_DELETED` | Removida |
| `PAYMENT_REFUNDED` | Estornada |

**Fluxo Pix:** `PAYMENT_CREATED` → `PAYMENT_RECEIVED` (sem `CONFIRMED`)

**Validação:** header `asaas-access-token` com token configurado no webhook (sem HMAC). Comparar com `CryptographicOperations.FixedTimeEquals`. IP allowlist recomendado.

**ID de idempotência:** campo `id` (prefixo `evt_`).

### Rate Limits

| Parâmetro | Valor |
|-----------|-------|
| Quota | 25.000 req / 12 horas por conta |
| GETs simultâneos | 50 |
| Resposta ao exceder | HTTP 429 |
| Headers | `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` |

---

## 2. Iugu

### Autenticação

| Item | Valor |
|------|-------|
| Mecanismo | HTTP Basic Auth |
| Header | `Authorization: Basic <base64(api_token:)>` |
| Sandbox | Usar `test_api_token` (mesmo host de produção) |
| Produção | Usar `live_api_token` |

> O `api_token` é visível por **apenas 1 hora** após criação — armazenar imediatamente.
> Endpoints sensíveis (transferências, saques) requerem assinatura RSA adicional.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Produção e Sandbox | `https://api.iugu.com/v1` |

> Ambiente sandbox = mesmo host, diferenciado pelo tipo de token (`test_api_token`).

### Endpoints Principais

| Operação | Método | Path |
|----------|--------|------|
| Criar fatura | `POST` | `/v1/invoices` |
| Consultar fatura | `GET` | `/v1/invoices/{id}` |
| Cancelar fatura | `PUT` | `/v1/invoices/{id}/cancel` |
| Listar faturas | `GET` | `/v1/invoices` |
| Listar webhooks suportados | `GET` | `/v1/web_hooks/supported_events` |
| Registrar webhook | `POST` | `/v1/web_hooks` |

### Campos Mínimos — Cobrança Pix

```json
{
  "email": "pagador@empresa.com",
  "due_date": "2025-08-01",
  "payable_with": "pix",
  "items": [{
    "description": "Serviço B2B",
    "quantity": 1,
    "price_cents": 10090
  }],
  "payer": {
    "name": "Empresa Pagadora LTDA",
    "cpf_cnpj": "12.345.678/0001-99",
    "email": "financeiro@empresa.com",
    "address": {
      "zip_code": "01310-100", "street": "Av. Paulista",
      "number": "1000", "city": "São Paulo",
      "state": "SP", "country": "BR"
    }
  }
}
```

QR Code Pix retornado diretamente no `GET /v1/invoices/{id}` — sem endpoint separado.

### Webhook Inbound

> **Atenção:** body chega como `application/x-www-form-urlencoded`, não JSON.

```
event=invoice.status_changed&data[status]=paid&data[id]=C34C8435CE0A4F79BFE5020C9A7BE2F3
```

| Evento | Significado |
|--------|-------------|
| `invoice.status_changed` | Status alterado (pagamento, cancelamento, expiração) |
| `invoice.released` | Valor liberado para uso |
| `invoice.created` | Fatura criada |
| `invoice.refunded` | Estorno |

**Validação:** header `Authorization: Basic <base64>` com valor configurado ao criar o webhook. IP de origem fixo: `98.82.243.132`.

**ID de idempotência:** par `event` + `data.id` (sem `event_id` dedicado).

**Retry automático** em falha (resposta != 200). Reenvio manual disponível para logs com até 3 dias.

### Rate Limits

Não documentados publicamente. Paginação: `limit` máximo de 100 por request.

---

## Comparativo Rápido

| Aspecto | Asaas | Iugu |
|---------|-------|------|
| Auth header | `access_token: <chave>` | `Authorization: Basic <base64(token:)>` |
| Sandbox | Host separado + chave `hmlg` | Mesmo host, `test_api_token` |
| Criar cobrança | `POST /v3/payments` | `POST /v1/invoices` |
| Pix QR Code | Endpoint separado | Retornado no GET da fatura |
| Cancelar | `DELETE /v3/payments/{id}` | `PUT /v1/invoices/{id}/cancel` |
| Webhook Content-Type | `application/json` | `application/x-www-form-urlencoded` |
| Webhook auth | Header `asaas-access-token` | Header `Authorization: Basic` |
| Event ID único | Campo `id` (prefixo `evt_`) | Par `event` + `data.id` |
| Rate limit | 25k req/12h; 50 GETs simultâneos | Não documentado |

## Notas para o Adapter .NET

1. **Asaas webhook:** ler `asaas-access-token` de `IHeaderDictionary`, comparar com `CryptographicOperations.FixedTimeEquals`.
2. **Iugu webhook:** `[FromForm]` ou `Request.Form["event"]`; verificar header `Authorization`.
3. **Cancelamento Iugu:** `PUT /v1/invoices/{id}/cancel` retorna erro se já paga — tratar `already_paid` no adapter.
4. **Correlação B2B:** `externalReference` (Asaas) e `order_id` (Iugu) como chaves de rastreamento interno.
5. **Idempotência inbound:** campo `id` (Asaas) ou par `event`+`data.id` (Iugu) na tabela `InboundWebhookEvents`.
