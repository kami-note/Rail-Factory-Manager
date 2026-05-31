# Requisitos Do Rail-Factory Fork

Este documento usa o PDF `DDE + ERS Rail Factory PDSOB_PDSCOB 2026 - AESA-CESA-1.pdf` como fonte canonica para RF, NF e RN.

Regras deste documento:

- IDs `RF-`, `NF-` e `RN-` seguem a numeracao do PDF.
- O PDF nao define `RF-33`; esse buraco deve ser preservado para rastreabilidade.
- Requisitos importantes que aparecem no DDE ou em documentacao posterior, mas nao como RF numerado no PDF, entram como `RD-*` (requisito derivado).
- A ordem de construcao nao fica aqui; ela fica em `ANALISE_REQUISITOS_E_PASSADAS.md`.

**Legenda de status:** ✅ Concluído | 🔄 Parcial | ❌ Pendente | — N/A

---

## 1. Requisitos Funcionais Canonicos Do PDF

### IAM

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-01 | Autenticacao SSO Google | Login via OAuth2 com Google Workspace | ✅ P1 |
| RF-02 | Provisionamento de Tenants | Cadastro de unidades/filiais com dados obrigatorios | ✅ P1 |
| RF-03 | RBAC granular por recurso | Roles e permissoes por recurso | ✅ P1 |
| RF-04 | Gestao de sessoes | Sessao ativa, revogacao e timeout | ✅ P1 |
| RF-05 | Trilhas de auditoria | Registro imutavel de quem/quando/IP/acao | ✅ P10 (IAM: role_assigned/revoked/session_created) |
| RF-06 | API key management | Gerar/revogar chaves para integracoes externas | ❌ P10 |
| RF-07 | Recuperacao de conta e MFA | Recuperacao e segundo fator | ❌ P10 |

### Production

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-08 | Versionamento de BOM | BOM com versao e vigencia | ✅ P4 |
| RF-09 | Gestao de Work Centers | Cadastro de maquinas/linhas/bancadas | ✅ P4 |
| RF-10 | Ciclo de vida da OP | Estados de OP e transicoes validas | ✅ P4/P5 |
| RF-11 | Reserva automatica de materiais | Bloquear insumos ao liberar OP | ✅ P5 |
| RF-12 | Registro de refugo/scrap | Lancar perdas com justificativa | ✅ P5 |
| RF-13 | Apontamento de parada | Registrar interrupcoes de maquina/processo | ✅ P5 |
| RF-14 | Controle de qualidade por etapa | Inspecoes obrigatorias antes de finalizar | ✅ P5 |
| RF-15 | Lote e rastreabilidade | Ligar produto acabado aos insumos consumidos | 🔄 P5 (parcial) |

### Supply Chain

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-16 | Monitoramento de XML/NF-e | Capturar NF-e emitida contra o CNPJ via SEFAZ/provedor | ✅ P2 (upload/manual) |
| RF-17 | Conferencia cega | Conferir sem exibir quantidade da nota | ✅ P3 |
| RF-18 | Gestao de devolucoes | Logistica reversa para defeitos/divergencias | ✅ P3 |

### Logistics

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-19 | Picking e packing | Separacao e embalagem guiadas | ✅ P8 |
| RF-20 | Gestao de transportadoras | Transportadoras, tabelas e prazos | ✅ P8 |
| RF-21 | Rastreio de entrega/tracking | Cliente acompanha status da entrega | ✅ P8/P9 |
| RF-22 | Webhooks de status | Notificar sistemas externos sobre despacho | ✅ P10 |
| RF-23 | Conferencia de embarque | Validar volumes antes da saida | ✅ P8 |
| RF-24 | Calculo de frete | Calcular por peso, cubagem e distancia | ✅ P8 |

### Fleet

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-25 | Prontuario do veiculo | Chassi, placa, Renavam, CRLV e vencimentos | ✅ P7 |
| RF-26 | Plano de manutencao | Preventivas por KM ou tempo | ✅ P8 |
| RF-27 | Controle de abastecimento | Litros, valor, veiculo, motorista e rota | ✅ P8 |
| RF-28 | Alocacao de motoristas | Vinculo motorista/veiculo por janela | ✅ P7 |
| RF-29 | Roteirizacao inteligente | Otimizar multiplas paradas | ❌ P10 |
| RF-30 | Telemetria basica | Ocorrencias de veiculo/motorista | ❌ P10 |

### HR

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-31 | Cadastro de pessoas | Colaboradores, motoristas e terceiros sem acesso ao sistema | ✅ P7 |
| RF-32 | Matriz de competencias | Habilidades tecnicas por pessoa | ❌ P10 |
| RF-33 | Nao definido no PDF | Buraco de numeracao preservado | — N/A |

### Dashboard E Reporting

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| RF-34 | Calculo de OEE | OEE e componentes por periodo/maquina | ✅ P6 |
| RF-35 | Mapas de calor de entrega | Entregas e atrasos em visualizacao geografica | ❌ P10 |
| RF-36 | Alertas em tempo real | Alertas de estoque critico/evento critico | 🔄 P6 (basico) |
| RF-37 | Exportacao multiformato | PDF, Excel e CSV | ❌ P10 |
| RF-38 | Dashboard de custos | Custo de producao vs preco de venda | ❌ P10 |

---

