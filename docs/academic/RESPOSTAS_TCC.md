# Relatório de Autoavaliação e Análise Crítica de Desenvolvimento: Plataforma Rail-Factory-Fork

Este documento apresenta uma análise crítica e metodológica sobre o planejamento, gerenciamento, construção e validação da plataforma **Rail-Factory-Fork**, um sistema ERP de planejamento e controle de recursos industriais desenvolvido de forma individual sob rigorosos padrões de engenharia de software.

---

## 1. Organização das Etapas de Trabalho e Ciclo de Desenvolvimento

### Abordagem Adotada: Ciclos Iterativos por Fatias Funcionais (*Passadas*)
O projeto foi estruturado utilizando uma abordagem iterativa e incremental, dividida em 11 passadas de escopo funcional fechado (da P0 à P10), complementadas pela passada P15 para a evolução do domínio de fichas técnicas (*Bill of Materials - BOMs*). Esta metodologia assemelha-se a fatias verticais (*vertical slicing*), onde cada ciclo entrega infraestrutura, banco de dados, lógica de negócio (API) e interface do usuário (React) integrados e testados.

```
[P0: Infra & Gateway] ──> [P1: IAM & Tenancy] ──> [P2: NF-e & Workbench] ──> [P3: Inventory & Ledger] 
                                                                                   │
[P7: HR & Fleet] <── [P6: Dashboards & KPIs] <── [P5: OP Execution] <── [P4: Production Core]
       │
[P8: Logistics Api] ──> [P9: RabbitMQ & FIFO] ──> [P10: Webhooks & Audit] ──> [P15: BOM Evolution]
```

### Justificativa da Escolha Metodológica
A adoção de fatias verticais iterativas foi determinante para mitigar o risco clássico de "inferno de integração" (*integration hell*), típico de abordagens rígidas (cascata). Se tivéssemos documentado todo o ecossistema multi-serviços para depois codificá-lo de uma vez, inconsistências conceituais nos contratos de mensageria assíncrona entre o contexto de logística (`Logistics.Api`) e saldo de estoque (`Inventory.Api`) teriam sido detectadas apenas na fase final de testes, resultando em retrabalhos massivos e quebras arquiteturais. 

Com os ciclos iterativos, ao final da **Passada 5 (Execução de OP)**, o fluxo crítico do chão de fábrica (BOM -> Ordem de Produção -> Consumo -> Scrap -> Inspeção de Qualidade -> Baixa no Estoque) já estava completamente integrado e operacional, permitindo validações contínuas de consistência e alinhamento do modelo de domínio com as regras de negócio em ambiente de simulação realista.

---

## 2. Atuação em Multi-Papéis e Gestão de Backlog

### Experiência de Atuação Simultânea
A execução de um projeto complexo de forma solo exigiu a adoção de posturas mentais distintas e bem delimitadas para evitar sobreposição ou perda de rigor técnico:
*   **Gerente de Requisitos:** Foco em traduzir regras de negócio complexas do setor industrial (como a conferência cega de mercadorias baseada em XML de NF-e e o fluxo rígido de impressão de qualidade) em especificações técnicas formais nos documentos `REQUISITOS.md` e `FLUXOS_DE_TRABALHO.md`.
*   **Arquiteto e Desenvolvedor:** Responsável por blindar o núcleo do domínio usando a **Arquitetura Hexagonal (Ports & Adapters)**, garantindo que as regras de negócio de manufatura não dependessem do framework de persistência (EF Core 10) ou do middleware de mensageria (RabbitMQ).
*   **Analista de Testes:** Exigiu distanciamento do papel de programador para projetar cenários de testes destrutivos, analisando logs de auditoria e simulando falhas de rede no envio de webhooks externos.

### Ferramentas e Organização Temporal
Para garantir o cumprimento dos prazos e evitar o desvio de escopo (*scope creep*), foram empregadas as seguintes práticas e ferramentas:
1.  **Backlog Estruturado por Passadas (`PLANO_DE_TASKS.md`):** Cada passada contava com critérios de aceitação específicos, requisitos associados e definição de pronto (*Definition of Done - DoD*). Nenhuma linha de código era escrita sem que estivesse atrelada a uma entrega de valor planejada na passada corrente.
2.  **Orquestração Unificada com .NET Aspire:** Em vez de configurar bancos de dados e filas localmente de forma fragmentada, utilizou-se o .NET Aspire (`RailFactory.AppHost`) para gerenciar as dependências de infraestrutura (PostgreSQL com esquemas por tenant, Redis para cache de sessão e RabbitMQ para mensageria) como código. Isso permitiu instanciar o ambiente de desenvolvimento idêntico ao de produção com um único comando.
3.  **Logs Estruturados e Auditoria por Design:** O uso de *OpenTelemetry* e logs estruturados em formato JSON facilitou o diagnóstico de gargalos de concorrência no início do desenvolvimento de APIs.

