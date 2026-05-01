# Justificativas Arquiteturais

Este documento registra as decisoes que guiam o fork.

O objetivo e manter o produto Rail-Factory completo, mas construir de forma incremental para evitar retrabalho e complexidade prematura.

## 1. O Fork Nao Muda O Produto

O fork continua sendo o Rail-Factory completo:

- IAM;
- multitenancy;
- Supply Chain;
- Inventory;
- Production;
- Logistics;
- Fleet;
- HR;
- Dashboard.

A mudanca esta na estrategia de implementacao.

O fork nao deve tentar reconstruir todos os modulos completos ao mesmo tempo. Ele deve construir fluxos pequenos e reais, voltando aos servicos conforme novas necessidades aparecerem.

## 0. Correcoes Sobre A Arquitetura Original

A arquitetura original deve ser usada como referencia tecnica, mas nao deve ser copiada sem ajustes.

| Risco encontrado | Correcao adotada no fork |
|---|---|
| Estoque sem fronteira propria, espalhado entre entrada e producao | Inventory vira bounded context proprio desde P2 |
| Muitos componentes ativos antes do primeiro fluxo real | Iniciar com tenant `dev` e somente os servicos necessarios para o fluxo atual |
| Tenant implicito em contexto HTTP | Eventos, outbox e jobs devem carregar `tenantCode` explicitamente |
| Possivel duplicacao entre BFF, Gateway e servicos | Definir responsabilidades fixas para cada camada |
| Mensageria e outbox antes da maturidade dos fluxos | Contratos primeiro; RabbitMQ/Outbox nos fluxos criticos |
| Auditoria sem politica de falha clara | Definir fail-closed para acoes sensiveis e fail-open controlado para acoes operacionais nao sensiveis |
| Banco por servico por tenant dificulta dashboards consolidados | Dashboard deve ler read models/consultas consolidadas, nao consultar saldo duplicado |

## 1.1. Rastreabilidade Dos Requisitos

### Decisao

- RF, NF e RN seguem a numeracao do PDF DDE + ERS.
- O buraco `RF-33` deve ser preservado.
- Itens que aparecem no DDE, em ADRs ou em documentacao posterior, mas nao como RF formal no PDF, devem usar prefixo `RD-*`.

### Justificativa

Misturar numeracao academica, numeracao posterior e itens derivados cria confusao na implementacao.

Separar `RF-*` de `RD-*` permite:

- rastrear o que veio do PDF;
- manter itens importantes do DDE;
- evitar reaproveitar IDs que o PDF nao definiu;
- discutir prioridades sem perder a origem do requisito.

## 2. Multitenancy

### Decisao

- Modelo alvo: PostgreSQL `database-per-tenant`.
- Tenant inicial: `dev`.
- Identificacao inicial: `X-Tenant-Code: dev`.
- Resolver tenant continua obrigatorio no desenho do sistema.
- Tenant Catalog/Registry deve ser separado dos bancos dos tenants, como no projeto original.
- O projeto original tambem usa tenant `qa`; o fork comeca com `dev` e deve manter o caminho preparado para adicionar `qa` depois.
- Eventos publicados, mensagens Outbox e jobs em background devem carregar o tenant explicitamente.

### Justificativa

Mesmo usando apenas uma filial no inicio, o sistema precisa nascer tenant-aware.

Se o tenant for ignorado no comeco, o projeto tera retrabalho alto quando novas filiais forem adicionadas.

Por outro lado, nao e necessario implementar toda a gestao avancada de tenants na primeira passada. O essencial e:

- toda operacao saber qual tenant esta usando;
- os dados principais guardarem ou resolverem o tenant corretamente;
- nao haver dependencia de tenant fixo escondido no codigo.
- nenhum consumidor assincrono depender de contexto HTTP para descobrir tenant.

## 3. IAM Primeiro

### Decisao

O primeiro dominio a nascer deve ser IAM.

Primeira passada:

- OAuth Google;
- usuario autenticado;
- tenant `dev`;
- identificacao basica de usuario e tenant nas requisicoes.

### Justificativa

IAM vem primeiro porque todos os fluxos precisam saber:

- quem esta operando;
- em qual tenant esta operando;
- quais dados podem ser acessados.

RBAC completo, MFA, API keys e auditoria detalhada entram depois, quando houver operacoes suficientes para justificar a granularidade.

## 4. Entrada De Materiais Antes De Producao

### Decisao

Depois do IAM, o primeiro fluxo de negocio deve ser Supply Chain / entrada de materiais.

### Justificativa

Production depende de material disponivel.

Se a producao for criada antes da entrada de materiais e do estoque, o projeto tende a inventar saldos falsos, mocks permanentes ou regras que depois serao reescritas.

Por isso a ordem preferida e:

1. entrada de material;
2. saldo pendente;
3. conferencia;
4. saldo disponivel;
5. producao usando saldo real.

## 5. Inventory Como Fronteira Clara

### Decisao

Inventory deve aparecer como dominio/fronteira desde o inicio.

No fork, Inventory deve nascer como bounded context proprio a partir de P2, com:

- API propria;
- banco proprio por tenant;
- ledger simples de movimentacao;
- saldos por tenant, material, local/status e UoM;
- contratos claros para Supply Chain e Production.

