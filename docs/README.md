# Rail-Factory Fork: Documentação Oficial

Esta pasta organiza a reconstrução incremental do Rail-Factory. O fork mantém o escopo completo do produto original, mas prioriza a qualidade técnica, o isolamento multitenant e a integridade hexagonal.

## 🧭 Ponto de Entrada
- **[CONTEXTO_ATUAL.md](./CONTEXTO_ATUAL.md)**: **O documento mais importante.** Registra o estado real do código, marcos concluídos e a próxima tarefa imediata.
- **[PLANO_DE_TASKS.md](./PLANO_DE_TASKS.md)**: O backlog executável com critérios de aceite detalhados por etapa.

## 🏗️ Arquitetura e Engenharia
- **[ARQUITETURA_GERAL.md](./ARQUITETURA_GERAL.md)**: Define a visão alvo, padrões hexagonais, isolamento de tenants e protocolos de integração.
- **[TENANT_ONBOARDING_STRATEGY.md](./TENANT_ONBOARDING_STRATEGY.md)**: Guia para expansão e validação de isolamento multitenant.
- **[CONTRATOS_API.md](./CONTRATOS_API.md)**: Registro dos contratos HTTP reais consumidos pelo Frontend e Gateway.

## 📋 Requisitos e Negócio
- **[REQUISITOS.md](./REQUISITOS.md)**: Mapeamento canônico dos requisitos RF/NF/RN baseados no PDF original.
- **[ANALISE_REQUISITOS_E_PASSADAS.md](./ANALISE_REQUISITOS_E_PASSADAS.md)**: O roadmap de "Passadas" (P0 a Px) que define a ordem de construção.
- **[FUNCIONALIDADES.md](./FUNCIONALIDADES.md)**: Descrição das responsabilidades de cada domínio.

## 🛠️ Regras para Desenvolvedores e IAs
- **[GEMINI.md](../GEMINI.md)** (Raiz): Contém os **Mandatos de Engenharia Elite**, protocolos de prevenção de retrabalho e padrões de documentação obrigatórios.
- **[GRAFOS_DO_PROJETO.md](./GRAFOS_DO_PROJETO.md)**: Visão visual das dependências entre serviços.

---
*Nota: Documentos obsoletos ou redundantes foram removidos para garantir uma "Fonte Única de Verdade" (SSoT).*
