# Requisitos Do Rail-Factory Fork

Este documento usa o PDF `DDE + ERS Rail Factory PDSOB_PDSCOB 2026 - AESA-CESA-1.pdf` como fonte canonica para RF, NF e RN.

Regras deste documento:

- IDs `RF-`, `NF-` e `RN-` seguem a numeracao do PDF.
- O PDF nao define `RF-33`; esse buraco deve ser preservado para rastreabilidade.
- Requisitos importantes que aparecem no DDE ou em documentacao posterior, mas nao como RF numerado no PDF, entram como `RD-*` (requisito derivado).
- A ordem de construcao nao fica aqui; ela fica em `ANALISE_REQUISITOS_E_PASSADAS.md`.

## 1. Requisitos Funcionais Canonicos Do PDF

### IAM

| ID | Requisito | Essencia |
|---|---|---|
| RF-01 | Autenticacao SSO Google | Login via OAuth2 com Google Workspace |
| RF-02 | Provisionamento de Tenants | Cadastro de unidades/filiais com dados obrigatorios |
| RF-03 | RBAC granular por recurso | Roles e permissoes por recurso |
| RF-04 | Gestao de sessoes | Sessao ativa, revogacao e timeout |
| RF-05 | Trilhas de auditoria | Registro imutavel de quem/quando/IP/acao |
| RF-06 | API key management | Gerar/revogar chaves para integracoes externas |
| RF-07 | Recuperacao de conta e MFA | Recuperacao e segundo fator |

### Production

| ID | Requisito | Essencia |
|---|---|---|
| RF-08 | Versionamento de BOM | BOM com versao e vigencia |
| RF-09 | Gestao de Work Centers | Cadastro de maquinas/linhas/bancadas |
| RF-10 | Ciclo de vida da OP | Estados de OP e transicoes validas |
| RF-11 | Reserva automatica de materiais | Bloquear insumos ao liberar OP |
| RF-12 | Registro de refugo/scrap | Lancar perdas com justificativa |
| RF-13 | Apontamento de parada | Registrar interrupcoes de maquina/processo |
| RF-14 | Controle de qualidade por etapa | Inspecoes obrigatorias antes de finalizar |
| RF-15 | Lote e rastreabilidade | Ligar produto acabado aos insumos consumidos |

### Supply Chain

| ID | Requisito | Essencia |
|---|---|---|
| RF-16 | Monitoramento de XML/NF-e | Capturar NF-e emitida contra o CNPJ via SEFAZ/provedor |
| RF-17 | Conferencia cega | Conferir sem exibir quantidade da nota |
| RF-18 | Gestao de devolucoes | Logistica reversa para defeitos/divergencias |

### Logistics

| ID | Requisito | Essencia |
|---|---|---|
| RF-19 | Picking e packing | Separacao e embalagem guiadas |
| RF-20 | Gestao de transportadoras | Transportadoras, tabelas e prazos |
| RF-21 | Rastreio de entrega/tracking | Cliente acompanha status da entrega |
| RF-22 | Webhooks de status | Notificar sistemas externos sobre despacho |
| RF-23 | Conferencia de embarque | Validar volumes antes da saida |
| RF-24 | Calculo de frete | Calcular por peso, cubagem e distancia |

### Fleet

| ID | Requisito | Essencia |
|---|---|---|
| RF-25 | Prontuario do veiculo | Chassi, placa, Renavam, CRLV e vencimentos |
| RF-26 | Plano de manutencao | Preventivas por KM ou tempo |
| RF-27 | Controle de abastecimento | Litros, valor, veiculo, motorista e rota |
| RF-28 | Alocacao de motoristas | Vinculo motorista/veiculo por janela |
| RF-29 | Roteirizacao inteligente | Otimizar multiplas paradas |
| RF-30 | Telemetria basica | Ocorrencias de veiculo/motorista |

### HR

| ID | Requisito | Essencia |
|---|---|---|
| RF-31 | Cadastro de pessoas | Colaboradores, motoristas e terceiros sem acesso ao sistema |
| RF-32 | Matriz de competencias | Habilidades tecnicas por pessoa |
| RF-33 | Nao definido no PDF | Buraco de numeracao preservado |

### Dashboard E Reporting

| ID | Requisito | Essencia |
|---|---|---|
| RF-34 | Calculo de OEE | OEE e componentes por periodo/maquina |
| RF-35 | Mapas de calor de entrega | Entregas e atrasos em visualizacao geografica |
| RF-36 | Alertas em tempo real | Alertas de estoque critico/evento critico |
| RF-37 | Exportacao multiformato | PDF, Excel e CSV |
| RF-38 | Dashboard de custos | Custo de producao vs preco de venda |

## 2. Requisitos Derivados Do DDE E Da Documentacao Posterior

