# Plugins HR/Ponto: Ahgora HCM + Control iD

**Categoria:** `hr-ponto`
**Última Atualização:** 2026-06-01
**Microsserviço:** `HumanResources.Api`

---

## 1. Ahgora HCM (TOTVS)

### Autenticação

- **Tipo:** Bearer Token via usuário de serviço provisionado pela TOTVS/Ahgora
- **Header:** `Authorization: Bearer <token>`
- **Obtenção:** Requer criação de usuário de serviço junto à TOTVS — não há fluxo público

### Base URL

| Ambiente | URL |
|----------|-----|
| Produção | `https://api-timesheet.www.ahgora.com.br/` |
| Sandbox | Não disponível publicamente — requer conta de demonstração TOTVS |

### Endpoints Principais

| Recurso | Endpoint | Método |
|---------|----------|--------|
| Listar colaboradores | `/people` | GET |
| Obter marcações por período | `/punches` | GET |
| Registrar marcação manual | `/punches` | POST |
| Jornadas | `/shifts` | GET |
| Escalas | `/schedules` | GET |
| Vínculo colaborador × escala | `/people-schedules` | GET/POST |

> Cada requisição cobre no máximo **30 dias**. Iterar com janela deslizante para períodos maiores.

### Webhooks / Eventos Push

Não disponível na API v17. Arquitetura **pull-only**: consultar `/punches` periodicamente.

### Rate Limits

| Parâmetro | Valor |
|-----------|-------|
| Requisições | 1.000 req/min |
| Janela máxima por consulta | 30 dias |
| Retry recomendado | Backoff 3 s → 5 s → 8 s (3 tentativas) |

### Ambiente de Teste

Não há sandbox público. Acesso completo à especificação v17 requer **contrato com TOTVS/Ahgora**.

### Notas para o Adapter .NET

- Modelar: `AhgoraPunchRecord`, `AhgoraEmployee`, `AhgoraSchedule`
- Implementar paginação com cursor de data (janela de 30 dias)
- `companyId` é parâmetro obrigatório em todas as chamadas
- Implementar circuit breaker + retry com backoff exponencial
- Documentação v17 completa requer acesso via TOTVS antes de iniciar implementação

---

## 2. Control iD (iDAccess / iDFlex / iDFace / iDBlock)

### Autenticação

| Passo | Detalhe |
|-------|---------|
| Endpoint de login | `POST http://<IP>/login.fcgi` |
| Body | `{ "login": "admin", "password": "admin" }` |
| Resposta | `{ "session": "<token>" }` |
| Uso do token | Query string `?session=<token>` ou cookie `session=<token>` |
| Verificação | `GET /session_is_valid.fcgi?session=<token>` |

> Desabilitar `Expect: 100-continue` no `HttpClient` (comportamento padrão do .NET).

### Base URL

| Contexto | URL |
|----------|-----|
| Produção | `http://<IP-DO-DISPOSITIVO>/` |
| Cloud centralizado | **Não existe** — API é embarcada no hardware |
| Sandbox | Não disponível — requer dispositivo físico |

Coleção Postman pública disponível: [documenter.getpostman.com/view/10800185/SztHW4xo](https://documenter.getpostman.com/view/10800185/SztHW4xo)

### Endpoints Principais

#### Colaboradores

| Recurso | Endpoint | Body |
|---------|----------|------|
| Listar usuários | `POST /load_objects.fcgi?session=<s>` | `{ "object": "users" }` |
| Listar com filtro + paginação | `POST /load_objects.fcgi?session=<s>` | `{ "object": "users", "where": { "id": { ">": 0 } }, "limit": 100, "offset": 0 }` |
| Criar colaborador | `POST /create_objects.fcgi?session=<s>` | `{ "object": "users", "values": [...] }` |
| Cadastro remoto biometria | `POST /remote_enroll.fcgi?session=<s>` | `{ "type": "biometry", "user_id": N, "save": true }` |

#### Marcações de Ponto (access_logs)

| Recurso | Endpoint | Body |
|---------|----------|------|
| Listar logs | `POST /load_objects.fcgi?session=<s>` | `{ "object": "access_logs" }` |
| Filtrar por período | `POST /load_objects.fcgi?session=<s>` | `{ "object": "access_logs", "where": { "time": { ">=": <unix_ts>, "<=": <unix_ts> } }, "limit": 500, "offset": 0 }` |
| Relatório de ponto | `POST /report_generate.fcgi?session=<s>` | Parâmetros de período |

> Campo `time` em `access_logs` é **Unix timestamp** — converter para `DateTimeOffset` no adapter.

### Webhooks / Eventos Push em Tempo Real

Três mecanismos disponíveis:

#### Monitor (recomendado)

Configurar via `POST /set_configuration.fcgi`:

```json
{
  "_monitor_": {
    "hostname": "meuservidor.com",
    "port": 8080,
    "path": "api/notifications",
    "request_timeout": 3000,
    "alive_interval": 30000
  }
}
```

O dispositivo envia `POST` para `hostname:port/path` nos eventos:

| Evento | Path | Payload |
|--------|------|---------|
| Novo log de acesso/ponto | `/api/notifications/dao` | `{ "object": "access_logs", "change_type": "inserted", ..., "device_id": N }` |
| Estado de porta | `/api/notifications/door` | `{ "door_id": N, "open": bool, "device_id": N }` |
| Heartbeat | `/api/notifications/device_is_alive` | Contagem de logs pendentes |
| Foto de identificação | `/api/notifications/access_photo` | JPEG base64 |

#### Push Server

Configuração via `_push_server_`: `push_remote_address`, `push_request_period`, `push_request_timeout`.

#### Online Mode (Pro/Enterprise)

Cada identificação é enviada ao servidor em tempo real antes de liberar acesso — o servidor responde com JSON de autorização (event codes 1–15).

### Rate Limits

Não documentados (API local/embarcada). Usar paginação com `limit` + `offset` em `load_objects`.

### Ambiente de Teste

Sem emulador oficial. Opções práticas:
- Dispositivo físico em rede local
- Coleção Postman pública: [documenter.getpostman.com/view/10800185/SztHW4xo](https://documenter.getpostman.com/view/10800185/SztHW4xo)
- Exemplos C# e NodeJS: [github.com/controlid/integracao](https://github.com/controlid/integracao)

### Notas para o Adapter .NET

- `BaseAddress = http://<IP>/` — sem `Expect: 100-continue`
- Gerenciar sessão com renovação automática ao receber sessão inválida
- Para tempo real: implementar endpoint HTTP receptor (ASP.NET minimal API) e configurar `_monitor_` no dispositivo
- Modelar: `ControlIdUser`, `ControlIdAccessLog`, `ControlIdMonitorEvent`
- Paginação obrigatória com `limit` + `offset`

---

## Comparativo Rápido

| Aspecto | Ahgora HCM | Control iD |
|---------|-----------|------------|
| Arquitetura | Cloud REST centralizada | REST embarcada no dispositivo |
| Autenticação | Bearer Token (usuário de serviço) | Session token via POST login |
| Eventos push | Não (pull only) | Sim — Monitor, Push Server, Online Mode |
| Rate limit | 1.000 req/min, janela 30 dias | N/A (local) |
| Sandbox | Não (requer contrato) | Não (requer hardware) |
| Documentação pública | Restrita (requer conta TOTVS) | Totalmente pública |