---

## 3. Fluxo de Versionamento e Estratégia de Branches

Adotou-se o modelo **Feature Branch Workflow** sobre o Git. A branch `main` foi mantida estritamente estável, representando o estado de entrega validado de cada passada funcional.

```
main        ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━● (Merge P8) ━━━━━━━━━━━━━━━━━━━● (Merge P9) ━━
                                         ▲                               ▲
feature/p9-rabbitmq                      └───■───────■───────■───────────┘
                                           (Outbox) (FIFO) (DLX Topology)
```

### Fluxo de Trabalho Integrado
1.  Para cada requisito ou conjunto de tarefas de uma passada (ex: Integração assíncrona do estoque na Passada 9), abria-se uma branch específica (ex: `feature/p9-rabbitmq-logistics`).
2.  O desenvolvimento avançava nessa ramificação de forma isolada do restante das APIs operacionais.
3.  Ao concluir a implementação do código, executavam-se os linters de TypeScript no Frontend e as suítes de testes locais em .NET no Backend.
4.  O merge com a `main` ocorria por meio de Pull Requests com merge commits limpos, garantindo um histórico linear e rastreabilidade sobre qual release introduziu determinada alteração técnica.

Essa abordagem impediu a contaminação de módulos estáveis de produção durante a reestruturação e correção de bugs concorrentes complexos no banco de dados.

---

## 4. Obstáculos Técnicos Críticos e Resolução Individual

### O Desafio: Quirks de Persistência no EF Core 10 com Npgsql em Transações Complexas
Durante o desenvolvimento da inserção em massa de sub-itens de coleções agregadas — especificamente na persistência de componentes de engenharia na BOM (`AddBomItem`) e itens de despacho logístico (`AddShipmentItem`) —, o Entity Framework Core 10, ao trabalhar com o driver Npgsql, falhava ao tentar inferir chaves autogeradas durante o processamento em lotes concorrentes, gerando violações de chave estrangeira que bloqueavam a execução.

### Estratégia de Contorno
Sem o apoio de uma equipe de DBA ou arquitetura sênior, a solução exigiu uma imersão profunda na documentação de *tracking* do EF Core e no repositório de issues do Npgsql. A solução adotada seguiu os seguintes passos:
1.  **Isolamento do Problema:** Identificou-se que o rastreador de estado do EF Core causava overhead desnecessário e ordenação inadequada de inserção para tabelas associativas multitenant com concorrência otimizada.
2.  **Abordagem Híbrida de Persistência:** Optou-se por pivotar cirurgicamente nesses fluxos de escrita. Enquanto as leituras e operações simples mantiveram o uso convencional do EF Core para preservar a produtividade, a inserção e associação dos itens foram reescritas utilizando **SQL bruto otimizado parametrizado** (`AddItemDirectAsync`) sob transações coordenadas no banco.
3.  Isso permitiu contornar as falhas de mapeamento nativo de tipos do ORM sem comprometer as transações globais, mantendo as tabelas íntegras.

---

## 5. Gestão de Prazos, Desvios e Esforço por Etapa

Embora a maioria das entregas de negócios tenha ocorrido dentro dos marcos estipulados, houve desvios significativos no esforço necessário para concluir determinadas camadas técnicas.

### Matriz de Distribuição de Esforço Realizado
*   **Definição de Requisitos e Modelagem (15%):** Rápida e fluida devido à forte separação de domínios industriais (Supply Chain, Production, Logistics).
*   **Desenvolvimento de Front-end (20%):** O uso de um Design System unificado (`DESIGN_STYLE_GUIDE.md`) e hooks React encapsulados (`useQuery.ts` com suporte a `AbortController`) acelerou drasticamente a criação de telas.
*   **Persistência de Dados e Banco de Dados (25%):** Exigiu bastante esforço devido à modelagem de isolamento multitenant (bancos dinâmicos resolvidos em tempo de execução via `Tenancy.Api`).
*   **Integrações de Back-end, Mensageria e Concorrência (40%):** A etapa que mais consumiu recursos cronológicos além do planejado.

