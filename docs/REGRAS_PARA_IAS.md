# Regras Para IAs No Rail-Factory Fork

Este documento define como agentes de IA devem trabalhar neste projeto.

Objetivo: evitar retrabalho, complexidade prematura, quebra arquitetural e documentacao fora de sincronia com o codigo.

## 1. Regra Principal

Toda IA deve entregar mudancas pequenas, verificaveis e alinhadas com a passada atual.

Nao e permitido:

- pular ordem de construcao;
- implementar recursos avancados antes do fluxo exigir;
- misturar responsabilidades entre dominios;
- criar abstracoes vazias sem uso real;
- manter implementacao provisoria, placeholder funcional ou contrato temporario quando ja houver definicao de versao final;
- marcar task como concluida sem criterio de aceite;
- deixar `CONTEXTO_ATUAL.md` desatualizado depois de mudar estado real do projeto.

Diretriz obrigatoria: priorizar sempre implementacao definitiva para a versao final. Quando um trecho estiver temporario, ele deve ser substituido na mesma passada ou registrado explicitamente como pendencia bloqueadora.

## 2. Fluxo Obrigatorio De Trabalho

Antes de editar codigo:

1. Ler `docs/CONTEXTO_ATUAL.md`.
2. Ler a secao relevante de `docs/PLANO_DE_TASKS.md`.
3. Identificar a passada atual.
4. Identificar o bounded context afetado.
5. Definir o criterio de aceite da mudanca.

Durante a implementacao:

1. Manter a mudanca pequena.
2. Separar regra de dominio de infraestrutura.
3. Nao mover responsabilidade entre microservicos sem decisao documentada.
4. Criar testes quando a regra de negocio ou contrato publico mudar.
5. Rodar validacao aplicavel.

Depois da implementacao:

1. Atualizar `docs/CONTEXTO_ATUAL.md` se o estado real mudou.
2. Atualizar `docs/PLANO_DE_TASKS.md` se uma task foi entregue ou desbloqueada.
3. Registrar decisao arquitetural no documento correto quando houver mudanca de direcao.
4. Informar builds/testes executados.

## 3. SOLID A Risca

SOLID deve ser aplicado como regra de desenho, nao como ornamentacao.

### S - Single Responsibility

Cada classe, endpoint, handler ou componente deve ter um unico motivo claro para mudar.

Regras:

- entidade de dominio nao acessa banco, HTTP, fila ou configuracao;
- caso de uso nao renderiza resposta HTTP;
- controller/endpoint nao contem regra de negocio;
- componente React nao deve concentrar chamada de API, transformacao complexa e layout extenso quando isso puder ser separado com clareza.

Sinais de violacao:

- classe com dependencias demais;
- metodo que valida, persiste, publica evento e monta resposta HTTP;
- servico chamado `Manager`, `Helper`, `Processor` ou `Service` sem responsabilidade precisa.

### O - Open/Closed

Adicionar comportamento deve preferir extensao em pontos de variacao reais.

Regras:

- criar interface quando existir fronteira externa, adaptador substituivel ou variacao concreta prevista no fluxo;
- nao criar interface para toda classe automaticamente;
- provedores externos, mensageria, relogio, storage, email, OAuth, NF-e e banco entram por portas.

### L - Liskov Substitution

Implementacoes de uma porta devem obedecer o mesmo contrato.

Regras:

- nao mudar significado de retorno entre implementacoes;
- nao esconder excecoes de forma diferente em adaptadores equivalentes;
- documentar comportamento de erro, idempotencia e timeout da porta.

### I - Interface Segregation

Portas devem ser pequenas e orientadas ao caso de uso.

Regras:

- evitar interfaces gigantes por dominio;
- separar leitura de escrita quando os consumidores nao precisam das duas;
- nao forcar um adaptador a implementar metodo que nao usa.

### D - Dependency Inversion

O dominio e a aplicacao nao dependem de detalhes.

Regras:

- Domain nao depende de Application, Infrastructure, Api ou UI;
- Application depende de abstracoes/portas;
- Infrastructure implementa portas;
- Api chama Application;
- composicao de dependencias fica na borda.

## 4. Arquitetura Hexagonal

Cada microservico deve seguir a organizacao conceitual:

```text
Api -> Application -> Domain
Infrastructure -> Application ports
```

Dependencias permitidas:

| Origem | Pode depender de |
|---|---|
| Domain | Nenhum projeto interno de camada externa |
| Application | Domain e portas da propria Application |
| Infrastructure | Application, Domain e bibliotecas externas |
| Api | Application, Infrastructure e contratos HTTP |
| Tests | Qualquer camada necessaria ao tipo de teste |

Dependencias proibidas:

- Domain -> Infrastructure;
- Domain -> Api;
- Domain -> EF Core, ASP.NET, RabbitMQ, Redis, Aspire, YARP;
- Application -> Api;
- Application -> implementacao concreta de banco/fila/provedor;
- UI -> Gateway ou microservicos internos.

## 4.1 Convencao De Program.cs Em APIs

Para reduzir divergencia estrutural entre microservicos, `Program.cs` deve seguir o mesmo desenho base.

