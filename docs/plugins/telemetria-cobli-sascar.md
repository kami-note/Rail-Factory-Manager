# Plugins Telemetria: Cobli + Sascar

**Categoria:** `telemetry`
**Última Atualização:** 2026-06-01
**Microsserviço:** `Fleet.Api`
**Nota:** Cobli marcado como **Fase 2** (sem sandbox). Usar `MockTelemetryAdapter` (`ProviderType = "mock"`) para testes.

---

## 1. Cobli

### Autenticação

| Item | Valor |
|------|-------|
| Header | `cobli-api-key: <sua_chave>` |
| Obtenção | Painel Cobli → Integrações → APIs → "Criar chave de API" |
| Visibilidade | Exibida **apenas uma vez** na criação — armazenar imediatamente |

Autenticação stateless por header — sem OAuth, sem refresh.

### Base URL

| Ambiente | URL |
|----------|-----|
| Produção | `https://api.cobli.co/public/v1` |
| Sandbox | Não disponível publicamente |

### Endpoints Principais

#### Veículos

| Operação | Método | Path |
|----------|--------|------|
| Listar veículos | `GET` | `/vehicles` |
| Buscar por ID | `GET` | `/vehicles/{vehicleId}` |
| Criar veículo | `POST` | `/vehicles` |
| Atualizar veículo | `PUT` | `/vehicles/{vehicleId}` |
| Associar a dispositivo | `POST` | `/vehicles/{vehicleId}/device-association` |

> `vehicleId` é **UUID** (string).

#### Hodômetro

| Operação | Método | Path |
|----------|--------|------|
| Buscar hodômetro | `GET` | `/vehicles/{vehicleId}/odometer` |
| Calibrar hodômetro | `POST` | `/vehicles/{vehicleId}/odometer` |

> Requer calibração inicial. Com integração CAN-bus, atualizado em tempo real pelo dispositivo.

#### Posição

A Cobli **não expõe endpoint de "posição atual" por polling REST**. Localização em tempo quase real é entregue via **webhook** (evento `position`). Para histórico:

| Operação | Método | Path |
|----------|--------|------|
| Resumo de trajetos/paradas | `GET` | `/paths/summary` |
| Detalhes de trajetos | `GET` | `/paths` |
| Distância percorrida | `GET` | `/distance-driven` |
| Relatório ponto a ponto | `GET` | `/stats/reports/paths/vehicles` |

#### Histórico de Eventos

| Operação | Método | Path |
|----------|--------|------|
| Eventos de risco | `GET` | `/events/risk` |
| Eventos de velocidade | `GET` | `/events/speed` |
| Histórico de webhooks | `GET` | `/webhook-events` |
| Análise de motor ocioso | `GET` | `/idle-engine` |

> Janela máxima por consulta: **90 dias**. Histórico de webhooks retido por **até 15 dias**.

### Webhooks (23 tipos de eventos)

**Configuração:** Painel → Configurações → Webhooks → URL pública HTTPS + `secret_key`

**Validação:** header `X-Cobli-Signature` = HMAC-SHA256 hex-encoded do body com `secret_key`. Rejeitar payloads sem assinatura válida com HTTP 400.

**Envelope padrão:**
```json
{
  "eventId": "UUID",
  "eventTime": "ISO 8601 UTC",
  "eventType": "position",
  "eventData": {
    "deviceId": "...", "vehicleId": "...", "fleetId": "...",
    "licensePlate": "...", "driverId": "...",
    "latitude": -23.5505, "longitude": -46.6333,
    "heading": 90, "odometer": 125000,
    "ignition": true, "speedInKmh": 60,
    "batteryVoltage": 12.4, "connectionType": "4G"
  }
}
```

| Categoria | Eventos |
|-----------|---------|
| Geofencing | `geofence_in`, `geofence_out` |
| Ignição | `ignition_on`, `ignition_off` |
| Posição | `position`, `position_sleep` |
| Velocidade | `alert_driven_over_speed`, `end_alert_driven_over_speed` |
| Bateria | `battery_external_low`, `battery_external_disconnected`, `battery_external_reconnected` |
| Câmera (11 tipos) | Frenagem, aceleração, curva, colisão, distração, olhos fechados, celular, fumo, bocejo, SOS, tailgating |

**Retry policy:** backoff 2s → 4s → 8s → 15s → 15s (±20% jitter), máx. 5 tentativas. Após 7 dias de falhas contínuas: subscription desabilitada automaticamente.

**Deduplicação:** usar `eventId` (UUID) como chave na tabela `InboundWebhookEvents`.

### Paginação

Cursor-based. Token de cursor expira em **24 horas** (ou 1h após o último item).

### Rate Limits

Não publicados. HTTP 429 suportado — implementar backoff exponencial com `Retry-After`.

---

## 2. Sascar

### Interface 1: SasIntegra (SOAP — principal, documentação pública)

#### Autenticação

- **Tipo:** `usuario` + `senha` por parâmetro SOAP em cada chamada
- Sem token session — autenticação stateless por credenciais
- Operação auxiliar: `atualizarSenha`

#### Base URL e WSDL

| Recurso | URL |
|---------|-----|
| Service endpoint | `https://sasintegra.sascar.com.br:443/SasIntegra/SasIntegraWSService` |
| WSDL | `https://sasintegra.sascar.com.br/SasIntegra/SasIntegraWSService?wsdl` |
| XSD Schema | `https://sasintegra.sascar.com.br/SasIntegra/SasIntegraWSService?xsd=1` |