```
Requisitos/Modelagem  [███] 15%
Interface Frontend    [████] 20%
Banco / Multitenancy  [█████] 25%
Mensageria/Outbox/Con  [████████] 40% (Maior Gargalo)
```

### Análise da Causa Raiz do Atraso na P9/P10
Garantir a consistência eventual via mensageria assíncrona com RabbitMQ foi complexo. A necessidade de implementar o padrão **Outbox** com processamento assíncrono seguro (evitando processar a mesma mensagem de despacho duas vezes usando tabelas de controle de idempotência `IntegrationMessage` no inventário) consumiu tempo excessivo de testes. Cenários onde o transportador publicava o despacho e o inventário precisava deduzir saldos usando ordenação FIFO rígida sob concorrência intensa exigiram várias sessões de depuração de concorrência no banco de dados com comandos `SKIP LOCKED`.

---

## 6. Planejamento de Infraestrutura de Produção e Custos em Nuvem

Para suportar o sistema operando em escala corporativa real, sob o paradigma de arquitetura multitenant distribuída definido no projeto, estruturou-se a seguinte estimativa de custos de infraestrutura em nuvem (Microsoft Azure):

| Recurso / Serviço | Componente Técnico | Propósito Técnico | Custo Mensal Estimado |
|---|---|---|---|
| **Azure Kubernetes Service (AKS)** | Cluster K8s (3 nós Standard_D2s_v5) | Executar os microsserviços do monorepo de forma isolada, elástica e suportada pelo .NET Aspire. | USD 180,00 |
| **Azure Flexible PostgreSQL Server** | Postgres Dedicado (GP_Standard_D2ds_v5) | Armazenamento seguro dos bancos de dados por tenant e tabelas transacionais de auditoria e mensageria. | USD 90,00 |
| **CloudAMQP (RabbitMQ Gerenciado)** | Instância RabbitMQ Dedicada | Barramento de mensageria assíncrona responsável pelo tráfego de eventos do Outbox e webhooks. | USD 45,00 |
| **Azure Cache for Redis** | Redis C-Series Basic | Cache distribuído para chaves de sessão do BFF e tokens de autenticação interna de microsserviços. | USD 25,00 |
| **Azure Monitor & Log Analytics** | OpenTelemetry Ingestion | Capturar a telemetria, logs de auditoria detalhados e logs do YARP Gateway gerados pelo `ServiceDefaults`. | USD 35,00 |
| **Total Estimado** | — | — | **USD 375,00 / mês** |

---

## 7. Garantia da Neutralidade de Validação e Testes

### Como mitigar o viés do desenvolvedor em testes solo?
O maior risco de um projeto individual é a ausência de um olhar externo que questione o fluxo da aplicação. Para assegurar a neutralidade e integridade da validação, adotaram-se duas frentes técnicas:

1.  **Testes de Caixa Preta Automatizados via Playwright:**
    Foram desenvolvidos 29 cenários de teste ponta a ponta (*End-to-End*). Os testes Playwright simulam fluxos completos na interface gráfica como se fossem o operador final, validando comportamentos rígidos:
    *   Se o preenchimento de códigos de materiais é convertido para caixa alta (`Uppercase` e `Trim`).
    *   Se os botões de inativação de frotas e pessoas ativam modais de confirmação reais e modificam o estado no grid de visualização.
    *   O bloqueio de ações na UI dependendo do escopo do papel do usuário retornado pelo RBAC.

2.  **Mecanismo de Dev Bypass Seguro e Isolado:**
    Para testar permissões de acesso ao sistema de forma neutra sem depender da autenticação externa de terceiros (Google OAuth2), implementou-se um middleware exclusivo de desenvolvimento no BFF:
    ```csharp
    #if DEBUG
    if (builder.Environment.IsDevelopment() && httpContext.Request.Headers.TryGetValue("X-Dev-User", out var testEmail))
    {
        // Injeta uma sessão Mock com privilégios específicos do RBAC para testar negações e permissões de API.
        var session = MockAuthSession.CreateFor(testEmail, SystemPermissions.LogisticsRead);
        httpContext.Items["AuthSession"] = session;
    }
    #endif
    ```
    Isso garantiu a capacidade de executar automações simulando diferentes usuários (como um operador de estoque que tenta acessar relatórios financeiros) e confirmar que o gateway e os microsserviços respondiam adequadamente com códigos HTTP `403 Forbidden` ou `401 Unauthorized` de forma 100% determinística.