A primeira versao deve ser pequena, mas a responsabilidade nao deve ficar dentro de Supply Chain nem dentro de Production:

- Supply Chain recebe materiais.
- Inventory controla saldo.
- Production reserva e consome saldo.

### Justificativa

Sem essa fronteira, Supply Chain e Production podem duplicar regras de estoque.

Duplicacao de saldo e uma das maiores fontes de retrabalho neste tipo de sistema.

Separar Inventory cedo adiciona um servico a mais, mas evita que a regra central do sistema fique duplicada em dois dominios que evoluem em ritmos diferentes.

## 5.1. Responsabilidades Da Borda: Frontend, BFF E Gateway

### Decisao

- A UI no navegador chama apenas o BFF.
- O BFF mantem cookie/sessao, CSRF e estado de autenticacao do usuario.
- O BFF encaminha chamadas de dominio pelo Gateway.
- O Gateway faz roteamento YARP, normalizacao de tenant header, rate limit e politicas transversais de entrada.
- Servicos de dominio nao devem depender de detalhes de cookie do frontend.
- Servicos de dominio validam tenant e permissao recebidos do fluxo autenticado.

### Justificativa

Sem essa divisao, autenticacao, tenant e roteamento ficam repetidos em varias camadas.

A regra pratica e:

- BFF entende usuario e sessao.
- Gateway entende entrada de API e roteamento.
- Servico entende regra de negocio.

## 5.2. Politica De Auditoria

### Decisao

A auditoria deve ter politica explicita por tipo de acao:

| Tipo de acao | Politica inicial |
|---|---|
| Login, permissao, API key, MFA, mudanca de tenant | Fail-closed: se nao auditar, a operacao nao deve concluir |
| Entrada de material, divergencia, reserva, consumo, despacho | Registrar auditoria no proprio fluxo e publicar/replicar depois se necessario |
| Consulta simples sem alteracao sensivel | Fail-open com log tecnico |

### Justificativa

Auditoria e requisito de seguranca, mas uma dependencia HTTP transversal nao pode parar toda a operacao industrial sem decisao consciente.

Operacoes sensiveis de seguranca devem bloquear se nao houver trilha. Operacoes operacionais devem preservar o registro no banco do proprio dominio e reconciliar auditoria depois quando necessario.

## 6. Eventos E Mensageria

### Decisao

O produto completo deve usar eventos onde fizer sentido, com RabbitMQ e MassTransit.

Na primeira passada, os contratos de evento podem ser definidos antes da mensageria completa.

O projeto original ja usa RabbitMQ no AppHost e MassTransit em Production. O fork deve preservar esse caminho, mas ativar a complexidade de mensageria conforme os fluxos precisarem.

Todo evento deve carregar ao menos:

- `eventId`;
- `occurredAt`;
- `tenantCode`;
- `correlationId`;
- `producer`;
- versao do contrato;
- payload minimo do acontecimento.

### Justificativa

Eventos ajudam a desacoplar os dominios, mas tambem aumentam complexidade.

O fork deve evitar criar infraestrutura pesada antes de haver fluxos reais. A ordem segura e:

1. definir o contrato do acontecimento;
2. usar chamada direta simples se ainda for suficiente;
3. trocar por evento real quando houver necessidade de desacoplamento, retry, Outbox ou multiplos consumidores.

## 7. Outbox Pattern

### Decisao

Outbox e parte da arquitetura alvo, mas nao precisa estar em todos os servicos no primeiro dia.

### Justificativa

Outbox e importante quando um evento precisa ser garantido mesmo se o broker falhar.

Antes de existirem eventos criticos suficientes, implementar Outbox em tudo aumenta complexidade sem retorno imediato.

Ele deve entrar primeiro nos fluxos onde a perda de evento causaria erro operacional, como:

- entrada confirmada de material;
- reserva de material;
- finalizacao de OP;
- despacho.

## 8. PlugNotas / SEFAZ

### Decisao

O dominio deve tratar a origem de NF-e como provedor substituivel.

O provedor inicial pode ser PlugNotas, mas os documentos devem falar em PlugNotas/SEFAZ quando a capacidade for captura de NF-e.

### Justificativa

Isso evita acoplar a regra de negocio a um fornecedor externo.

Supply Chain precisa entender "NF-e recebida", "XML importado", "recebimento criado" e "conferencia feita". O detalhe de buscar isso via PlugNotas, SEFAZ direto ou upload manual deve ficar isolado.

## 9. Dashboard Depois Dos Dados Reais

### Decisao

Dashboard completo deve ficar depois dos fluxos operacionais principais.

### Justificativa

Dashboard sem dados reais tende a virar tela demonstrativa.

O fork deve primeiro gerar dados confiaveis em Supply, Inventory e Production. Depois o Dashboard pode nascer lendo informacoes reais e evoluir para OEE, custos, alertas e relatorios.

## 10. Criterio Para Adiar Complexidade

Uma solucao mais complexa deve ser adiada quando:

- nao e necessaria para o fluxo atual;
- depende de dados que ainda nao existem;
- nao reduz retrabalho;
- aumenta acoplamento;
- exige integracao externa que ainda nao sera validada.

Uma solucao deve entrar cedo quando:

- evita reescrita estrutural;
- define uma fronteira importante;
- protege tenant;
- garante consistencia de dados;
- desbloqueia o proximo fluxo.