## 2. Requisitos Derivados Do DDE E Da Documentacao Posterior

Estes requisitos nao aparecem como RF numerado no ERS do PDF, mas aparecem no escopo, criterios de aceitacao, arquitetura ou documentacao posterior. Eles devem ser tratados como parte do produto, sem alterar a numeracao canonica do PDF.

| ID | Requisito derivado | Origem | Status |
|---|---|---|---|
| RD-IAM-01 | Tenant resolver por requisicao | ADR/docs posteriores + RN-01 | ✅ P1 |
| RD-INV-01 | Inventory como bounded context de saldo | DDE + regras de producao/inbound + critica arquitetural | ✅ P2/P3 |
| RD-INV-02 | Ledger de movimentacoes de estoque | DDE | ✅ P3/P5 |
| RD-INV-03 | Visao de inventario global para matriz | DDE | ✅ P6 |
| RD-SUP-01 | Entrada manual/upload de XML como fallback | DDE | ✅ P2 |
| RD-SUP-02 | Provedor externo substituivel para NF-e | DDE + fork | ✅ P2 |
| RD-LOG-01 | API B2B de consulta de status | DDE | ✅ P8/P9 |
| RD-PRD-01 | Dimensoes de produto | DDE | ✅ P4/P8 |
| RD-FLE-01 | Capacidade de carga do veiculo | DDE | ✅ P7/P8 |
| RD-HR-01 | Apontamento de horas por usuario/tenant | DDE | ✅ P7 |
| RD-HR-02 | Integracao com software contabil | DDE | ❌ P10 |
| RD-HR-03 | Escalas e turnos | Docs posteriores | ❌ P10 |
| RD-TEC-01 | Eventos de dominio via RabbitMQ | DDE + NF-02 | ✅ P3/P5/P8/P9 |
| RD-EDGE-01 | Responsabilidades da borda BFF/Gateway | Critica arquitetural do fork | ✅ P1 |
| RD-AUD-01 | Politica de falha de auditoria | RF-05, RN-08 + critica arquitetural | ✅ P10 |
| RD-UI-01 | Interface responsiva | DDE | ✅ P1+ |
| RD-DOC-01 | Manual, documentacao tecnica e deploy | DDE | ❌ P10 |

---

## 3. Requisitos Nao Funcionais

| ID | Requisito | Essencia | Status |
|---|---|---|---|
| NF-01 | Resiliencia | Circuit breaker e retry para falhas de servicos | ✅ P1+ |
| NF-02 | Consistencia eventual | Outbox Pattern para evitar perda de eventos | ✅ P3/P5/P8/P9 |
| NF-03 | Observabilidade | Logs, metricas e tracing correlacionaveis | ✅ P0/P1 |
| NF-04 | Performance de API | Consultas abaixo de 500ms no P95 | ❌ P10 |
| NF-05 | Seguranca de dados | Dados sensiveis em repouso e TLS em transito | 🔄 P1 (basico) |
| NF-06 | Escalabilidade horizontal | Servicos com multiplas instancias sem sessao sticky obrigatoria | ✅ P0+ |
| NF-07 | Localizacao e fuso horario | i18n e timezone por filial | 🔄 P1 (basico) |

---

## 4. Regras De Negocio

| ID | Regra | Essencia | Status |
|---|---|---|---|
| RN-01 | Isolamento por tenant | Impedir leitura/escrita cruzada entre unidades | ✅ P1 |
| RN-02 | Autorizacao obrigatoria por permissao | Deny by default em operacoes sensiveis | ✅ P1+ |
| RN-03 | Reserva na liberacao da OP | Reservar insumos e bloquear duplicidade/saldo insuficiente | ✅ P5 |
| RN-04 | Bloqueio por qualidade | OP nao finaliza sem inspecoes aprovadas | ✅ P5 |
| RN-05 | Conferencia cega sem vazamento | Quantidades esperadas so aparecem depois da contagem | ✅ P3 |
| RN-06 | Divergencias inbound exigem tratamento | Material divergente nao fica disponivel sem decisao | ✅ P3 |
| RN-07 | Conferencia de embarque obrigatoria | Saida bloqueada se volume divergir | ✅ P8 |
| RN-08 | Auditoria de acoes sensiveis | Token/API key/permissao precisam de trilha auditavel | ✅ P10 |

---

## 5. Pontos Criticos De Rastreabilidade

- O fork deve usar os IDs do PDF quando se referir ao ERS academico.
- IDs antigos como `RF-SUP-06`, `RF-LOG-01` ou `RF-HRS-03` podem aparecer em docs anteriores, mas nao sao a numeracao canonica deste PDF.
- `RF-33` nao deve ser reaproveitado sem decisao explicita, porque o PDF pulou esse ID.
- Requisitos derivados devem continuar com prefixo `RD-*` para nao confundir com RF formal.

---

## 6. Resumo De Cobertura (P0–P9)

| Categoria | Concluídos | Parciais | Pendentes | Total |
|---|---|---|---|---|
| RF (funcionais) | 21 | 3 | 10 | 34 |
| RD (derivados) | 12 | 2 | 3 | 17 |
| NF (não funcionais) | 3 | 3 | 1 | 7 |
| RN (regras de negócio) | 7 | 1 | — | 8 |
| **Total** | **43** | **9** | **14** | **66** |
