# Plugins Fiscal: PlugNotas + Focus NFe

**Categoria:** `fiscal`
**Última Atualização:** 2026-06-01
**Microsserviço:** `Logistics.Api` (emissão outbound) · `SupplyChain.Api` (captura inbound)

---

## 1. PlugNotas (TecnoSpeed)

### Autenticação

| Item | Valor |
|------|-------|
| Mecanismo | API Key via header HTTP |
| Header | `x-api-key: <token>` |
| Token sandbox público | `2da392a6-79d2-4304-a8b7-959572c7e44d` |
| Token de produção | Painel GUI → avatar → "Token" |

> Adapter .NET: adicionar `x-api-key` em `HttpClient.DefaultRequestHeaders`. Injetar via `IOptions<PlugNotasOptions>`.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Sandbox | `https://api.sandbox.plugnotas.com.br` |
| Produção | `https://api.plugnotas.com.br` |

> Sandbox é mock completo. **Webhook não funciona em sandbox** — testar callbacks apenas em produção/homologação.

### Endpoints Principais — NF-e

| Operação | Método | Path |
|----------|--------|------|
| Emissão | `POST` | `/nfe` |
| Consulta por ID | `GET` | `/nfe/{idNota}` |
| Consulta por chave de acesso | `GET` | `/nfe/{chaveAcesso}` |
| Cancelamento | `POST` | `/nfe/{idNota}/cancelamento` |
| Download PDF (DANFE) | `GET` | `/nfe/{idNota}/pdf` |
| Download XML | `GET` | `/nfe/{idNota}/xml` |
| Download XML Cancelamento | `GET` | `/nfe/{idNota}/xml/cancelamento` |

Emissão é **assíncrona**: `POST /nfe` retorna `202 Accepted` com `idNota`. Status final chega via webhook ou polling em `GET /nfe/{idNota}`.

### CT-e e MDF-e

| Documento | Emissão | Consulta | Cancelamento |
|-----------|---------|----------|--------------|
| CT-e | `POST /cte` | `GET /cte/{id}` | `POST /cte/{id}/cancelamento` |
| MDF-e | `POST /mdfe` | `GET /mdfe/{id}` | `POST /mdfe/{id}/cancelamento` |

### Webhook Inbound

**Disparo:** quando a nota atinge `CONCLUIDO`, `REJEITADO`, `CANCELADO` ou `DENEGADO`.

**Payload:**
```json
{
  "id": "5f9ad47eff3b4d0d7b4994ea",
  "idIntegracao": "XXX999",
  "emissao": "29/10/2020",
  "status": "CONCLUIDO",
  "emitente": "29062609000177",
  "destinatario": "08114280956",
  "valor": 9.20,
  "numero": "1000013",
  "serie": "805",
  "chave": "41201029062609000177558050010000131609769080",
  "protocolo": "141200000956123",
  "dataAutorizacao": "29/10/2020",
  "mensagem": "Autorizado o uso da NF-e",
  "cStat": 100,
  "pdf": "https://api.plugnotas.com.br/nfe/5f9ad47eff3b4d0d7b4994ea/pdf",
  "xml": "https://api.plugnotas.com.br/nfe/5f9ad47eff3b4d0d7b4994ea/xml"
}
```

**Validação:** sem HMAC documentado. Usar HTTPS + IP allowlist. `idIntegracao` é a chave de correlação e idempotência.

### Rate Limits

HTTP 429 quando excedido. Implementar `Polly` com `WaitAndRetryAsync` baseado em `Retry-After`, fallback: 2s → 4s → 8s.

### Campos Mínimos — Emissão NF-e

```json
[{
  "idIntegracao": "seu-id-unico-123",
  "emitente": { "cpfCnpj": "29062609000177" },
  "destinatario": {
    "cpfCnpj": "08114280956",
    "nome": "CLIENTE TESTE",
    "endereco": {
      "logradouro": "Rua Exemplo", "numero": "100",
      "bairro": "Centro", "municipio": "São Paulo",
      "uf": "SP", "cep": "01001000"
    }
  },
  "naturezaOperacao": "VENDA DE MERCADORIA",
  "finalidadeEmissao": 1,
  "consumidorFinal": true,
  "produtos": [{
    "codigo": "PROD001",
    "descricao": "Produto de Teste",
    "ncm": "84713012", "cfop": "5102",
    "unidade": "UN", "quantidade": 1, "valorUnitario": 100.00,
    "icms": { "origem": 0, "cst": "40" },
    "pis": { "cst": "07" }, "cofins": { "cst": "07" }
  }]
}]
```

---

## 2. Focus NFe (Acras Tecnologia)

### Autenticação

| Item | Valor |
|------|-------|
| Mecanismo | HTTP Basic Auth |
| Username | Token da API |
| Password | String vazia `""` |
| Header | `Authorization: Basic <base64(token:)>` |

> .NET: `new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(token + ":")))`
>
> Tokens vêm em pares — um para homologação, outro para produção.

### Base URLs

| Ambiente | URL |
|----------|-----|
| Homologação | `https://homologacao.focusnfe.com.br` |
| Produção | `https://api.focusnfe.com.br` |

### Endpoints Principais — NF-e