#### Endpoints Principais (Operações SOAP)

**Veículos:**

| Operação | Descrição |
|----------|-----------|
| `obterVeiculos` | Lista veículos da conta |
| `obterVeiculosJson` | Lista veículos em JSON |
| `verificarVeiculoIntegrado` | Verifica se veículo está integrado |

**Campos do objeto `veiculo`:** `idVeiculo` (int), `placa`, `ccid`, `esn`, `descricao`, `idCliente`, `satelital`, `telemetria`.

> `idVeiculo` é **inteiro** — diferente da Cobli que usa UUID.

**Hodômetro:**

Não há endpoint isolado. O hodômetro é o campo `odometro` dentro do objeto `pacotePosicao` (ver abaixo) e também em `deltaTelemetria`:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `odometro` | `xsd:int` | Leitura do hodômetro |
| `distanciaPercorrida` | `xsd:int` | Distância no período |
| `horimetro` | `xsd:int` | Horas de motor |

**Posição / Localização:**

| Operação | Descrição |
|----------|-----------|
| `obterPacotePosicoes` | Última posição dos veículos |
| `obterPacotePosicoesComPlaca` | Posições filtradas por placa |
| `obterPacotePosicoesJSON` | Posições em JSON |
| `obterEnderecoPosicao` | Geocoding reverso |

**Campos do objeto `pacotePosicao`:**

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `idVeiculo` | `xsd:int` | ID do veículo |
| `placa` | `xsd:string` | Placa |
| `latitude` | `xsd:double` | Coordenada |
| `longitude` | `xsd:double` | Coordenada |
| `dataPosicao` | `xsd:dateTime` | Timestamp |
| `velocidade` | `xsd:int` | km/h |
| `odometro` | `xsd:int` | Hodômetro |
| `direcao` | `xsd:int` | Bearing/heading (graus) |
| `idMotorista` | `xsd:int` | ID do motorista |

**Histórico de Eventos:**

| Operação | Descrição |
|----------|-----------|
| `obterPacotePosicaoHistorico` | Histórico de posições por período |
| `obterEventoTelemetriaIntegracao` | Eventos de telemetria |
| `obterEventosTempoDirecao` | Eventos de velocidade/direção |
| `solicitarEventosCaixaPreta` | Solicitar eventos do black box |
| `recuperarEventosCaixaPreta` | Recuperar eventos do black box |

**Campos do objeto `eventoTelemetria`:** `idEvento` (int), `idVeiculo`, `idMotorista`, `dataPosicao`, `latitude`, `longitude`, `velocidadeMaximaEvento`, `tempoDuracao`, `aceleracaoLateralForcaG`.

#### Webhooks / Push

**Não disponível no SasIntegra.** Arquitetura **polling-only**: consultar `obterPacotePosicoes` e `obterEventoTelemetriaIntegracao` periodicamente.

### Interface 2: API Portal REST (Moderno)

Disponível em `api-portal.sascar.com.br`. Exige **cadastro de desenvolvedor** — não há documentação pública sem registro. Tecnologia: plataforma Sensedia (OAuth 2.0 + JWT provável). Contato comercial necessário para obter credenciais.

### Rate Limits

Não documentados para nenhuma das interfaces.

### Ambiente de Teste

Não identificado publicamente. Sandbox requer contato com Sascar.

---

## Comparativo Rápido

| Aspecto | Cobli | Sascar |
|---------|-------|--------|
| Protocolo | REST/JSON | SOAP/XML (+ REST via cadastro) |
| Autenticação | API Key header `cobli-api-key` | `usuario`+`senha` por parâmetro SOAP |
| Base URL | `https://api.cobli.co/public/v1` | `https://sasintegra.sascar.com.br/...` |
| Hodômetro | `GET /vehicles/{id}/odometer` | Campo `odometro` em `obterPacotePosicoes` |
| Posição atual | Webhook evento `position` | Polling `obterPacotePosicoes` |
| Histórico eventos | `/events/risk`, `/events/speed` (90 dias) | `obterPacotePosicaoHistorico` |
| Webhook | Sim (23 tipos, HMAC-SHA256) | Não (polling apenas) |
| Sandbox | Não documentado | Não documentado |
| ID de veículo | UUID (string) | Inteiro (`xsd:int`) |
| Documentação pública | Completa em `docs.cobli.co` | WSDL/XSD públicos; REST requer cadastro |

## Notas para o Adapter .NET

**Cobli:**
1. Header `cobli-api-key` em todas as requests
2. `vehicleId` é UUID — não inteiro
3. Posição atual: implementar receptor de webhook evento `position`; polling via `/paths` como fallback
4. Janela máxima de histórico: 90 dias com paginação por cursor (TTL 24h)
5. Deduplicação: campo `eventId` na tabela `InboundWebhookEvents`

**Sascar:**
1. Cliente WCF gerado com `dotnet-svcutil` a partir do WSDL em `?wsdl`
2. `usuario`/`senha` por parâmetro SOAP — sem token a gerenciar
3. Hodômetro: extrair campo `odometro` do tipo `pacotePosicao`
4. `idVeiculo` é inteiro — não UUID
5. Várias operações possuem variante `*JSON` que simplifica deserialização
6. Para REST moderno: contato com Sascar para credenciais de desenvolvedor
