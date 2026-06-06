# Backlog — Experiência dos Operadores (Logistics)

## Contexto
O sistema de logística tem o fluxo técnico funcionando (ordens, despachos, NF-e),
mas a experiência dos operadores está incompleta. Este arquivo rastreia o que falta
para cada perfil: Separador, Embalador, Despachante e Motorista.

---

## Prioridade 1 — Conferência com Checklist (risco operacional)

**Problema:** O botão "Conferir" no despacho dispara um PUT sem nenhuma verificação.
O despachante pode conferir sem checar nenhum item — o caminhão sai com itens errados
e o sistema registra como "conferido".

**O que fazer:**
- [ ] Criar `ConferenceModal` — abre ao clicar "Conferir" em vez de disparar direto
- [ ] Modal lista todos os itens do despacho (materialCode, qtd, UOM, peso)
- [ ] Cada item tem checkbox; botão "Confirmar Conferência" só habilita quando todos marcados
- [ ] Ao confirmar, dispara o PUT `/conference` e fecha o modal

**Arquivos afetados:**
- `src/features/logistics/components/DispatchesPage.tsx`
- `src/features/logistics/components/ConferenceModal.tsx` (novo)

---

## Prioridade 2 — Detalhe da Ordem (uso diário dos separadores/embaladores)

**Problema:** A tabela de ordens mostra apenas a contagem de itens (ex: `3`).
Clicar na linha não faz nada. O separador não sabe o que buscar no estoque.

**O que fazer:**
- [ ] Criar `ShipmentOrderDetailPanel` — painel lateral (Drawer) que abre ao clicar numa ordem
- [ ] Painel mostra: cabeçalho da ordem, destinatário, status atual
- [ ] Lista de itens com: código, nome, quantidade, UOM, peso (kg), volume (m³)
- [ ] Botões de ação contextuais no painel (Separar / Embalar / Pronto) conforme status

**Arquivos afetados:**
- `src/features/logistics/components/ShipmentOrdersPage.tsx`
- `src/features/logistics/components/ShipmentOrderDetailPanel.tsx` (novo)

---

## Prioridade 3 — Filtros por Status (fila de trabalho)

**Problema:** `ShipmentOrdersPage` joga todas as ordens juntas.
O separador vê Draft, Picking, Packing, Shipped misturados — não sabe o que é dele agora.

**O que fazer:**
- [ ] Adicionar chips de filtro rápido na `ShipmentOrdersPage`:
  `Todos | Rascunho | Separação | Embalagem | Pronto p/ Despacho | Despachado`
- [ ] Filtro selecionado persiste enquanto a página estiver aberta
- [ ] Contagem por status ao lado de cada chip (ex: "Separação (4)")

**Arquivos afetados:**
- `src/features/logistics/components/ShipmentOrdersPage.tsx`

---

## Prioridade 4 — Romaneio para Impressão (motorista)

**Problema:** O motorista usa papel impresso. Hoje não há como gerar esse documento.

**O que fazer:**
- [ ] Botão "Imprimir Romaneio" na linha do despacho em `DispatchesPage`
- [ ] Criar `DispatchPrintView` — componente de impressão (`@media print`)
- [ ] Conteúdo: número do despacho, tracking code, data, transportadora, veículo, motorista
- [ ] Destinatário: nome, CNPJ, endereço completo
- [ ] Lista de itens: código, descrição, qtd, UOM, peso
- [ ] Rodapé: chave NF-e (se emitida), assinatura do recebedor

**Arquivos afetados:**
- `src/features/logistics/components/DispatchesPage.tsx`
- `src/features/logistics/components/DispatchPrintView.tsx` (novo)

---

## Melhorias menores (após as 4 acima)

- [ ] **Feedback visual de loading por ação** — spinner no botão "Despachar" / "Entregar"
        enquanto o request está em voo (hoje o botão some sem indicação)
- [ ] **Paginação na tabela de despachos** — aviso ou paginação real quando há > 50 registros
- [ ] **Montagem condicional do `CreateDispatchModal`** — modal faz 4 fetches ao montar
        mesmo fechado; usar `{createOpen && <CreateDispatchModal />}` para montar só quando aberto

---

## Notas

- Motorista não usa sistema — usa romaneio impresso (Prioridade 4)
- Conferência (Prioridade 1) é o maior risco operacional: endereça antes de qualquer outra coisa
- O backend não precisa de mudanças para nenhum desses itens —
  todos os dados necessários já existem na API
