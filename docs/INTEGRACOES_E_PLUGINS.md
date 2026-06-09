# Arquitetura de Integrações e Plugins (App Store)

**Status:** Visão Estratégica e Planejamento
**Última Atualização:** 2026-06-01

Este documento define a estratégia do **Rail-Factory** para se comunicar com o mundo externo. Adotaremos um modelo de **Plataforma (Integration Hub / App Store)**, onde o núcleo do ERP fornece as abstrações (Interfaces/Ports) e o usuário final (Tenant) escolhe qual "Plugin" deseja habilitar.

---

## 1. Princípios Arquiteturais (O Modelo App Store)

Para garantir que o Rail-Factory mantenha sua *Hexagonal Integrity* e não fique acoplado a um único fornecedor, a arquitetura de integrações seguirá o padrão **Strategy com Tenant Resolution**:

1. **Abstração:** O Domínio define portas puras (ex: `IPaymentGateway`, `IFiscalIssuer`).
2. **Tenant Settings:** A `Tenancy.Api` armazenará de forma segura (criptografada) as credenciais e a escolha do fornecedor de cada Tenant (ex: Tenant A usa Asaas; Tenant B usa Iugu).
3. **Resolução Dinâmica:** No momento da execução de um caso de uso, uma *Factory* injeta o Adapter correspondente à escolha do Tenant.

---

## 2. Catálogo Oficial de Plugins a Disponibilizar

Os plugins abaixo foram mapeados e homologados quanto à disponibilidade de APIs públicas e ambientes para desenvolvedores.

### 2.1. Fiscal e Tributário (SEFAZ)
*Responsabilidade: `Logistics.Api` (Emissão Outbound) e `SupplyChain.Api` (Captura Inbound)*

*   ✅ **Plugin: PlugNotas (Implementado)**
    *   **Funcionalidade:** Emissão de NFe, CTe e MDFe abstrata (envio via JSON e webhooks assíncronos).
    *   **Developer Experience:** Excelente. Possui um **Sandbox gratuito e público** (`api.sandbox.plugnotas.com.br`) com token genérico liberado, permitindo codificar e testar imediatamente sem contrato.
    *   **Status:** Adapter, dispatcher, webhook handler e UI implementados. Pendente: cadastrar emitente no painel sandbox para autorização real.
*   🔌 **Plugin: Focus NFe**
    *   **Funcionalidade:** Emissão fiscal.
    *   **Developer Experience:** Possui ambiente de homologação (`homologacao.focusnfe.com.br`) e Collections públicas do Postman, emitindo notas sem validade tributária para testes.

### 2.2. Financeiro e Pagamentos (Gateway)
*Responsabilidade: Novo Consumer no RabbitMQ escutando `logistics.shipment_dispatched`*

*   ✅ **Plugin: Asaas (Implementado)**
    *   **Funcionalidade:** Emissão de Boletos e Pix B2B, automatização de cobranças.
    *   **Developer Experience:** Excelente. Possui o `sandbox.asaas.com`, onde é possível criar uma conta grátis para simular pagamentos e testar webhooks sem cartão de crédito ou contrato ativo.
    *   **Status:** Adapter (busca/cria customer → cria cobrança Boleto/PIX), dispatcher, webhook handler (X-Asaas-Access-Token) e UI implementados. Campos `PaymentExternalId/Status/BoletoUrl/PixUrl` na entidade Dispatch. Testado end-to-end no sandbox: cobrança `pay_tj3uqzlu1rjted2a` criada com sucesso. **Atenção:** URL correta do sandbox é `api-sandbox.asaas.com/v3` (não `sandbox.asaas.com/v3`); auth via header `access_token` (não Bearer); `User-Agent` obrigatório.
*   🔌 **Plugin: Iugu / Stripe**
    *   **Funcionalidade:** Faturamentos complexos, divisão de recebíveis (Split).

### 2.3. Logística e Gestão de Frete (TMS)
*Responsabilidade: `Logistics.Api`*

