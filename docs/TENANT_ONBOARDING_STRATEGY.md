# Estrategia Para Criacao De Novo Tenant

## Objetivo Deste Documento

Este documento define, de forma clara e executavel, como criar um novo tenant no Rail-Factory sem quebrar isolamento de dados, autenticacao e roteamento.

Foco atual: concluir `P1.5 - Multi-Tenancy Validation` com o tenant `acme`.

## Problema Real

Criar tenant nao e apenas inserir um registro na tabela `tenants`.

No estado atual do projeto, um tenant funcional depende de 4 frentes ao mesmo tempo:

1. Infraestrutura (bancos por servico por tenant).
2. Catalogo de tenant (metadados + connection strings corretas).
3. Fluxo de autenticacao (BFF/Gateway/IAM com tenant dinamico).
4. UI (usuario escolhe tenant antes de autenticar).

Quando uma dessas frentes fica incompleta, o sistema aparenta funcionar em `dev`, mas falha em tenants novos.

## Tese

A criacao de tenant deve ser tratada como um fluxo de onboarding verificavel, com checklist e evidencias objetivas.

Sem isso, o projeto acumula risco de:

- erro de conexao por string inconsistente;
- login funcionando em um tenant e quebrando em outro;
- fallback acidental para `dev`;
- risco de vazamento cruzado entre tenants.

## Como Resolver

### 1. Padronizar provisionamento no AppHost

Provisionar explicitamente os bancos do tenant novo, seguindo o mesmo padrao do `dev`:

- `tenant-acme-iamdb`
- `tenant-acme-supplychaindb`
- `tenant-acme-inventorydb`
- `tenant-acme-productiondb`

Regra: sem provisionamento completo, onboarding nao comeca.

### 2. Seed idempotente no Tenant Catalog

Registrar `acme` no catalogo com:

- `code`;
- status ativo;
- locale/timezone;
- mapa completo de `connection_strings` para os servicos tenant-aware.

Regra: catalogo e a fonte da verdade de resolucao. Nao pode existir tenant "parcial".

### 3. Tenant dinamico no fluxo de auth

Garantir que BFF/UI usem o tenant selecionado pelo usuario em:

- inicio do OAuth;
- callback/finalizacao;
- consulta de sessao;
- chamadas autenticadas seguintes.

Regra: proibido hardcode de `dev` em fluxo autenticado.

### 4. Selecao de tenant no Frontend

Adicionar tela (ou modal) para informar tenant antes do login e persistir em `localStorage`.

Regra: usuario sempre sabe em qual tenant esta entrando.

## Plano Executavel (P1.5)

1. Provisionar bancos `tenant-acme-*` no AppHost.
2. Criar seed idempotente de `acme` no Tenant Catalog.
3. Implementar seletor de tenant na entrada da UI.
4. Persistir tenant selecionado e recarregar automaticamente.
5. Tornar `tenantCode` dinamico em `App.tsx` e no auth flow.
6. Executar build/tests da fronteira alterada.
7. Executar smoke tests de `dev` e `acme`.
8. Registrar evidencias em `docs/CONTEXTO_ATUAL.md` e atualizar checklist em `docs/PLANO_DE_TASKS.md`.

## Evidencias Minimas Para Considerar Concluido

### Catalogo

- `GET /api/tenancy/tenants/acme` retorna `200` com metadados corretos.

### Tenant-aware

- `/info` sem `X-Tenant-Code` retorna `400` (`tenant.code_required`).
- `/info` com `X-Tenant-Code: acme` retorna `200`.
- `/info` com tenant inexistente retorna `404` (`tenant.not_found`).

### Auth

- Login Google inicia e finaliza com `tenantCode=acme`.
- Sessao retornada pelo BFF reflete tenant correto.

### Isolamento

- Nao existe fallback silencioso para `dev` quando a requisicao usa `acme`.
- Operacoes de `acme` gravam/leem apenas bancos `tenant-acme-*`.

## Criterio De Pronto

A task so pode ser marcada como concluida quando:

- provisioning, catalogo, auth e UI estiverem completos;
- smoke tests de `dev` e `acme` estiverem verdes;
- evidencias estiverem documentadas;
- checklist de `P1.5` estiver atualizado.

## Conclusao

A dificuldade de criar um novo tenant nao esta no volume de codigo, mas na coordenacao entre fronteiras. A forma correta de reduzir risco e adotar onboarding padronizado com validacao objetiva, iniciando por `acme` em `P1.5`.
