# Rail-Factory Fork: Documentação Oficial

Esta pasta centraliza toda a documentação do projeto. Organizada para **busca rápida** — cada documento cobre uma área exclusiva, sem redundância.

---

## 🧭 Ponto de Entrada (Ler primeiro)

| Documento | O que responde |
|---|---|
| **[CONTEXTO_ATUAL.md](./CONTEXTO_ATUAL.md)** | Estado real do código: o que está de pé, o que está pendente, próxima task. |
| **[PLANO_DE_TASKS.md](./PLANO_DE_TASKS.md)** | Backlog executável com critérios de aceite por passada (P0–P10). |

---

## 🏗️ Arquitetura e Engenharia

| Documento | O que responde |
|---|---|
| **[ARQUITETURA_GERAL.md](./ARQUITETURA_GERAL.md)** | Visão C4, diagramas de dependência, protocolos elite, contratos cross-domain, UoM, fluxos de sequência. |
| **[CONTRATOS_API.md](./CONTRATOS_API.md)** | Contratos HTTP reais: headers globais, endpoints por serviço, payloads e erros. |
| **[RBAC_SYSTEM.md](./RBAC_SYSTEM.md)** | Arquitetura do RBAC: roles, permissões, fluxo de vida, arquivos-chave no código. |
| **[TENANT_ONBOARDING_STRATEGY.md](./TENANT_ONBOARDING_STRATEGY.md)** | Como criar e validar um novo tenant (checklist, critérios de aceite). |

---

## 📋 Requisitos e Negócio

| Documento | O que responde |
|---|---|
| **[REQUISITOS.md](./REQUISITOS.md)** | Requisitos canônicos do PDF (RF/NF/RN) + requisitos derivados (RD). |
| **[ANALISE_REQUISITOS_E_PASSADAS.md](./ANALISE_REQUISITOS_E_PASSADAS.md)** | Matriz de mapeamento: qual requisito entra em qual passada e por quê. |
| **[FLUXOS_DE_TRABALHO.md](./technical/FLUXOS_DE_TRABALHO.md)** | Mapeamento dos principais fluxos de trabalho (Recebimento, Conferência, Produção, Expedição, Webhooks, Auditoria). |

---

## 🏗️ Documentação Técnica Adicional

| Documento | O que responde |
|---|---|
| **[DOCUMENTACAO_TECNICA.md](./technical/DOCUMENTACAO_TECNICA.md)** | Guia de engenharia, padrões de código de microsserviços e diretrizes do sistema. |
| **[ENTREGA_4_IAM_MESSAGERIA_ORQUESTRACAO.md](./technical/ENTREGA_4_IAM_MESSAGERIA_ORQUESTRACAO.md)** | Relatório técnico de Identidade (IAM/RBAC), Mensageria (Outbox/RabbitMQ) e BFF/Gateway. |

---

## 📊 Diagramas e Engenharia Visual

| Documento | O que responde |
|---|---|
| **[DIAGRAMAS.md](./diagrams/DIAGRAMAS.md)** | Inventário geral de diagramas, fluxogramas, tabelas ER e BPMN. |
| **[Pasta Principal](./diagrams/principal/)** | Repositório de arquivos PlantUML (`.puml`) e diagramas renderizados (`.png`). |

---

## 📖 Manuais, Guias e Simulador de Telas

| Documento | O que responde |
|---|---|
| **[MANUAL_DO_USUARIO.md](./manuals/MANUAL_DO_USUARIO.md)** | Guia operacional completo com capturas de tela reais formatadas. |
| **[copiar_manual_usuario.html](./manuals/copiar_manual_usuario.html)** | Página HTML compilada (Base64) para cópia direta e formatação automática no Word/Docs. |
| **[wireframes.html](./wireframes/wireframes.html)** | Simulador local interativo off-line para navegação entre telas do sistema. |
| **[copiar_documento.html](./wireframes/copiar_documento.html)** | Página HTML compilada com as 21 telas do simulador em Base64 para documentação simplificada. |

---

## 🎓 Acadêmico (TCC & Entregas)

| Documento | O que responde |
|---|---|
| **[TCC_RELATORIO_DESENVOLVIMENTO.md](./academic/TCC_RELATORIO_DESENVOLVIMENTO.md)** | Relatório consolidado das passadas e desenvolvimento do projeto do TCC. |
| **[RESPOSTAS_TCC.md](./academic/RESPOSTAS_TCC.md)** | Caderno com perguntas e respostas acadêmicas preparadas para a defesa do TCC. |
| **[RESPOSTAS_CADERNO.md](./academic/RESPOSTAS_CADERNO.md)** | Caderno de respostas e atividades teóricas/práticas do TCC. |
| **[docfinal-7.pdf](./academic/docfinal-7.pdf)** | Arquivo PDF final oficial do relatório acadêmico de TCC. |
| **[rascunho-dde-ers/](./academic/rascunho-dde-ers/)** | Rascunho inicial do DDE + ERS com detalhes estruturais. |

---

## 🎨 Frontend

| Documento | O que responde |
|---|---|
| **[DESIGN_STYLE_GUIDE.md](./DESIGN_STYLE_GUIDE.md)** | Paleta de cores, tipografia, iconografia, componentes compartilhados, padrões de UX. |
| **[../src/RailFactory.Frontend/GEMINI.md](../src/RailFactory.Frontend/GEMINI.md)** | Arquitetura Feature-Sliced, regras de dependência, shared kernel. |

---

## 🛠️ Regras de Desenvolvimento

| Documento | O que responde |
|---|---|
| **[../GEMINI.md](../GEMINI.md)** | Mandatos de engenharia: documentação, FTQ, estilo, protocolos elite, dead code. |
| **[../AGENTS.md](../AGENTS.md)** | Guia rápido de onboarding para desenvolvedores (aponta para GEMINI.md). |

---

## 🐛 Issues e Diagnósticos

| Documento | O que responde |
|---|---|
| **[ISSUES_CONHECIDOS.md](./ISSUES_CONHECIDOS.md)** | Bugs diagnosticados (duplicidade de associação, loop de sessão Ngrok), causa raiz e arquivos relevantes. |

---

## 📌 Guia de Busca Rápida

| Pergunta | Onde buscar |
|---|---|
| Qual é a tarefa atual? | `CONTEXTO_ATUAL.md` |
| Qual endpoint implementar? | `CONTRATOS_API.md` |
| Como o sistema autentica? | `ARQUITETURA_GERAL.md` §7 + `RBAC_SYSTEM.md` |
| Quais são os requisitos RF-XX? | `REQUISITOS.md` |
| Em qual passada entra RF-XX? | `ANALISE_REQUISITOS_E_PASSADAS.md` §2 |
| Como fazer um componente UI? | `DESIGN_STYLE_GUIDE.md` |
| Padrão de código (C#/TS)? | `GEMINI.md` |
| Estrutura de pastas do frontend? | `src/RailFactory.Frontend/GEMINI.md` |
| Como adicionar um tenant? | `TENANT_ONBOARDING_STRATEGY.md` |
| Há algum bug conhecido? | `ISSUES_CONHECIDOS.md` |
| Como funciona o fluxo de trabalho de X? | `./technical/FLUXOS_DE_TRABALHO.md` |
| Como operar o sistema ou ver capturas de tela? | `./manuals/MANUAL_DO_USUARIO.md` |
| Como visualizar ou copiar as telas do simulador? | `./wireframes/wireframes.html` ou `./wireframes/copiar_documento.html` |
| Onde estão as respostas do caderno do TCC? | `./academic/RESPOSTAS_CADERNO.md` ou `./academic/RESPOSTAS_TCC.md` |
