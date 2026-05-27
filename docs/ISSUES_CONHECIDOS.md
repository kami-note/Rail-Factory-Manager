# Issues Conhecidos e Diagnósticos Técnicos

Este documento registra bugs conhecidos, diagnósticos realizados e seus status de resolução.

---

## 1. Duplicidade de Associação e Estoque (P2 — Association Workbench)

**Status:** ⚠️ Diagnosticado — Fix pendente de validação  
**Área:** Frontend (`AssociationWorkbenchPage.tsx`) + Supply Chain API + Inventory API

### 1.1 Sintoma
Dois produtos diferentes ficam associados ao mesmo SKU interno, gerando saldos consolidados no inventário, mas mantendo algumas informações fiscais separadas.

### 1.2 Causa Raiz: Frontend (Persistência de Estado React)

**Arquivo:** `src/RailFactory.Frontend/App/src/features/supply-chain/components/AssociationWorkbenchPage.tsx`

O componente `DecisionPanel` possui estado interno (`useState`) para o formulário de criação de material. Quando o operador resolve o item A e avança automaticamente para o item B, o React **não desmonta** o `DecisionPanel` — apenas atualiza a prop `item`. O estado interno (incluindo `formData.materialCode`) fica do item A, mas o formulário exibe o item B.

**Resultado:** O operador pode submeter o SKU do Produto A para o Produto B sem perceber.

**Fix recomendado:** Forçar remount do `DecisionPanel` ao trocar de item, usando `key={selectedItem.itemId}` na prop do componente:
```tsx
// Fix: adicionar key para forçar remount ao trocar de item
<DecisionPanel
  key={selectedItem.itemId}  // ← isso garante reset do estado interno
  tenantCode={tenantCode}
  receiptId={selectedReceiptId!}
  item={selectedItem}
  onSuccess={handleDecisionSuccess}
/>
```

### 1.3 Propagação no Backend: Tolerância a Conflitos (Supply Chain)

**Arquivos:**
- `src/RailFactory.SupplyChain.Api/Application/Receiving/CreateMaterialAndAssociate.cs`
- `src/RailFactory.SupplyChain.Api/Infrastructure/Integration/InventoryMaterialService.cs`

O `InventoryMaterialService` intercept o `409 Conflict` do Inventory e busca o material existente para manter idempotência. Isso faz o Supply Chain mapear silenciosamente o Produto B para o SKU já criado do Produto A.

### 1.4 Consolidação no Inventário

**Arquivo:** `src/RailFactory.Inventory.Api/Application/Balances/CreatePendingBalance.cs`

Com ambos os itens mapeados para o mesmo SKU, o Inventory cria **dois registros de saldo** com o mesmo `MaterialCode`:
- Saldo 1: `SKU-001`, Origem: `ReceiptId:ItemId_ProdutoA`
- Saldo 2: `SKU-001`, Origem: `ReceiptId:ItemId_ProdutoB`

### 1.5 Fluxo Completo do Erro

```
1. Frontend → Operador associa Produto A → Define SKU-001. Estado do form não limpa. Avança para B.
2. Frontend → Formulário do Produto B envia requisição com SKU-001.
3. Supply Chain → Tenta criar SKU-001 no Inventory → recebe 409 Conflict.
4. Supply Chain → Tolera o conflito, recupera SKU-001 existente, mapeia Produto B → SKU-001.
5. Supply Chain → Nota liberada. Eventos para Produto A (SKU-001) e Produto B (SKU-001) emitidos.
6. Inventory → Cria dois saldos físicos distintos para o mesmo SKU-001.
```

---

## 2. Loop de Sessão (Ambiente Ngrok)

**Status:** ✅ Contornado via Dev Bypass — Google OAuth ainda requer investigação em produção  
**Área:** BFF + Gateway (YARP) + IAM API + Cookie Auth

### 2.1 Sintoma
Ao acessar via Ngrok (`https://*.ngrok-free.app`), o usuário completa o Google OAuth com sucesso, mas qualquer chamada ao IAM via BFF retorna **HTTP 401 Unauthorized**, causando loop de "Sessão Expirada".

### 2.2 Arquitetura do Fluxo

```
Browser (Ngrok URL) → Ngrok Tunnel → BFF (Port 5082) → Gateway (YARP) → IAM API
```
- **Sessão:** Cookie-based auth.
- **Downstream:** BFF injeta JWT Interno.

### 2.3 Correções Já Aplicadas

| Ação | Objetivo |
|---|---|
| `CookieSecurePolicy` de `Always` → `None` em Development | Evitar descarte de cookie HTTPS→HTTP |
| `SameSiteMode` de `Lax` → `Unspecified` no IAM | Compatibilidade com túnel |
| Removido prefixo `__Host-` dos cookies | Prefixo exige HTTPS estrito |
| Script `scripts/deep-clean.sh` criado | Eliminar cache de DLLs antigas |
| `FrontendAuthSessionEndpoint.cs` repassa headers `X-Forwarded-*` | IAM recebe domínio real do Ngrok |
| `InternalTokenTenantBindingMiddleware` desativado em Development | Evitar rejeição por inconsistência de tenant |

### 2.4 Dev Bypass (Workaround para Testes)

Implementado header `X-Dev-User` no BFF que, quando `IsDevelopment() == true`, emite um Internal JWT assinado diretamente sem passar pelo Google OAuth nem pelo IAM:

