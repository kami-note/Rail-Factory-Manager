# Sistema de RBAC (Role-Based Access Control)

Este documento descreve a arquitetura, o modelo de dados e o fluxo de execução do sistema de Controle de Acesso Baseado em Funções (RBAC) do Rail-Factory Fork.

## 1. Visão Geral
O sistema de RBAC é **multitenant** e **hierárquico**. Ele permite definir papéis (Roles) que contêm permissões atômicas e outros papéis (Composite Roles). O sistema segue o princípio de **Defesa em Profundidade**, com validações na UI, no BFF e nos Microserviços.

---

## 2. Implementação Técnica (End-to-End)

### 2.1 Criação de Funções (Roles)
A criação de funções ocorre no microserviço `IAM`.
- **Arquivo**: `src/RailFactory.Iam.Api/Application/Auth/RoleManagement.cs`
- **Classe**: `CreateTenantRole`
- **Funcionamento**: Recebe um nome, descrição e listas de permissões (`string`) e IDs de funções filhas (`Guid`). Persiste no banco de dados do tenant.

### 2.2 Atribuição de Funções a Usuários
Vincula um usuário existente a uma função definida no tenant.
- **Arquivo**: `src/RailFactory.Iam.Api/Application/Auth/AssignRoleToUser.cs`
- **Classe**: `AssignRoleToUser`
- **Funcionamento**: 
  1. Localiza o usuário local pelo e-mail.
  2. Valida a existência da Role no tenant.
  3. Persiste o vínculo e **invalida o cache de permissões** do Redis para garantir que a mudança seja aplicada no próximo login/refresh de sessão.

### 2.3 Definição de Permissões Atômicas
As permissões são constantes estáticas compartilhadas.
- **Backend (C#)**: `src/RailFactory.BuildingBlocks/Auth/SystemPermissions.cs`
- **Frontend (TS)**: `src/RailFactory.Frontend/App/src/shared/types/permissions.ts`
- **Mandato**: Ambos os arquivos devem ser mantidos em sincronia manual para garantir a integridade da comunicação.

---

## 3. Verificação de Acesso e Hardening

A verificação ocorre de forma multinível para garantir segurança total.

### 3.1 Proteção de Endpoints de Microserviços (JWT Interno)
Utilizamos o **Internal JWT** propagado pelo BFF.
- **Arquivo**: `src/RailFactory.ServiceDefaults/Identity/PermissionAuthorizationExtensions.cs`
- **Método**: `.RequirePermission(string permission)`
- **Lógica**: O `PermissionAuthorizationHandler.cs` valida os claims de `permission` dentro do JWT Interno.

### 3.2 Hardening do BFF (Endpoints Locais)
Endpoints do BFF que não passam pelo Proxy YARP (como uploads de arquivos) realizam verificação explícita.
- **Arquivos**: 
  - `src/RailFactory.Frontend/Api/MaterialImageUploadEndpoint.cs` (Exige `inventory.write`)
  - `src/RailFactory.Frontend/Api/MaterialImageServingEndpoint.cs` (Exige `inventory.read`)
- **Padrão**: Após recuperar a sessão do IAM, o código verifica se `session.User.Permissions.Contains(...)`.

### 3.3 Verificação Programática
- **Contrato**: `IUserContext` em `src/RailFactory.ServiceDefaults/IUserContext.cs`
- **Implementação**: `UserContextAccessor.cs` permite extrair a identidade do usuário a partir dos claims para auditoria e lógica de domínio.

---

## 4. Fluxo de Vida de uma Permissão
1. **Definição**: Adicione a constante em `SystemPermissions.cs` (Backend) e `permissions.ts` (Frontend).
2. **Criação**: Use `CreateTenantRole` para criar uma Role.
3. **Atribuição**: Use `AssignRoleToUser` para dar essa Role a um usuário.
4. **Resolução**: No login, o IAM resolve a hierarquia e salva no Redis.
5. **Propagação**: O BFF injeta as permissões no JWT Interno para chamadas downstream.
6. **Bloqueio**: 
   - O Microserviço bloqueia via `RequirePermission`.
   - O BFF bloqueia chamadas locais (ex: imagens) via verificação de array de permissões.

---

## 5. UI (Frontend)
- **Hook de Permissões**: `usePermissions` em `src/features/auth/hooks/usePermissions.ts`.
- **Componente de Guarda**: `Authorized.tsx` permite envolver elementos de UI.
- **Navegação**: O `ProtectedDashboardLayout.tsx` filtra os itens de menu baseando-se no objeto `SystemPermissions`.

---
*Este documento é a especificação técnica definitiva do RBAC para o Rail-Factory Fork. Última atualização: 2026-05-16.*