Regras:

- `Program.cs` e apenas composicao de pipeline e mapeamento de rotas;
- regra de negocio fica fora do endpoint inline, em caso de uso na `Application`;
- quando houver muitos endpoints, extrair mapeamento para `Api/*Endpoints` e manter `Program.cs` com composicao minima;
- usar constante para rotas publicas estaveis (ex: `InfoPath = "/info"`);
- preferir handler nomeado (`HandleGetInfo`) em arquivo de endpoints em vez de bloco inline longo no `Program.cs`;
- manter ordem consistente: `AddServiceDefaults`/`AddTenantResolution`, build, `UseServiceDefaults`/`UseTenantResolution`, `MapDefaultEndpoints`, rotas do servico.

Objetivo: facilitar manutencao, leitura e comparacao entre servicos sem alterar contratos HTTP.

## 5. Bounded Contexts

Responsabilidades nao devem vazar:

| Contexto | Dono da regra |
|---|---|
| Tenancy | catalogo e resolucao de tenant |
| IAM | identidade, login, sessao, permissao e auditoria de seguranca |
| Supply Chain | recebimento, XML/NF-e, conferencia e divergencia |
| Inventory | saldo, reserva, bloqueio, consumo e ledger |
| Production | BOM, OP, execucao, scrap, qualidade e lote |
| Logistics | expedicao, picking, packing, frete e tracking |
| HR | pessoas, horas, competencias e turnos |
| Fleet | veiculos, motorista/veiculo, manutencao e abastecimento |
| Dashboard | leitura, indicadores e read models |

Regra critica: Supply Chain e Production nunca viram donos de saldo. Inventory e a unica fonte de saldo.

## 6. Tenant Sempre Explicito

Toda operacao tenant-aware deve ter tenant claro.

Regras:

- HTTP usa `X-Tenant-Code`;
- eventos carregam `tenantCode`;
- jobs recebem `tenantCode`;
- logs e traces devem carregar tenant quando houver;
- tenant `dev` e o unico tenant inicial, mas nao deve ser hardcoded como regra permanente de dominio.

## 7. Eventos, Outbox E Idempotencia

Eventos devem ser modelados cedo, mas publicados por RabbitMQ/Outbox apenas quando o fluxo exigir confiabilidade.

Todo evento deve ter:

- `eventId`;
- `eventType`;
- `eventVersion`;
- `occurredAt`;
- `tenantCode`;
- `correlationId`;
- `producer`;
- `payload`.

Consumidores devem ser idempotentes quando houver retry, fila ou outbox.

## 8. Erros, Logs E Observabilidade

P0 ja entregou os padroes iniciais:

- contrato padrao de erro com `ProblemDetails`;
- `X-Correlation-Id`;
- logs estruturados com `CorrelationId` e `TraceId`;
- contratos HTTP iniciais em `docs/CONTRATOS_API.md`.

Ao ampliar esses itens:

- nao expor segredo em erro;
- erro de dominio deve ser distinguivel de erro tecnico;
- toda request deve ter correlation id;
- health checks devem continuar baratos e sem efeito colateral.

## 9. Atualizacao De Contexto

Atualize `docs/CONTEXTO_ATUAL.md` quando:

- novo projeto for criado;
- novo recurso Aspire for adicionado;
- endpoint ou rota for validado;
- uma task mudar de estado;
- uma pendencia for removida ou adicionada;
- um comando de verificacao mudar.

Atualize `docs/PLANO_DE_TASKS.md` quando:

- task for concluida;
- criterio de aceite mudar;
- dependencia nova aparecer;
- nova task for necessaria.

Atualize arquitetura/requisitos apenas quando houver decisao real, nao por detalhe de implementacao.

## 10. Checklist Antes De Encerrar Uma Task

- [ ] A mudanca esta na passada correta.
- [ ] A responsabilidade ficou no bounded context correto.
- [ ] SOLID foi respeitado.
- [ ] Domain nao depende de infraestrutura.
- [ ] Tenant foi tratado explicitamente quando aplicavel.
- [ ] Erros e logs nao escondem falha importante.
- [ ] Build/teste aplicavel foi executado.
- [ ] `CONTEXTO_ATUAL.md` foi atualizado se o estado mudou.
- [ ] `PLANO_DE_TASKS.md` foi atualizado se task mudou de estado.

## 11. Refatoracao Continua Em Paralelo As Tasks

Refatoracao e permitida durante as proximas passadas, desde que seja incremental e nao desvie da entrega funcional da task ativa.

Regras:

- cada refatoracao deve ficar no mesmo bounded context da task atual ou em camada compartilhada ja usada pela task;
- nao mudar contrato publico sem atualizar aceite e `docs/CONTRATOS_API.md`;
- refatoracao deve sair com build/teste/smoke da task ativa ainda passando;
- preferir refatoracao por fatias pequenas (ex: um servico por vez), evitando big-bang nos microservicos;
- quando houver refatoracao paralela, registrar no `CONTEXTO_ATUAL.md` o que foi padronizado e o que ainda falta.