- `GET /api/iam/auth/session` com header `X-Dev-User: email@exemplo.com` → sessão autenticada fake com `SystemPermissions.All()`.
- O header é removido antes de chegar a qualquer microserviço downstream (`StripSensitiveProxyHeaders`).
- Usado para testes e automação via Playwright sem necessidade de Google SSO.

**Arquivos relevantes:**
- `src/RailFactory.Frontend/Api/FrontendAuthSessionEndpoint.cs`
- `src/RailFactory.Frontend/Api/FrontendEndpoints.cs`

### 2.5 Suspeitas Pendentes (OAuth real)

1. **Mismatch de Host no Cookie:** ASP.NET pode emitir o cookie para `localhost` enquanto o browser está em `ngrok-free.app`. Verificar se `Cookie.Domain` precisa ser ignorado explicitamente.
2. **YARP Protocol Hijacking:** Gateway pode sobrescrever `X-Forwarded-Proto` de forma agressiva, confundindo o middleware de auth do IAM.
3. **Data Protection Key Mismatch:** Se IAM e BFF não compartilharem as Data Protection Keys (containers separados no Aspire), o BFF não consegue ler o cookie emitido pelo IAM.
4. **Vite Proxy:** Dev server Vite (porta 5082) pode remover headers de segurança antes de passar ao BFF .NET.

### 2.6 Arquivos para Auditoria Futura

- `src/RailFactory.Iam.Api/Infrastructure/IamHostingExtensions.cs`
- `src/RailFactory.Frontend/Infrastructure/FrontendHostingExtensions.cs`
- `src/RailFactory.Frontend/Api/FrontendAuthSessionEndpoint.cs`
- `src/RailFactory.ServiceDefaults/Identity/InternalTokenTenantBindingMiddleware.cs`
- `src/RailFactory.Gateway/appsettings.json` (Rotas YARP)

---

## 3. DbUpdateConcurrencyException ao Adicionar Item a BOM (EF Core 10 + Npgsql 10)

**Status:** ✅ Resolvido via Raw SQL bypass  
**Área:** `RailFactory.Production.Api` — `AddBomItem` use case  
**Data de resolução:** 2026-05-27

### 3.1 Sintoma

`POST /api/production/boms/{id}/items` retornava HTTP 500 com:

```
DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s),
but actually affected 0 row(s).
```

O item nunca era persistido no banco.

### 3.2 Causa Raiz

EF Core 10 + Npgsql 10 infere `ValueGeneratedOnAdd()` por convenção para PKs do tipo `Guid`. Quando um novo `BomItem` com `Id = Guid.NewGuid()` (não-zero) é adicionado à coleção `_items` do agregado `BillOfMaterials` (que já estava rastreada pelo contexto), o change tracker trata a entidade como `Unchanged` — não como `Added` — pois interpreta que um Guid não-zero significa que o registro já existe no banco.

`SaveChanges()` então gera `UPDATE bom_items SET ... WHERE Id = <novo-guid>`, que afeta 0 linhas (o registro não existe) → `DbUpdateConcurrencyException`.

### 3.3 Fix Aplicado

Bypass completo do EF Core change tracking para esta operação usando raw SQL parametrizado via `context.Database.ExecuteSqlAsync`:

**`BillOfMaterials.AddItem()`** — alterado para retornar o `BomItem` criado.

**`IBomRepository`** — adicionado `AddItemDirectAsync(Guid bomId, BomItem item, DateTimeOffset bomUpdatedAt, CancellationToken ct)`.

**`PostgresBomRepository.AddItemDirectAsync()`** — executa dois SQLs raw:
```sql
INSERT INTO bom_items ("Id", "BomId", "MaterialCode", "Quantity", "UnitOfMeasure")
VALUES ({item.Id}, {bomId}, {item.MaterialCode.Value}, {item.Quantity}, {item.UnitOfMeasure})

UPDATE boms SET "UpdatedAt" = {bomUpdatedAt} WHERE "Id" = {bomId}
```

**`AddBomItem.ExecuteAsync()`** — chama `AddItemDirectAsync` em vez de `SaveChangesAsync`.

### 3.4 Arquivos Modificados

| Arquivo | Mudança |
|---|---|
| `src/RailFactory.Production.Api/Domain/BillOfMaterials.cs` | `AddItem()` retorna `BomItem` em vez de `void` |
| `src/RailFactory.Production.Api/Application/Ports/IBomRepository.cs` | `AddItemDirectAsync` adicionado à porta |
| `src/RailFactory.Production.Api/Infrastructure/Persistence/PostgresBomRepository.cs` | Implementação raw SQL de `AddItemDirectAsync` |
| `src/RailFactory.Production.Api/Application/Boms/AddBomItem.cs` | Usa `AddItemDirectAsync` em vez de `SaveChangesAsync` |

### 3.5 Nota de Manutenção

Se no futuro a stack for migrada para EF Core + Npgsql com `ValueGeneratedNever()` explícito no `BomItem.Id`, o raw SQL bypass pode ser substituído pelo path normal com `context.BomItems.Add(item)` + `SaveChangesAsync()`. A causa raiz é a convenção implícita de `ValueGeneratedOnAdd`, não o EF Core em si.

---

*Última atualização: 2026-05-27. Adicione novos issues com título, status, causa raiz e arquivos relevantes.*
