# Resumo do Projeto Rail-Factory Fork

## 1. Objetivo do Fork

O `Rail-Factory-Fork` e o mesmo produto Rail-Factory: um ERP industrial multitenant para gestao fabril, entrada de materiais, producao, estoque, expedicao, frota, pessoas e dashboards.

A diferenca do fork nao e o escopo do sistema. A diferenca e a forma de construcao.

O fork deve reconstruir o projeto completo com uma abordagem mais incremental, limpa e objetiva, evitando:

- criar complexidade antes de ela ser necessaria;
- implementar recursos que ainda nao serao usados;
- repetir trabalho por falta de ordem;
- criar separacao fisica ou integracoes complexas antes de entender bem os fluxos reais;
- misturar requisitos do produto final com tarefas da primeira entrega.

## 2. Regra Principal de Construcao

O sistema sera construido por fluxos de negocio de ponta a ponta.

Em vez de finalizar um dominio inteiro antes de tocar nos outros, a implementacao deve ir e voltar entre os dominios conforme cada fluxo exigir.

Exemplo:

1. Criar o minimo de IAM para login e tenant de desenvolvimento.
2. Criar o minimo de Supply Chain para entrada de materiais.
3. Criar o minimo de Inventory/estoque para disponibilizar saldo.
4. Voltar ao Supply Chain para fechar conferencia e divergencias.
5. Depois iniciar Production usando o saldo que ja existe.
6. Voltar ao Inventory para reserva de materiais.
7. Voltar ao Production para consumo, sucata e fechamento de OP.

Essa abordagem reduz retrabalho porque cada parte nasce quando existe uma necessidade concreta no fluxo.

## 2.1. Correcoes Aplicadas Sobre A Arquitetura Original

A arquitetura original e uma boa referencia de stack e topologia, mas o fork corrige alguns pontos antes de reconstruir:

| Problema observado no original | Decisao no fork |
|---|---|
| Estoque aparecia espalhado entre Supply Chain e Production | Inventory vira bounded context proprio desde P2 |
| Topologia inicial muito pesada, com dev e qa desde o comeco | Primeira reconstrucao usa apenas tenant `dev` |
| Tenant podia ser tratado como contexto de requisicao | Eventos, outbox e jobs sempre carregam tenant explicitamente |
| BFF, Gateway e servicos podiam duplicar responsabilidades | UI no browser fala com BFF; BFF cuida de sessao; Gateway cuida de roteamento, tenant header e politicas de entrada |
| Mensageria podia entrar antes da necessidade real | Contratos de evento entram cedo; RabbitMQ/Outbox entram apenas nos fluxos criticos |
| Auditoria dependia de integracao transversal sem politica clara | Operacoes sensiveis definem se falha de auditoria bloqueia ou apenas registra erro operacional |

## 3. Prioridade Inicial

A primeira construcao deve seguir esta ordem:

1. **IAM**
   - OAuth com Google.
   - Usuario autenticado.
   - Tenant de desenvolvimento.
   - Base para autorizacao futura.

2. **Supply Chain / Entrada de Materiais**
   - Entrada de XML/NF-e.
   - Conferencia cega.
   - Tratamento de divergencias.
   - Disponibilizacao de saldo para o estoque.

3. **Inventory / Estoque**
   - Saldo recebido.
   - Saldo bloqueado ou pendente.
   - Saldo disponivel para producao.
   - Reserva futura para OP.
   - API e banco proprios por tenant para evitar duplicacao de saldo em Supply Chain e Production.

4. **Production**
   - BOM.
   - Ordem de producao.
   - Reserva de material.
   - Consumo, sucata, paradas, qualidade e lote.

5. **Dashboard inicial**
   - Entra depois que Supply, Inventory e Production gerarem dados reais.
   - Comeca simples, sem OEE completo.

6. **HR e Fleet**
   - Entram antes da expedicao completa porque Logistics precisa de pessoas, motoristas e veiculos.
   - Comecam com cadastro basico, horas, veiculos e capacidade.

7. **Logistics**
   - Entra depois de existir produto acabado, pessoas e veiculos.
   - Comeca com expedicao simples, frete simples e status B2B.

## 4. Tenant Inicial

A primeira versao do fork deve trabalhar com apenas uma filial de desenvolvimento:

