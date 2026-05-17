# Diagnóstico: Duplicidade de Associação e Estoque

Este documento detalha tecnicamente o problema reportado onde dois produtos diferentes são associados ao mesmo SKU interno, gerando saldos consolidados e duplicados no inventário, mantendo, no entanto, algumas informações originais das notas fiscais separadas.

## 1. Origem do Problema: Frontend (Persistência de Estado React)

**Arquivo:** `src/RailFactory.Frontend/App/src/features/supply-chain/components/AssociationWorkbenchPage.tsx`

O ciclo de erro se inicia na interface do usuário (UI). Quando o operador está na tela da Bancada de Associação ("Association Workbench"), ele seleciona um item da nota fiscal recebida para tomar uma decisão de associação (vincular a um existente ou criar um novo).

O componente responsável por exibir o formulário de decisão é o `DecisionPanel`:

```tsx
// Trecho do código atual (com erro)
{selectedItem ? (
  <DecisionPanel 
    tenantCode={tenantCode}
    receiptId={selectedReceiptId!}
    item={selectedItem}
    onSuccess={handleDecisionSuccess}
  />
) : ( ... )}
```

### O que acontece no React:
O componente `DecisionPanel` possui estado interno (usando `useState`) para gerenciar as abas (`tab`), a busca (`searchQuery`) e, principalmente, os dados do formulário de criação de um novo material no sub-componente `CreateMaterialForm` (como `formData.materialCode`, `formData.officialName`, etc.).

Quando o operador resolve a associação do primeiro item (ex: **Produto A**), a função `handleDecisionSuccess` é chamada. Esta função, por design, avança automaticamente a seleção para o próximo item pendente na lista (ex: **Produto B**). 

Ao alterar a propriedade `item` passada para o `DecisionPanel`, o React tenta otimizar a renderização. Como o componente pai (`DecisionPanel`) é o mesmo, o React apenas atualiza as propriedades, mas **não reinicia (desmonta e remonta) a árvore de componentes**. Consequentemente, todo o estado interno mantido pelos `useStates` (incluindo o `formData.materialCode` preenchido para o Produto A) permanece intacto.

**Efeito Prático:** O operador vê o formulário para o Produto B, mas o campo "SKU Interno" ainda está preenchido com o SKU que ele acabou de criar para o Produto A. Se ele não perceber e clicar em "Criar e Vincular", a requisição será enviada para o backend solicitando a criação e o vínculo do Produto B usando o mesmo SKU do Produto A.

## 2. Propagação no Backend: Tolerância a Conflitos (Supply Chain)

**Arquivos:** 
- `src/RailFactory.SupplyChain.Api/Application/Receiving/CreateMaterialAndAssociate.cs`
- `src/RailFactory.SupplyChain.Api/Infrastructure/Integration/InventoryMaterialService.cs`

Quando o frontend envia a requisição para associar o Produto B com o SKU do Produto A, o caso de uso `CreateMaterialAndAssociate` na API de Supply Chain é acionado.

A primeira ação deste caso de uso é chamar o microserviço de Inventário para garantir que o material existe:
```csharp
var material = await inventoryMaterialService.CreateMaterialAsync(materialInput, cancellationToken);
```

No `InventoryMaterialService`, se o microserviço de Inventário retornar um erro de conflito (`409 Conflict`), significando que o SKU (o SKU do Produto A) já existe, o serviço de integração intercepta esse erro e busca os metadados do material já existente para manter a **idempotência**:

```csharp
if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
{
    // Material already exists, try to fetch its metadata to maintain idempotency
    var existing = await GetMaterialsByCodesAsync(new[] { input.MaterialCode }, cancellationToken);
    if (existing.TryGetValue(input.MaterialCode, out var meta))
    {
        return meta;
    }
    // ...
}
```

**Efeito Prático:** O backend de Supply Chain permite a operação silenciosamente. Ele mapeia o Produto B da nota fiscal para o SKU interno existente do Produto A. O item da nota (Produto B) agora aponta para o `InternalMaterialCode` do Produto A no banco de dados da Supply Chain.

## 3. Consolidação no Inventário (A "Duplicidade")

**Arquivo:** `src/RailFactory.Inventory.Api/Application/Balances/CreatePendingBalance.cs`

Quando a nota fiscal inteira é finalmente "Liberada para Conferência" (ReleaseReceiptToConference), a Supply Chain emite eventos de integração (`supply.receipt_item_registered`) para cada item mapeado.

O microserviço de Inventário recebe esses eventos. Para cada evento, ele chama o `CreatePendingBalance`.

Como ambos os itens da nota fiscal (Produto A e Produto B) foram mapeados para o mesmo SKU (ex: "SKU-001"), o Inventário criará **dois registros de saldo separados (`InventoryBalance`)**, mas ambos atrelados ao mesmo `MaterialCode`.

Embora eles pareçam um único produto consolidado na tela de saldos totais (pois o total por SKU é somado), o sistema mantém os saldos físicos separados por causa da `SourceReference`, que guarda a origem de cada registro:
- Saldo 1: SKU-001, Origem: `ReceiptId:ItemId_ProdutoA`
- Saldo 2: SKU-001, Origem: `ReceiptId:ItemId_ProdutoB`

Esta é a razão pela qual "os dois produtos ficaram como apenas um" (mesmo SKU interno), mas "algumas informações ficaram" (os saldos possuem referências de origem, quantidades da nota e possivelmente preços unitários e NCMs originais distintos vindos do XML, que são preservados nos metadados do `InventoryBalance`).

## Resumo do Fluxo do Erro

1. **Frontend:** Operador associa Produto A -> Define SKU-001. Estado do form não limpa. Avança para Produto B.
2. **Frontend:** Formulário do Produto B envia requisição com SKU-001.
3. **Supply Chain API:** Tenta criar SKU-001 no Inventário. Toma um "Conflict" porque SKU-001 acabou de ser criado no passo 1.
4. **Supply Chain API:** A API tolera o conflito, recupera os dados do SKU-001 existente e mapeia o Produto B para o SKU-001.
5. **Supply Chain API:** A nota é liberada. Eventos para Produto A (SKU-001) e Produto B (SKU-001) são enviados.
6. **Inventory API:** Cria dois saldos físicos distintos para o mesmo SKU-001, um para cada item da nota original.