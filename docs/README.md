# Documentacao Do Fork

Esta pasta organiza a reconstrucao incremental do Rail-Factory.

O fork continua sendo o projeto completo. A documentacao aqui existe para guiar uma construcao mais limpa, rapida e com menos retrabalho.

## Ordem De Leitura

1. `CONTEXTO_ATUAL.md`
   - Mostra o que ja foi implementado no fork.
   - Registra comandos, rotas validadas, estado de P0/P1 e proxima task.

2. `REGRAS_PARA_IAS.md`
   - Define como IAs devem trabalhar no projeto.
   - Reforca SOLID, Arquitetura Hexagonal, limites por dominio e atualizacao de contexto.

3. `RESUMO_PROJETO_CONTEXTO.md`
   - Explica o objetivo do fork.
   - Define a prioridade inicial: IAM, entrada de materiais, estoque e depois producao.

4. `ARQUITETURA_GERAL.md`
   - Define a arquitetura alvo do fork.
   - Mostra C4 Context e Container.
   - Define responsabilidades de BFF, Gateway, servicos, bancos, tenant, Inventory, eventos, seguranca e observabilidade.

5. `GRAFOS_DO_PROJETO.md`
   - Mostra os grafos do sistema.
   - Mostra todas as partes do projeto.
   - Mostra a ordem de construcao.
   - Mostra quando voltamos a cada dominio.

6. `ANALISE_REQUISITOS_E_PASSADAS.md`
   - Lista todos os requisitos um por um.
   - Mostra o que cada requisito precisa antes.
   - Define a primeira entrega de cada requisito.
   - Ordena as passadas de construcao.

7. `PLANO_DE_TASKS.md`
   - Transforma as passadas em checklist executavel.
   - Detalha tasks, dependencias e criterios de aceite ate o final do projeto.
   - Lista tambem os proximos documentos tecnicos que ainda precisam ser criados.

8. `CONFERENCIA_REQUISITOS_PDF.md`
   - Confere a matriz do fork com o PDF DDE + ERS.
   - Mostra requisitos cobertos.
   - Registra o buraco `RF-33` e os requisitos derivados.

9. `REQUISITOS.md`
   - Lista os requisitos canonicos do PDF.
   - Lista os requisitos derivados `RD-*`.

10. `FUNCIONALIDADES.md`
   - Organiza as funcionalidades por dominio.
   - Mostra o que entra em cada passada.

11. `JUSTIFICATIVAS.md`
   - Explica as decisoes arquiteturais.
   - Registra por que IAM vem primeiro e entrada de materiais vem antes de producao.
   - Registra as correcoes aplicadas sobre os riscos encontrados na arquitetura original.

## Estado Atual Da Implementacao

Fonte de verdade: `CONTEXTO_ATUAL.md`.

Resumo:

- P0 foi concluido como base inicial: AppHost, ServiceDefaults, Gateway, BFF, UI, infra local, BuildingBlocks, contratos HTTP, erro padrao, `correlationId`, logs e convencoes event-driven iniciais.
- P1 foi iniciado pelo Tenancy: tenant `dev` persistido no Tenant Catalog, tabela `tenants`, seed idempotente, repositorio PostgreSQL e endpoint de leitura via Gateway.
- A proxima task e criar o resolver por `X-Tenant-Code` e o middleware tenant-aware.
- Toda mudanca de estado deve atualizar primeiro `CONTEXTO_ATUAL.md` e depois refletir no checklist de `PLANO_DE_TASKS.md`.

## Decisoes Ja Definidas

- O produto final e o Rail-Factory completo.
- A stack tecnica segue o codigo original real: .NET 10, Aspire, BFF .NET + React/Vite, Gateway YARP, PostgreSQL, Redis e RabbitMQ.
- A construcao comeca por IAM.
- O login inicial deve ser OAuth com Google.
- A primeira filial/tenant sera `dev`.
- A primeira logica de negocio sera entrada de materiais.
- Inventory deve existir como fronteira clara de estoque/saldo desde a entrada de materiais.
- No fork, Inventory deve nascer como bounded context proprio, com API e banco por tenant, mesmo que a primeira versao seja pequena.
- Production vem depois que existir saldo real para usar.
- Dashboard inicial vem depois de Supply, Inventory e Production.
- HR e Fleet entram antes de Logistics completa, porque expedicao precisa de pessoas, motoristas e veiculos.
- RF/NF/RN seguem a numeracao do PDF; escopo derivado usa `RD-*`.
- UI no browser fala com BFF; BFF mantem sessao/cookie; Gateway roteia e normaliza chamadas internas.
- Eventos carregam tenant explicitamente; nenhum consumidor deve depender de tenant vindo de contexto HTTP.
- Auditoria sensivel deve ter politica clara de falha antes de ir para producao.
- IAs devem seguir `AGENTS.md`, `docs/REGRAS_PARA_IAS.md` e a skill local `.codex/skills/rail-factory-engineering`.

## Regra Pratica

Construir primeiro o menor fluxo real que desbloqueia o proximo passo.

Depois voltar aos dominios e ampliar somente o que o fluxo seguinte exigir.