- tenant: `dev`
- identificacao: `X-Tenant-Code: dev`
- objetivo: reduzir complexidade inicial sem abandonar o modelo multitenant.

Mesmo com um unico tenant inicial, o codigo deve respeitar a separacao por tenant desde o comeco. Isso evita reescrita quando novas filiais forem adicionadas.

## 5. Stack Canonica Baseada No Projeto Original

O fork deve usar como referencia o que o projeto original ja usa no codigo:

- C# / .NET 10
- .NET Aspire AppHost 13.2.4
- PostgreSQL com banco separado por tenant e banco separado para Tenant Catalog
- banco de Inventory separado por tenant desde a entrada de materiais
- Redis para cache/sessao/estado de autenticacao quando necessario
- RabbitMQ para mensageria
- MassTransit nos fluxos que precisarem de orquestracao/eventos robustos
- Gateway com YARP
- Frontend com BFF .NET + React/Vite
- React 19.1.x, Vite 7.1.x, MUI 7, lucide-react
- OAuth2 com Google para autenticacao
- OpenTelemetry e health checks via ServiceDefaults

Componentes reais usados como base:

- `RailFactory.AppHost`
- `RailFactory.ServiceDefaults`
- `RailFactory.Gateway`
- `RailFactory.Frontend` e `RailFactory.Frontend/App`
- `RailFactory.Tenancy.Api`
- `RailFactory.Tenancy.Application`
- `RailFactory.Tenancy.Domain`
- `RailFactory.Tenancy.Infrastructure`
- `RailFactory.Iam.Api/Application/Domain/Infrastructure`
- `RailFactory.Production.Api/Application/Domain/Infrastructure`
- `RailFactory.SupplyChain.Api/Application/Domain/Infrastructure`

O projeto original tambem possui tenants `dev` e `qa` no AppHost. No fork, a primeira reconstrucao deve comecar apenas com `dev`, mantendo o desenho preparado para adicionar `qa` depois.

## 6. Como Ler Esta Documentacao

- `CONTEXTO_ATUAL.md`: estado real da implementacao, comandos, rotas validadas e pendencias imediatas.
- `GRAFOS_DO_PROJETO.md`: visao visual do sistema, dependencias e ordem de construcao.
- `REQUISITOS.md`: fonte canonica dos requisitos do PDF, mais requisitos derivados `RD-*`.
- `ANALISE_REQUISITOS_E_PASSADAS.md`: analise requisito por requisito e ordem de construcao.
- `FUNCIONALIDADES.md`: responsabilidades por microservico e o que fazer em cada passada.
- `JUSTIFICATIVAS.md`: decisoes arquiteturais que devem guiar a reconstrucao.

## 6.1. Estado Atual Da Implementacao

Fonte de verdade do estado atual: `CONTEXTO_ATUAL.md`.

Resumo em 2026-05-01:

- solution e projetos iniciais em `src/`;
- AppHost Aspire;
- ServiceDefaults;
- BuildingBlocks compartilhado;
- Gateway YARP;
- BFF .NET;
- UI React/Vite;
- Tenancy com tenant `dev` persistido no Tenant Catalog e leitura via Gateway;
- APIs placeholder de IAM, SupplyChain, Inventory e Production;
- PostgreSQL, Tenant Catalog DB, bancos tenant `dev`, Redis e RabbitMQ no AppHost.

P0 foi concluido como base inicial. Ja existem:

- contrato padrao de erro com `ProblemDetails`;
- logs estruturados com `CorrelationId` e `TraceId`;
- `X-Correlation-Id` propagado pelo Gateway/servicos;
- contratos HTTP iniciais documentados;
- convenções event-driven e ports de repositorio/publicador.

P1 esta iniciado. A proxima retomada deve implementar o resolver `X-Tenant-Code` e o middleware tenant-aware antes do OAuth Google.

## 7. Principio de Decisao

Sempre que houver duvida entre uma solucao simples e uma solucao mais completa, escolha a solucao simples se ela:

- atende o fluxo atual;
- nao bloqueia a evolucao futura;
- respeita tenant;
- deixa contratos claros;
- evita acoplamento desnecessario.

Complexidade deve entrar quando houver necessidade real no fluxo, nao por antecipacao.