*   ✅ **Plugin: Melhor Envio (Implementado)**
    *   **Funcionalidade:** Cotação e geração de etiquetas para encomendas fracionadas (Correios/Jadlog).
    *   **Developer Experience:** Muito boa. Possui o `sandbox.melhorenvio.com.br` com saldo fictício. O sistema avança o status do pacote automaticamente (Postado -> Entregue) a cada 15 minutos para testarmos a rastreabilidade.
    *   **Status:** Adapter (fluxo cart→checkout→generate→print), dispatcher, webhook handler (HMAC-SHA256) e UI implementados. Coluna Etiqueta na tela de Despachos com link para PDF e tracking code. Pendente: criar conta no sandbox e obter access_token OAuth2.
*   🔌 **Plugin: Intelipost / RoutEasy**
    *   **Funcionalidade:** Hub logístico corporativo e roteirização otimizada para a frota do módulo `Fleet`.
    *   **Developer Experience:** Acesso ao Sandbox geralmente restrito após acordo comercial.

### 2.4. Telemetria e IoT (Frota)
*Responsabilidade: `Fleet.Api`*

*   🔌 **Plugin: Cobli** *(Fase 2 — sem Sandbox público)*
    *   **Funcionalidade:** Extração de hodômetro para engatilhar `VehicleMaintenancePlan` e webhooks de rastreamento de veículos em tempo real.
    *   **Developer Experience:** A API possui documentação aberta e extensa (`docs.cobli.co`), mas não há Sandbox público para gerar eventos fictícios sem conta paga.
    *   **Estratégia:** Enquanto não houver conta Cobli ativa, utilizar o `MockTelemetryAdapter` (implementado na camada de Infraestrutura de `Fleet.Api`) que gera eventos de hodômetro simulados via `IHostedService`. O adapter é selecionado quando `TenantIntegrations.ProviderType = "mock"`, garantindo que o fluxo de `VehicleMaintenancePlan` possa ser testado end-to-end sem dependência externa.
*   🔌 **Plugin: Sascar**
    *   **Funcionalidade:** Telemetria com foco em gerenciamento de risco e bloqueio veicular.

### 2.5. Recursos Humanos e Ponto (REP)
*Responsabilidade: `HumanResources.Api`*

*   🔌 **Plugin: Ahgora / Control iD**
    *   **Funcionalidade:** Captura de biometria de entrada/saída da fábrica, injetando as horas diretamente no Rail-Factory para alocar custo nos *Work Centers* da `Production.Api`.

### 2.6. ERP Backoffice (Contabilidade)
*Responsabilidade: Consumers no RabbitMQ (Cross-Domain)*

*   🔌 **Plugin: Omie**
    *   **Funcionalidade:** Integração contábil, Contas a Pagar e Contas a Receber.
    *   **Developer Experience:** O portal `developer.omie.com.br` permite criar um **"Aplicativo Teste"** gratuitamente, funcionando como uma base isolada para testar a injeção de dados via REST.
*   🔌 **Plugin: Sankhya / Conta Azul**
    *   **Funcionalidade:** Opções para grandes indústrias (Sankhya) e PMEs (Conta Azul).

---

## 3. Schema da Tabela `TenantIntegrations`

A tabela reside em `RailFactory.Tenancy.Api` e centraliza todas as credenciais e escolhas de fornecedor por Tenant.

```sql
CREATE TABLE "TenantIntegrations" (
    "Id"             UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"       UUID         NOT NULL REFERENCES "Tenants"("Id") ON DELETE CASCADE,
    "Category"       VARCHAR(50)  NOT NULL,  -- ex: 'fiscal', 'payment', 'shipping', 'telemetry', 'erp'
    "ProviderType"   VARCHAR(50)  NOT NULL,  -- ex: 'plugnotas', 'asaas', 'melhorenvio', 'cobli', 'mock'
    "IsEnabled"      BOOLEAN      NOT NULL DEFAULT FALSE,
    -- Credenciais cifradas com AES-256-GCM; DEK gerada por tenant e armazenada no campo abaixo.
    -- Nunca armazenar credenciais em texto claro.
    "EncryptedCredentials" BYTEA  NOT NULL,  -- ciphertext (AES-256-GCM)
    "CredentialsDek"       BYTEA  NOT NULL,  -- DEK cifrada com a KEK global (variável de ambiente)
    "CredentialsIv"        BYTEA  NOT NULL,  -- IV/nonce do GCM (96 bits, único por registro)
    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE ("TenantId", "Category")  -- cada tenant tem no máximo um provider ativo por categoria
);
```

