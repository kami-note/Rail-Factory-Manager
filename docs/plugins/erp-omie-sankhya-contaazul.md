# Plugins ERP Backoffice: Omie + Sankhya + Conta Azul

**Categoria:** `erp`
**Última Atualização:** 2026-06-01
**Microsserviço:** Consumers RabbitMQ (Cross-Domain)

---

## 1. Omie

### Autenticação

**`app_key` + `app_secret` embutidos no body de cada chamada — sem token separado.**

```json
{
  "app_key": "SUA_APP_KEY",
  "app_secret": "SUA_APP_SECRET",
  "call": "NomeDaCall",
  "param": [{ ... }]
}
```

- Credenciais: Developer Portal → Aplicativos → "Chave de Integração"
- Somente usuários **Administrador** podem gerar/visualizar chaves
- Criar "Aplicativo Teste" gratuito para desenvolvimento

### Base URL

| Ambiente | URL |
|----------|-----|
| Produção | `https://app.omie.com.br/api/v1/` |
| Sandbox | Não existe — usar conta de teste com App Key separado |

> **Somente POST.** Seleção de operação pelo campo `"call"` no body (padrão RPC sobre HTTP).

### Endpoints Principais

| Recurso | Path | Calls |
|---------|------|-------|
| Contas a Receber | `financas/contareceber/` | `IncluirContaReceber`, `AlterarContaReceber`, `ConsultarContaReceber`, `ListarContasReceber`, `ExcluirContaReceber` |
| Baixa de C. Receber | `financas/contareceber/` | `LancarRecebimento` |
| Contas a Pagar | `financas/contapagar/` | `IncluirContaPagar`, `AlterarContaPagar`, `ConsultarContaPagar`, `ListarContasPagar`, `ExcluirContaPagar` |
| Baixa de C. Pagar | `financas/contapagar/` | `LancarPagamento` |
| Extrato | `financas/extrato/` | `ListarMovimentos` |
| Boletos C.R. | `financas/contareceberboleto/` | `GerarBoleto`, `ObterBoleto`, `ProrrogarBoleto`, `CancelarBoleto` |
| PIX | `financas/pix/` | `GerarPix` |

### Campos Mínimos — Criar Conta a Receber

```json
{
  "app_key": "...", "app_secret": "...",
  "call": "IncluirContaReceber",
  "param": [{
    "codigo_lancamento_integracao": "string-unico-seu-sistema",
    "codigo_cliente_fornecedor": 3795249054,
    "data_vencimento": "DD/MM/YYYY",
    "valor_documento": 100.00,
    "codigo_categoria": "1.01.01",
    "data_previsao": "DD/MM/YYYY",
    "id_conta_corrente": 3731356020
  }]
}
```

> `codigo_lancamento_integracao` é a chave de **idempotência** — chamadas repetidas com o mesmo valor fazem update, não duplicam.

Resposta retorna: `codigo_lancamento_omie` (ID interno) + `codigo_lancamento_integracao`.

### Webhooks

Suportados. Configuração no Developer Portal. Entrega com retry FIFO — considera entregue com HTTP 2XX. Após 10 falhas consecutivas, evento pode ser descartado. Sem assinatura HMAC documentada publicamente.

### Rate Limits

| Tipo | Limite |
|------|--------|
| Por IP | 960 req/min |
| Por IP + App Key + Method | 240 req/min |
| Simultaneidade por IP + App Key + Method | 4 req simultâneas (alguns métodos: 1) |
| Consulta duplicada do mesmo ID | Apenas 1ª retorna dados dentro de 60s |
| Bloqueio por abuso | 30 min (HTTP 425) após 10 req incorretas |
| Registros por página | 100 máx. |

---

## 2. Sankhya

### Autenticação

**OAuth 2.0 Client Credentials + header `X-Token` adicional.**

Três credenciais obrigatórias: `client_id`, `client_secret` (portal dev) e `X-Token` (configurações do Gateway no SankhyaOM).

**Fluxo:**
```
POST https://api.sankhya.com.br/gateway/v1/authenticate
  ?client_id=...&client_secret=...
Headers: token: <X-TOKEN>

Resposta: { "bearerToken": "eyJ...", "expires_in": 3600 }
```

O `bearerToken` (JWT, validade **1 hora**) vai em `Authorization: Bearer <token>` em todas as chamadas.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Produção | `https://api.sankhya.com.br/gateway/v1/` |
| Sandbox | `https://api.sandbox.sankhya.com.br/gateway/v1/` |

### Modelo de API — Dualidade

**A) API REST (Gateway v1 — recomendado para novos adapters):**

| Operação | Método | Path |
|----------|--------|------|
| Criar C. Receber | `POST` | `/financeiros/receitas` |
| Listar C. Receber | `GET` | `/financeiros/receitas` |
| Atualizar | `PUT` | `/financeiros/receitas/{id}` |
| Baixar C. Receber | `POST` | `/financeiros/receitas/{id}/baixar` |
| Criar C. Pagar | `POST` | `/financeiros/despesas` |
| Listar C. Pagar | `GET` | `/financeiros/despesas` |
| Baixar C. Pagar | `POST` | `/financeiros/despesas/{id}/baixar` |

**B) API de Serviços (legado XML — `service.sbr`):**

```
POST /gateway/v1/mge/service.sbr?serviceName=CRUDServiceProvider.saveRecord
Content-Type: text/xml;charset=ISO-8859-1
Authorization: Bearer <token>
```

Campos mínimos (XML, entidade `Financeiro` / tabela `TGFFIN`):

