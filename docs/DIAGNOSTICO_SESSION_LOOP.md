# Relatório de Diagnóstico: Loop de Sessão (Ambiente Ngrok)

Este documento detalha o estado técnico, as tentativas de correção e os pontos de falha identificados no loop infinito de "Sessão Expirada" ao acessar o sistema via túnel Ngrok.

## 1. O Problema
Ao acessar o sistema pela URL do Ngrok (`https://*.ngrok-free.app`), o usuário completa o login no Google OAuth com sucesso, mas ao retornar ao Frontend, qualquer chamada ao microserviço de IAM via BFF resulta em **HTTP 401 Unauthorized**. Isso dispara a mensagem "Sua sessão expirou" no React, forçando um novo login.

## 2. Arquitetura de Fluxo (Ngrok Context)
`Browser (Ngrok URL)` -> `Ngrok Tunnel` -> `BFF (Port 5082)` -> `Gateway (YARP)` -> `IAM API`

- **Sessão**: Baseada em Cookies (Cookie-based auth).
- **Comunicação Downstream**: BFF injeta um JWT Interno para os microserviços.

## 3. O que já foi executado (Histórico de Correções)

### A. Políticas de Cookie (IAM & Frontend)
- **Ação**: Alterado `CookieSecurePolicy` de `Always` para `None` em modo `Development`.
- **Ação**: Alterado `SameSiteMode` de `Lax` para `Unspecified` no IAM.
- **Ação**: Removido o prefixo `__Host-` dos cookies (que exige HTTPS).
- **Objetivo**: Evitar que o navegador descarte o cookie recebido via Ngrok (HTTPS) ao ser processado pelo backend local (HTTP).

### B. Limpeza e Build
- **Ação**: Criado script `scripts/deep-clean.sh` para remover todas as pastas `bin` e `obj`.
- **Ação**: Executado `npm install` no Frontend para garantir executáveis do Vite.
- **Objetivo**: Eliminar qualquer cache de DLLs antigas com políticas de segurança rígidas.

### C. Propagação de Headers (BFF)
- **Ação**: O endpoint `FrontendAuthSessionEndpoint.cs` foi alterado para repassar todos os headers `X-Forwarded-*` recebidos do Ngrok para o IAM.
- **Objetivo**: Fazer o IAM "acreditar" que a requisição está vindo do domínio do Ngrok e não do host interno `gateway`.

### D. Bypass de Segurança (ServiceDefaults)
- **Ação**: Desativado o `InternalTokenTenantBindingMiddleware` quando `environment.IsDevelopment()`.
- **Objetivo**: Impedir que o sistema expulse o usuário por inconsistência entre o Tenant do Token e o Host da URL (comum em túneis).

## 4. Suspeitas Atuais (Pontos para o Claude Code)

1. **Mismatch de Host no Cookie**: O ASP.NET Core pode estar emitindo o cookie para o domínio `localhost` enquanto o navegador está no domínio `ngrok-free.app`. Verifique se o `Cookie.Domain` precisa ser explicitamente ignorado ou ajustado.
2. **YARP Protocol Hijacking**: O Gateway (YARP) pode estar sobrescrevendo os headers `X-Forwarded-Proto` de forma agressiva, causando confusão no middleware de autenticação do IAM.
3. **Data Protection Key**: Como os serviços rodam em containers separados via Aspire, se o IAM e o BFF não compartilharem as chaves de proteção de dados (`AddDataProtection`), o BFF pode não conseguir ler o cookie que ele mesmo (ou o IAM) emitiu.
4. **Vite Proxy**: O servidor de desenvolvimento do Vite (porta 5082) pode estar removendo headers de segurança ou cookies antes de passá-los para o BFF .NET.

## 5. Arquivos Chave para Auditoria
- `src/RailFactory.Iam.Api/Infrastructure/IamHostingExtensions.cs`
- `src/RailFactory.Frontend/Infrastructure/FrontendHostingExtensions.cs`
- `src/RailFactory.Frontend/Api/FrontendAuthSessionEndpoint.cs`
- `src/RailFactory.ServiceDefaults/Identity/InternalTokenTenantBindingMiddleware.cs`
- `src/RailFactory.Gateway/appsettings.json` (Rotas YARP)

---
*Este relatório serve como handover técnico para diagnóstico profundo.*
