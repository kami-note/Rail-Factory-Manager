# Regras Para Agentes De IA

Estas regras valem para qualquer IA trabalhando neste repositorio.

## Prioridade Das Fontes

1. `docs/CONTEXTO_ATUAL.md`: estado real do codigo e proxima acao.
2. `docs/PLANO_DE_TASKS.md`: checklist executavel.
3. `docs/REGRAS_PARA_IAS.md`: regras de engenharia e documentacao.
4. `docs/ARQUITETURA_GERAL.md`: arquitetura alvo.
5. `docs/REQUISITOS.md`: requisitos canonicos.

Se houver conflito, nao inventar. Registrar a divergencia e corrigir o documento mais especifico.

## Antes De Implementar

- Ler `docs/CONTEXTO_ATUAL.md`.
- Confirmar em `docs/PLANO_DE_TASKS.md` qual task esta sendo trabalhada.
- Nao pular passadas.
- Nao implementar funcionalidade futura sem dependencia real do fluxo atual.
- Verificar se a mudanca pertence a Domain, Application, Infrastructure, Api, Gateway, BFF ou UI.

## Regras De Codigo

- Aplicar SOLID com rigor.
- Usar Arquitetura Hexagonal.
- Dominio nao referencia infraestrutura, framework web, banco, HTTP, fila, Aspire ou UI.
- Application orquestra casos de uso e depende de portas.
- Infrastructure implementa portas.
- Api adapta HTTP para Application.
- Gateway roteia; nao implementa regra de negocio.
- BFF cuida de sessao/cookie/CSRF/fachada; nao implementa regra de dominio.
- UI chama BFF, nunca servicos internos diretamente.

## Contexto E Documentacao

Toda mudanca relevante deve atualizar:

- `docs/CONTEXTO_ATUAL.md`, quando mudar o estado real do projeto;
- `docs/PLANO_DE_TASKS.md`, quando concluir ou abrir task;
- documentos de arquitetura/requisitos apenas quando houver decisao ou mudanca de direcao.

Nao marcar task como concluida sem build/teste ou criterio de aceite verificavel.

## Validacao Minima

- Rodar build/teste aplicavel.
- Se nao puder validar, registrar claramente o motivo.
- Nao esconder pendencia tecnica como concluida.