```xml
<RECDESP>1</RECDESP>        <!-- 1=Receita, -1=Despesa -->
<CODPARC>1</CODPARC>        <!-- Código do parceiro/cliente -->
<CODTIPOPER>421</CODTIPOPER>
<CODTIPTIT>1</CODTIPTIT>
<CODEMP>1</CODEMP>
<DTNEG>14/10/2025</DTNEG>
<DTVENC>01/12/2025</DTVENC>
<VLRDESDOB>250.25</VLRDESDOB>
<ORIGEM>E</ORIGEM>
<PROVISAO>S</PROVISAO>
```

> `NUFIN` (PK) é auto-gerado — não enviar na criação. Para update: incluir `<key><NUFIN>123</NUFIN></key>`.

### Webhooks / Sincronização

Webhooks nativos **não documentados publicamente**. Modelo recomendado: **polling** com filtro por `DTALTER` (data de alteração) via `CRUDServiceProvider.loadRecords`.

### Rate Limits

Não publicados. Evitar polling < 1 min. Implementar backoff em 429/503.

---

## 3. Conta Azul

### Autenticação

**OAuth 2.0 — Authorization Code Flow** (obrigatório — sem Client Credentials).

| Token | Validade |
|-------|----------|
| `access_token` | 1 hora |
| `refresh_token` | 2 semanas (uso único — ao usar, novo par é emitido) |

**Endpoints OAuth:**

| Ação | URL |
|------|-----|
| Autorização | `https://auth.contaazul.com/login?response_type=code&client_id=...&redirect_uri=...&state=...` |
| Troca de código | `POST https://auth.contaazul.com/oauth2/token` |
| Renovação | `POST https://auth.contaazul.com/oauth2/token` (grant_type=refresh_token) |

> Se `refresh_token` expirar sem ser usado, o usuário deve reautorizar (novo consentimento).

### Base URL

| Ambiente | URL |
|----------|-----|
| Produção | `https://api-v2.contaazul.com/v1/` |
| Sandbox | Não existe. Contas de teste provisionadas por 3 dias (prorrogáveis via `api@contaazul.com`) |

### Endpoints Principais

| Recurso | Método | Path |
|---------|--------|------|
| Criar evento financeiro (C.R./C.P.) | `POST` | `/v1/financeiro/eventos-financeiros` |
| Listar parcelas | `GET` | `/v1/financeiro/eventos-financeiros/{id}/parcelas` |
| Vendas | `POST/GET/PUT` | `/v1/vendas/` |

**Campos mínimos — Criar C. Receber:**

| Campo | Tipo | Notas |
|-------|------|-------|
| `tipo` | enum | `RECEITA` |
| `customer_id` | UUID | Obrigatório para status COMMITTED |
| `data_vencimento` | ISO 8601 date | |
| `valor` | decimal | |
| `financial_account_id` | UUID | Conta de destino |
| `category_id` | UUID | Plano de contas |

**Status de parcela:** `PENDENTE`, `QUITADO`, `CANCELADO`, `RENEGOCIADO`, `RECEBIDO_PARCIAL`, `ATRASADO`, `PERDIDO`.

### Webhooks

**Não suportados.** Modelo obrigatório: **polling** com filtros por `data_vencimento`, `data_pagamento`, `status`.

### Rate Limits

| Tipo | Limite |
|------|--------|
| Por conta conectada | 600 req/min |
| Por segundo | 10 req/s |

---

## Comparativo Rápido

| Critério | Omie | Sankhya | Conta Azul |
|----------|------|---------|------------|
| Autenticação | `app_key`+`app_secret` no body | OAuth2 CC + `X-Token` header | OAuth2 Authorization Code |
| Protocolo | RPC-over-HTTP (JSON, sempre POST) | REST + XML legado | REST JSON |
| Produção URL | `app.omie.com.br/api/v1/` | `api.sankhya.com.br/gateway/v1/` | `api-v2.contaazul.com/v1/` |
| Sandbox | Não (conta de teste) | Sim (`api.sandbox.sankhya.com.br`) | Não (conta de teste 3 dias) |
| Webhooks | Sim (retry FIFO) | Não (polling) | Não (polling) |
| Rate limit | 960/min por IP; 240/min por IP+Key+Method | Não publicado | 600/min; 10 req/s |
| Formato data | `DD/MM/YYYY` | `DD/MM/YYYY` | ISO 8601 |
| Idempotência | `codigo_lancamento_integracao` | PK `NUFIN` | `referencia` / `id` |

## Notas para o Adapter .NET

1. **Interface comum:** `IContabilidadeAdapter` com `CriarContaReceber`, `CriarContaPagar`, `LancarBaixa`, `ConsultarExtrato`.

2. **Omie:**
   - Sempre POST com campo `"call"` identificando a operação
   - `Polly` com retry para HTTP 425 (back-off 30 min) e HTTP 429
   - Pré-requisito: resolver `codigo_cliente_fornecedor`, `codigo_categoria` e `id_conta_corrente` via APIs de listagem antes de criar C.R./C.P.

3. **Sankhya:**
   - Preferir API REST (`/financeiros/receitas`) ao XML legado
   - `X-Token` é segredo adicional ao par OAuth — armazenar junto com `client_id`/`client_secret`
   - `ITokenCache` com renovação proativa aos ~55 min (token expira em 1h)
   - XML legado usa `ISO-8859-1` — atenção ao encoding no `HttpClient`
   - Polling via `DTALTER` para sincronização

4. **Conta Azul:**
   - Authorization Code Flow requer UX de onboarding (redirect + callback)
   - `refresh_token` é single-use — lock distribuído se múltiplas instâncias
   - `IHostedService` de polling com `IMemoryCache` para evitar requests duplicados
   - Conta de teste de 3 dias é suficiente para testes de integração em CI/CD