| Operação | Método | Path |
|----------|--------|------|
| Emissão | `POST` | `/v2/nfe?ref={ref}` |
| Consulta | `GET` | `/v2/nfe/{ref}` |
| Consulta completa (com XML) | `GET` | `/v2/nfe/{ref}?completa=1` |
| Cancelamento | `DELETE` | `/v2/nfe/{ref}` |
| Download DANFE | `GET` | `/v2/nfe/{ref}.pdf` *(302 redirect)* |
| Download XML | `GET` | `/v2/nfe/{ref}.xml` *(302 redirect)* |

> `ref` = identificador único no **seu sistema** (até 255 chars). É a chave de correlação.
>
> Download retorna `302 Found`. Em .NET: `AllowAutoRedirect = false`, capturar `Location` header e fazer segundo GET **sem** o header `Authorization`.

### CT-e e MDF-e

| Documento | Emissão | Consulta | Cancelamento |
|-----------|---------|----------|--------------|
| CT-e | `POST /v2/cte?ref={ref}` | `GET /v2/cte/{ref}` | `DELETE /v2/cte/{ref}` |
| MDF-e | `POST /v2/mdfe?ref={ref}` | `GET /v2/mdfe/{ref}` | `DELETE /v2/mdfe/{ref}` |
| MDF-e Encerramento | `POST /v2/mdfe/{ref}/encerramento` | — | — |

### Status da Nota

`processando_autorizacao` → `autorizado` / `erro_autorizacao` / `cancelado` / `denegado`

### Webhook Inbound

**Configuração:** parâmetro `url_notificacao` no body da emissão, ou globalmente no painel.

**Payload:**
```json
{
  "ref": "seu-ref-unico",
  "cnpj_emitente": "12345678000195",
  "status": "autorizado",
  "status_sefaz": "100",
  "mensagem_sefaz": "Autorizado o uso da NF-e",
  "chave_nfe": "35230112345678000195550010000000011234567890",
  "numero": "1", "serie": "1",
  "caminho_xml_nota_fiscal": "/arquivos/.../nfe.xml",
  "caminho_danfe": "/arquivos/.../danfe.pdf",
  "caminho_xml_cancelamento": null
}
```

**Validação:** sem HMAC. Incluir token secreto na URL do callback (`?secret=valor`). Correlacionar por `cnpj_emitente` + `ref`.

### Rate Limits

HTTP 429 ao exceder. Não fazer polling mais frequente que 1 req/5s por nota. Paginação: header `X-Total-Count`, 50–100 registros por página.

### Campos Mínimos — Emissão NF-e

```json
{
  "natureza_operacao": "VENDA DE MERCADORIA",
  "data_emissao": "2026-06-01T10:00:00-03:00",
  "tipo_documento": 1, "finalidade_emissao": 1,
  "consumidor_final": 1, "presenca_comprador": 1,
  "modalidade_frete": 9, "local_destino": 1,
  "cnpj_emitente": "12345678000195",
  "inscricao_estadual_emitente": "1234567890",
  "nome_destinatario": "CLIENTE TESTE",
  "cnpj_destinatario": "98765432000100",
  "logradouro_destinatario": "Rua Teste",
  "numero_destinatario": "100",
  "bairro_destinatario": "Centro",
  "municipio_destinatario": "São Paulo",
  "uf_destinatario": "SP",
  "indicador_inscricao_estadual_destinatario": 9,
  "items": [{
    "numero_item": 1,
    "codigo_produto": "PROD001",
    "descricao": "Produto de Teste",
    "codigo_ncm": "84713012", "cfop": "5102",
    "unidade_comercial": "UN",
    "quantidade_comercial": 1.0,
    "valor_unitario_comercial": 100.00,
    "icms_origem": 0, "icms_situacao_tributaria": "40",
    "pis_situacao_tributaria": "07",
    "cofins_situacao_tributaria": "07"
  }]
}
```

---

## Comparativo Rápido

| Aspecto | PlugNotas | Focus NFe |
|---------|-----------|-----------|
| Auth | `x-api-key` header | HTTP Basic Auth |
| Sandbox público | Sim (token fixo) | Não (cadastro necessário) |
| ID da nota | Gerado pelo PlugNotas | `ref` definido pelo cliente |
| Cancelamento | `POST /nfe/{id}/cancelamento` | `DELETE /v2/nfe/{ref}` |
| Download PDF | Direto | 302 redirect |
| Webhook sandbox | Não funciona | Disponível (limitado) |
| Assinatura webhook | Não documentada | Não implementada |

## Notas para o Adapter .NET

1. Interface comum: `IFiscalProviderAdapter` com `EmitirAsync`, `ConsultarAsync`, `CancelarAsync`, `DownloadPdfAsync`, `DownloadXmlAsync`, `ProcessarWebhookAsync`.
2. `IHttpClientFactory` com clientes nomeados por provider, `BaseAddress` e headers padrão em `DelegatingHandler`.
3. Retry: `Polly.Extensions.Http` com `WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))` em 429/503.
4. Download Focus NFe: `AllowAutoRedirect = false` + captura do `Location` header + segundo GET sem `Authorization`.
5. Correlação de webhook: armazenar `idIntegracao` (PlugNotas) e `ref` (Focus NFe) como chave na tabela `InboundWebhookEvents`.