Estes requisitos nao aparecem como RF numerado no ERS do PDF, mas aparecem no escopo, criterios de aceitacao, arquitetura ou documentacao posterior. Eles devem ser tratados como parte do produto, sem alterar a numeracao canonica do PDF.

| ID | Requisito derivado | Origem | Motivo |
|---|---|---|---|
| RD-IAM-01 | Tenant resolver por requisicao | ADR/docs posteriores + RN-01 | Necessario para database-per-tenant e isolamento real |
| RD-INV-01 | Inventory como bounded context de saldo | DDE + regras de producao/inbound + critica arquitetural | Evita duplicar estoque em Supply e Production |
| RD-INV-02 | Ledger de movimentacoes de estoque | DDE | Entrada, saida, reserva, consumo e auditoria operacional |
| RD-INV-03 | Visao de inventario global para matriz | DDE | Admin Matriz precisa enxergar consolidado entre filiais |
| RD-SUP-01 | Entrada manual/upload de XML como fallback | DDE | Operacao nao pode parar se API externa falhar |
| RD-SUP-02 | Provedor externo substituivel para NF-e | DDE + fork | PlugNotas/SEFAZ/fornecedor nao devem contaminar regra de negocio |
| RD-LOG-01 | API B2B de consulta de status | DDE | Parceiros externos precisam consultar despacho/tracking |
| RD-PRD-01 | Dimensoes de produto | DDE | Necessario para carga, cubagem e frete |
| RD-FLE-01 | Capacidade de carga do veiculo | DDE | Necessario para frota, rota e expedicao |
| RD-HR-01 | Apontamento de horas por usuario/tenant | DDE | Dashboard e integracao contabilidade citam horas trabalhadas |
| RD-HR-02 | Integracao com software contabil | DDE | Envio de dados de RH/horas para sistema externo |
| RD-HR-03 | Escalas e turnos | Docs posteriores | Necessario para alocacao de pessoas e motoristas |
| RD-TEC-01 | Eventos de dominio via RabbitMQ | DDE + NF-02 | Fluxos precisam publicar entrada, despacho e alteracao de OP; eventos carregam tenant explicitamente |
| RD-EDGE-01 | Responsabilidades da borda BFF/Gateway | Critica arquitetural do fork | Evita duplicar autenticacao, tenant e roteamento entre camadas |
| RD-AUD-01 | Politica de falha de auditoria | RF-05, RN-08 + critica arquitetural | Define quando uma falha de auditoria bloqueia a operacao |
| RD-UI-01 | Interface responsiva | DDE | Validacao em desktop, tablet e mobile |
| RD-DOC-01 | Manual, documentacao tecnica e deploy | DDE | Entregaveis finais do projeto |

## 3. Requisitos Nao Funcionais

| ID | Requisito | Essencia |
|---|---|---|
| NF-01 | Resiliencia | Circuit breaker e retry para falhas de servicos |
| NF-02 | Consistencia eventual | Outbox Pattern para evitar perda de eventos |
| NF-03 | Observabilidade | Logs, metricas e tracing correlacionaveis |
| NF-04 | Performance de API | Consultas abaixo de 500ms no P95 |
| NF-05 | Seguranca de dados | Dados sensiveis em repouso e TLS em transito |
| NF-06 | Escalabilidade horizontal | Servicos com multiplas instancias sem sessao sticky obrigatoria |
| NF-07 | Localizacao e fuso horario | i18n e timezone por filial |

## 4. Regras De Negocio

| ID | Regra | Essencia |
|---|---|---|
| RN-01 | Isolamento por tenant | Impedir leitura/escrita cruzada entre unidades |
| RN-02 | Autorizacao obrigatoria por permissao | Deny by default em operacoes sensiveis |
| RN-03 | Reserva na liberacao da OP | Reservar insumos e bloquear duplicidade/saldo insuficiente |
| RN-04 | Bloqueio por qualidade | OP nao finaliza sem inspecoes aprovadas |
| RN-05 | Conferencia cega sem vazamento | Quantidades esperadas so aparecem depois da contagem |
| RN-06 | Divergencias inbound exigem tratamento | Material divergente nao fica disponivel sem decisao |
| RN-07 | Conferencia de embarque obrigatoria | Saida bloqueada se volume divergir |
| RN-08 | Auditoria de acoes sensiveis | Token/API key/permissao precisam de trilha auditavel |

## 5. Pontos Criticos De Rastreabilidade

- O fork deve usar os IDs do PDF quando se referir ao ERS academico.
- IDs antigos como `RF-SUP-06`, `RF-LOG-01` ou `RF-HRS-03` podem aparecer em docs anteriores, mas nao sao a numeracao canonica deste PDF.
- `RF-33` nao deve ser reaproveitado sem decisao explicita, porque o PDF pulou esse ID.
- Requisitos derivados devem continuar com prefixo `RD-*` para nao confundir com RF formal.