**Regras de acesso:**
- A KEK (Key Encryption Key) é lida exclusivamente de variável de ambiente (`TENANCY__KEK`), nunca do banco.
- A `TenantIntegrationFactory` descriptografa a DEK em memória apenas no momento da resolução do adapter e descarta imediatamente após o uso.
- Logs nunca devem incluir campos derivados de `EncryptedCredentials`.

---

## 4. Estratégia de Webhooks Inbound (Idempotência e Retry)

Plugins como Asaas e PlugNotas entregam eventos via HTTP POST para endpoints do Rail-Factory. Sem tratamento adequado, reentregas causam processamento duplicado.

### 4.1 Fluxo de Recebimento

```
Provider (Asaas / PlugNotas)
        │  POST /webhooks/{provider}
        ▼
  WebhookController
        │  1. Valida assinatura HMAC (header X-Webhook-Token ou X-Hub-Signature-256)
        │  2. Persiste o evento bruto na tabela InboundWebhookEvents (status = Pending)
        │  3. Retorna HTTP 200 imediatamente
        ▼
  InboundWebhookProcessor (BackgroundService)
        │  4. Lê eventos Pending com SELECT ... FOR UPDATE SKIP LOCKED
        │  5. Chama o handler do domínio correspondente
        │  6. Marca status = Processed (sucesso) ou Failed (exceção)
        │  7. Incrementa RetryCount; após MaxRetries → status = DeadLettered
```

### 4.2 Schema da Tabela `InboundWebhookEvents`

```sql
CREATE TABLE "InboundWebhookEvents" (
    "Id"           UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"     UUID         NOT NULL,
    "Provider"     VARCHAR(50)  NOT NULL,   -- ex: 'asaas', 'plugnotas'
    "EventType"    VARCHAR(100) NOT NULL,   -- ex: 'payment.confirmed'
    "ExternalId"   VARCHAR(255) NOT NULL,   -- ID único do evento no provider (chave de idempotência)
    "Payload"      JSONB        NOT NULL,
    "Status"       VARCHAR(20)  NOT NULL DEFAULT 'Pending', -- Pending | Processed | Failed | DeadLettered
    "RetryCount"   SMALLINT     NOT NULL DEFAULT 0,
    "LastError"    TEXT,
    "ReceivedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ProcessedAt"  TIMESTAMPTZ,
    UNIQUE ("Provider", "ExternalId")  -- garante idempotência global por provider
);
```

**Política de retry:** backoff exponencial com jitter, máximo de 5 tentativas antes de `DeadLettered`. Eventos `DeadLettered` geram alerta via o canal SSE já implementado (RF-36).

---

## 5. Guia de Implementação (Protocolo por Plugin)

Sempre que a equipe for implementar uma nova categoria de Plugin, o protocolo será:

1. Criar a interface (Port) na camada Application do microsserviço relevante (ex: `IPaymentGateway`).
2. Adicionar a linha em `TenantIntegrations` com `Category` e `ProviderType` correspondentes.
3. Construir os Adapters (ex: `PlugNotasAdapter`, `AsaasAdapter`) na camada de Infraestrutura, implementando a Port.
4. Para providers sem Sandbox, criar um `Mock{Category}Adapter` selecionável via `ProviderType = "mock"`.
5. Para providers que entregam eventos inbound, registrar o endpoint em `WebhookController` e o handler em `InboundWebhookProcessor`.
6. Adicionar a "Chave de Liga/Desliga" na UI (Frontend) para o Administrador do Tenant.
7. Validar end-to-end no Sandbox gratuito (Asaas: `sandbox.asaas.com`; PlugNotas: `api.sandbox.plugnotas.com.br`).