---

## 8. Artefatos de Design e Engenharia Cruciais

Ao longo dos meses de desenvolvimento, três documentos foram fundamentais para evitar desvios lógicos e manter a coesão do ecossistema:

1.  **`docs/CONTRATOS_API.md` (Catálogo de Contratos HTTP):**
    Foi a principal especificação entre o Frontend/BFF e as APIs downstream. Ao definir de antemão as rotas exatas, payloads JSON esperados e estruturas de erro (como `422 Unprocessable Entity`), impediu-se que mudanças nas APIs de domínio quebrassem a interface visual.
2.  **`docs/ARQUITETURA_GERAL.md` (Desenho C4 e Fluxo de Autenticação):**
    Documentou formalmente como a segurança de borda funcionava. Esclarecer o fluxo em que o BFF valida a sessão via Cookies e gera um token JWT de rede interna assinado com o claim do tenant (`tenant`) foi vital para garantir que nenhum microsserviço downstream acessasse dados de outro tenant acidentalmente.
3.  **`docs/FLUXOS_DE_TRABALHO.md` (Mapeamento dos Processos):**
    A fábrica de software possui regras severas no chão de fábrica (por exemplo, uma Ordem de Produção não pode ser liberada sem um Work Center ativo e uma BOM aprovada com status ativo). Ter esse mapeamento sequencial e visual impediu a inserção de inconsistências lógicas no código.

---

## 9. Análise Retrospectiva e Mudança de Postura Arquitetural

Se fôssemos reiniciar o desenvolvimento do sistema hoje, sob a perspectiva da experiência adquirida, implementaríamos as seguintes alterações:

### Mudança de Postura Gerencial
Iniciaria a automação dos testes de integração de API (nível HTTP) logo no início da Passada 1 (IAM & Tenancy). Depender apenas de validações de logs no console nas fases iniciais causou perda de produtividade. Uma cobertura de testes de integração nas interfaces de porta (*Ports*) teria evitado regressões sutis que só foram percebidas tardiamente nas suítes Playwright de front-end.

### Ajuste Tecnológico e Arquitetural
Substituiria o Entity Framework Core por um micro-ORM (como **Dapper**) nas APIs de leitura e relatórios (como no microsserviço de dashboards e no fluxo de consulta pública de despachos). O EF Core introduz complexidade e overhead al mapear grafos de entidades para consultas simples de tabelas de consulta rápida. A arquitetura hexagonal suporta essa alteração facilmente: bastaria criar um adaptador de repositório de leitura baseado em Dapper sem alterar o domínio.

---

## 10. Contribuição para o Amadurecimento Profissional

O desenvolvimento solo de um ecossistema complexo como o **Rail-Factory-Fork** constitui um divisor de águas na formação de um engenheiro de software:

*   **Domínio de Engenharia Distribuída de Ponta a Ponta:** Deixa de ser um conhecimento puramente teórico e torna-se prático o entendimento sobre como orquestrar múltiplos microsserviços, gerenciar segredos de banco de dados, desenhar topologias idempotentes de RabbitMQ, e implementar protocolos robustos de autenticação unificada (Cookies, CSRF e tokens JWT de rede interna).
*   **Valorização da Disciplina "First-Time Quality" (FTQ):** A assume-se o compromisso de que boas práticas de código (como o uso de Value Objects no domínio para evitar *primitive obsession* e a garantia de guards de estado em transições críticas de entidades) não são preciosismo acadêmico. São recursos essenciais de engenharia que viabilizam a evolução contínua do software sem gerar quebras de produção.
*   **Perfil Autônomo e Resolutivo:** Lidar sozinho com quirks graves de persistência de banco de dados, conflitos de concorrência e integrações complexas de segurança consolida a resiliência e a capacidade de conduzir análises de causa raiz focadas em dados empíricos, características essenciais de profissionais preparados para arquitetar sistemas de alta complexidade e missão crítica no mercado corporativo.
