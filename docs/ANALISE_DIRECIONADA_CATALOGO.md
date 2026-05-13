# Pacote de Refatoração: Catálogo, Inbound e Rastreabilidade Operacional

## Objetivo Principal
Resolver a duplicação crônica de materiais causada por códigos de fornecedores (`cProd`), garantir a rastreabilidade total (física e de auditoria) e estruturar o catálogo para suportar de forma inteligente as áreas de Compras (Buy) e Produção (Make).

---

## Eixo 1: A Barreira de Recebimento (Supply Chain)

**1. Fim do Provisionamento Automático (JIT Provisioning)**
*   **Decisão:** O sistema não criará mais produtos automaticamente ao importar um XML de NF-e. A barreira ocorre no Supply Chain.
*   **Mecanismo:** Criação da entidade `SupplierMaterialMapping` (Tabela de De-Para), que traduz o código do fornecedor para o SKU interno oficial.

**2. Fluxo de Associação (Aprendizado)**
*   **Decisão:** Se a NF-e contiver um código desconhecido, o Recebimento fica travado no novo status `PendingAssociation`.
*   **UI:** Criação da tela "Inbox de Associação" no Frontend, onde o operador visualiza os itens órfãos e os vincula ao catálogo existente (ou cria o primeiro cadastro oficial).

**3. A Armadilha da Unidade de Medida (Fator de Conversão e Matemática Financeira)**
*   **Decisão:** A tabela de De-Para deve conter a Unidade do Fornecedor e o `ConversionFactor`.
*   **A Matemática:** Se o fornecedor vende em Caixa (CX) e o estoque é em Unidade (UN), o sistema **multiplica a quantidade** na entrada (garantindo o saldo físico real) e **obrigatoriamente divide o Preço Unitário (`vUnCom`)** pelo mesmo fator. Isso impede a inflação irreal do valor financeiro do estoque.

**4. Gestão de Mapeamentos e Correção de Erros (Unlink)**
*   **Decisão:** Para prevenir que um erro humano no De-Para corrompa todas as futuras compras daquele fornecedor, o Frontend terá uma área de "Gerenciamento de Fornecedores" dentro do Produto Oficial.
*   **Mecanismo:** O operador poderá Desvincular (Unlink) ou Corrigir um mapeamento errado, registrando a auditoria dessa correção.

---

## Eixo 2: A Verdadeira Unificação de Catálogo (Inventory)

**5. O Ajuste Contábil Seguro (Sem Apagar Histórico)**
*   **Decisão:** Não haverá "Delete" ou renomeação do passado. O `MergeMaterialCommand` executará um **Stock Out** (Saída) no produto duplicado e um **Stock In** (Entrada) no produto oficial. O produto duplicado recebe o status `Obsolete` e o ponteiro `ReplacedBy`.
*   **UI:** O produto `Obsolete` some imediatamente das buscas de NF-e e Produção.

**6. Preservação do Rastreio Físico (Lotes e Validades)**
*   **Decisão:** A transferência de saldos durante a unificação é obrigada a carregar os dados originais de Lote (Batch) e Data de Validade. Sem isso, a fábrica perde a capacidade de *recall*.

**7. Recálculo Financeiro (Custo Médio Ponderado)**
*   **Decisão:** O comando de mesclagem registrará o custo unitário das entradas anteriores, garantindo que o sistema recalcule corretamente o Custo Médio Ponderado do produto oficial.

**8. O Efeito Cascata Seguro (Versionamento de Engenharia)**
*   **Decisão:** A unificação dispara o evento `MaterialMergedEvent`.
    *   *Supply Chain:* Atualiza as linhas de Notas Fiscais que já haviam sido importadas com o código velho e aguardavam conferência.
    *   *Produção:* Ao invés de uma substituição silenciosa (que fere normas de engenharia), o módulo de Produção inativará a Receita (BOM v1) que usava o insumo obsoleto e **gerará automaticamente uma nova versão (BOM v2)** utilizando o produto oficial.

---

## Eixo 3: Estratégia de Suprimentos (Exibir o que é relevante)

**9. O Conceito de Origem (`ProcurementType`)**
*   **Decisão:** O cadastro de Material ganha a flag `ProcurementType` com os valores `Make` (Fabricado Internamente), `Buy` (Comprado de Terceiros) ou `MakeAndBuy` (Ambos).

**10. Telas Dinâmicas e Mutáveis (UI/UX)**
*   **Decisão:** O Frontend adapta a tela de Detalhes do Produto com base na Origem:
    *   **Se for Comprado (Buy):** Foca em Custos de Última Compra, Histórico de Preços, Mapeamentos de Fornecedores e oculta abas de fabricação.
    *   **Se for Fabricado (Make):** Foca na Árvore de Produto (BOM), Custos de Produção e Lead Time de fábrica, ocultando abas de compras externas.

**11. Homologação Automática de Fornecedores**
*   **Decisão:** Quando a associação de uma NF-e é feita, o sistema adiciona aquele fornecedor à lista de **Fornecedores Homologados/Conhecidos** na ficha técnica do Produto Oficial.

---

## Eixo 4: Auditoria, Rastreabilidade e Débito Técnico

**12. Rastreabilidade Completa no Banco de Dados**
*   **Decisão:** Extrair o ID do usuário logado (token JWT) e passá-lo para os comandos do domínio. Todas as entidades base (`Material`, `SupplierMaterialMapping`) ganham `CreatedBy` e `LastModifiedBy`. O ledger físico de estoque assina todas as movimentações com o responsável.

**13. A Linha do Tempo ("Quem Fez o Quê") na UI**
*   **Decisão:** Criação de uma aba visível de **"Activity Log / Histórico"** na ficha do material no Frontend, exibindo uma *timeline* auditável para o usuário final (Ex: "João unificou este produto com P123 em 10/05").

**14. Quitação de Débito Técnico (Task 2.8.2)**
*   **Decisão:** Garantir que o Nome do Fornecedor Original (`SupplierName`) seja permanentemente visível nas tabelas de saldos pendentes e no histórico do catálogo (Inventory UI), fechando o *gap* operacional de rastreabilidade visual.
