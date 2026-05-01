# Conferencia Com O PDF DDE + ERS

Fonte conferida:

`Rail-Factory/docs/DDE + ERS Rail Factory PDSOB_PDSCOB 2026 - AESA-CESA-1.pdf`

## 1. Resultado

A documentacao do fork foi alinhada ao PDF.

Estado atual:

- `REQUISITOS.md` usa a numeracao RF/NF/RN do PDF.
- `RF-33` foi preservado como buraco de numeracao, porque o PDF pula de `RF-32` para `RF-34`.
- Itens do DDE que nao eram RF formal viraram requisitos derivados `RD-*`.
- Riscos arquiteturais encontrados no projeto original tambem viraram `RD-*` quando afetam a reconstrucao do fork.
- A matriz de passadas usa os IDs canonicos do PDF e os `RD-*`.

## 2. Cobertura Dos Requisitos Formais

### IAM

| PDF | Requisito | Situacao |
|---|---|---|
| RF-01 | Autenticacao SSO Google | Coberto |
| RF-02 | Provisionamento de Tenants | Coberto |
| RF-03 | RBAC granular por recurso | Coberto |
| RF-04 | Gestao de sessoes | Coberto |
| RF-05 | Trilhas de auditoria | Coberto |
| RF-06 | API key management | Coberto |
| RF-07 | Recuperacao de conta e MFA | Coberto |

### Production

| PDF | Requisito | Situacao |
|---|---|---|
| RF-08 | Versionamento de BOM | Coberto |
| RF-09 | Gestao de Work Centers | Coberto |
| RF-10 | Ciclo de vida da OP | Coberto |
| RF-11 | Reserva automatica de materiais | Coberto |
| RF-12 | Registro de refugo/scrap | Coberto |
| RF-13 | Apontamento de parada | Coberto |
| RF-14 | Controle de qualidade por etapa | Coberto |
| RF-15 | Lote e rastreabilidade | Coberto |

### Supply Chain

| PDF | Requisito | Situacao |
|---|---|---|
| RF-16 | Monitoramento de XML SEFAZ | Coberto como NF-e via provider PlugNotas/SEFAZ |
| RF-17 | Conferencia cega | Coberto |
| RF-18 | Gestao de devolucoes | Coberto |

### Logistics

| PDF | Requisito | Situacao |
|---|---|---|
| RF-19 | Picking e packing | Coberto |
| RF-20 | Gestao de transportadoras | Coberto |
| RF-21 | Rastreio de entrega/tracking | Coberto |
| RF-22 | Webhooks de status | Coberto |
| RF-23 | Conferencia de embarque | Coberto |
| RF-24 | Calculo de frete | Coberto |

### Fleet

| PDF | Requisito | Situacao |
|---|---|---|
| RF-25 | Prontuario do veiculo | Coberto |
| RF-26 | Plano de manutencao | Coberto |
| RF-27 | Controle de abastecimento | Coberto |
| RF-28 | Alocacao de motoristas | Coberto |
| RF-29 | Roteirizacao inteligente | Coberto |
| RF-30 | Telemetria basica | Coberto |

### HR

| PDF | Requisito | Situacao |
|---|---|---|
| RF-31 | Cadastro de pessoas | Coberto |
| RF-32 | Matriz de competencias | Coberto |
| RF-33 | Nao definido no PDF | Preservado, nao reutilizado |

### Dashboard E Reporting

| PDF | Requisito | Situacao |
|---|---|---|
| RF-34 | Calculo de OEE | Coberto |
| RF-35 | Mapas de calor de entrega | Coberto |
| RF-36 | Alertas em tempo real | Coberto |
| RF-37 | Exportacao multiformato | Coberto |
| RF-38 | Dashboard de custos | Coberto |

### Nao Funcionais E Regras

| Grupo | IDs | Situacao |
|---|---|---|
| NF | NF-01 a NF-07 | Cobertos |
| RN | RN-01 a RN-08 | Cobertos |

## 3. Itens Do DDE Tratados Como Derivados

| Item do DDE | ID criado |
|---|---|
| Tenant resolver/database-per-tenant por requisicao | RD-IAM-01 |
| Estoque como fronteira propria de saldo | RD-INV-01 |
| Movimentacoes/ledger de estoque | RD-INV-02 |
| Visao global de inventario para matriz | RD-INV-03 |
| Entrada manual/upload XML como fallback | RD-SUP-01 |
| Provedor substituivel para NF-e | RD-SUP-02 |
| API B2B de consulta de status | RD-LOG-01 |
| Dimensoes de produto | RD-PRD-01 |
| Capacidade de carga do veiculo | RD-FLE-01 |
| Apontamento de horas por usuario/tenant | RD-HR-01 |
| Integracao com software contabil | RD-HR-02 |
| Escalas e turnos vindos de docs posteriores | RD-HR-03 |
| Eventos de dominio via RabbitMQ | RD-TEC-01 |
| Responsabilidades da borda BFF/Gateway | RD-EDGE-01 |
| Politica de falha de auditoria | RD-AUD-01 |
| Interface responsiva | RD-UI-01 |
| Manual, documentacao tecnica e deploy | RD-DOC-01 |

## 4. Divergencia De Stack

O PDF cita a stack planejada antiga:

- C# / .NET 8
- Blazor
- PostgreSQL
- Redis
- RabbitMQ
- Docker

O projeto original, no codigo atual, usa:

- C# / .NET 10
- .NET Aspire AppHost 13.2.4
- React 19.1.x + Vite 7.1.x no frontend
- BFF .NET em `RailFactory.Frontend`
- Gateway com YARP
- PostgreSQL
- Redis
- RabbitMQ
- MassTransit em Production
- OpenTelemetry/health checks via `RailFactory.ServiceDefaults`

Essa diferenca nao e um requisito faltando. O fork deve seguir o que o projeto original realmente usa no codigo, nao a stack antiga escrita no PDF.

## 5. Conclusao

Nao ha grande requisito formal do PDF faltando.

As correcoes arquiteturais mais importantes para o fork sao:

- Inventory deve ser bounded context proprio desde P2.
- BFF e Gateway devem ter responsabilidades separadas.
- Eventos, Outbox e jobs devem carregar tenant explicitamente.
- Auditoria deve ter politica de falha por tipo de acao.

O cuidado principal agora e manter a disciplina de rastreabilidade:

- usar `RF-01` a `RF-38` conforme o PDF;
- nao reutilizar `RF-33`;
- usar `RD-*` para escopo derivado;
- atualizar as passadas quando um requisito mudar de prioridade.
