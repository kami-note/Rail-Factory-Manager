# Arquitetura Geral: Rail-Factory Fork

Este documento define a arquitetura alvo, padrões de engenharia, diagramas de dependência e contratos cross-domain do Rail-Factory Fork.

---

## 1. Visão Arquitetural

O sistema é um ERP industrial **multitenant** construído sobre uma **Arquitetura Hexagonal (Ports & Adapters)**.

### Objetivos Principais
- **Isolamento Total**: Dados de diferentes tenants nunca se misturam (DB por Tenant).
- **Integridade Hexagonal**: A regra de negócio (Domain) é protegida de tecnologias externas (DB, HTTP, Frameworks).
- **Consistência por Eventos**: Mudanças de estado que cruzam fronteiras de domínio usam o padrão **Outbox** para entrega atômica.

---

## 2. Componentes do Sistema (C4 — Containers)

```mermaid
flowchart TD
    Browser[Browser / Mobile]
    BFF[Frontend BFF .NET]
    UI[React SPA]
    Gateway[Gateway YARP]
    IAM[IAM API]
    Tenancy[Tenancy API]
    Supply[Supply Chain API]
    Inventory[Inventory API]
    Production[Production API]

    Browser --> UI
    UI --> BFF
    BFF --> Gateway
    Gateway --> IAM
    Gateway --> Tenancy
    Gateway --> Supply
    Gateway --> Inventory
    Gateway --> Production
```

### Papéis e Responsabilidades

| Componente | Responsabilidade | Segurança / Auth |
|---|---|---|
| **BFF** | Sessão Browser, CSRF, Orquestração de UI. | Emite **Internal JWT** (curto). |
| **Gateway** | Roteamento, Rate Limit, Normalização de Headers. | Valida Internal JWT / API Key. |
| **IAM** | Login (Google SSO), Usuários, Permissões. | Dono da Autenticação Primária. |
| **Tenancy** | Resolução e isolamento de tenant (connection strings). | Interna — sem exposição direta ao browser. |
| **Inventory** | **Único dono de saldo**, ledger e catálogo de materiais. | Proteção por Internal JWT + Tenant. |
| **Supply Chain** | Recebimento, XML/NF-e, Associação, Conferência e Devolução. | Proteção por Internal JWT + Tenant. |
| **Production** | Work Centers, BOM, Ordens de Produção, Execução. | Proteção por Internal JWT + Tenant. |

---

## 3. Visão Geral dos Domínios

```mermaid
flowchart LR
    Frontend[Frontend BFF .NET<br/>+ React/Vite]
    Gateway[Gateway YARP]
    IAM[IAM]
    Tenancy[Tenancy]
    TenantCatalog[Tenant Catalog DB]
    Supply[Supply Chain<br/>Entrada de materiais]
    Inventory[Inventory<br/>Estoque e saldos]
    InventoryDb[(Inventory DB<br/>por tenant)]
    Production[Production<br/>Produção]
    Logistics[Logistics<br/>Expedição]
    Fleet[Fleet<br/>Frota]
    HR[HR<br/>Pessoas]
    Dashboard[Dashboard<br/>Indicadores]

    Frontend --> Gateway
    Gateway --> IAM
    Gateway --> Supply
    Gateway --> Inventory
    Gateway --> Production
    Gateway --> Logistics
    Gateway --> Fleet
    Gateway --> HR
    Gateway --> Dashboard

    IAM --> Tenancy
    Supply --> Tenancy
    Inventory --> Tenancy
    Production --> Tenancy
    Logistics --> Tenancy
    Fleet --> Tenancy
    HR --> Tenancy
    Dashboard --> Tenancy
    Tenancy --> TenantCatalog

    Supply --> Inventory
    Production --> Inventory
    Inventory --> InventoryDb
    Production --> Dashboard
    Supply --> Dashboard
    Logistics --> Dashboard
    Fleet --> Logistics
    HR --> Fleet
    HR --> Production
    HR --> Dashboard
```

**Leitura simples:**
- `IAM` e `Tenancy` sustentam o acesso ao sistema.
- `Supply Chain` alimenta o estoque.
- `Inventory` é uma fronteira própria e guarda saldos que outras áreas usam.
- `Production` depende de estoque para reservar e consumir material.
- `Dashboard` depende dos eventos/dados gerados pelas operações.
- `HR` e `Fleet` entram antes da expedição completa porque Logistics precisa de pessoas e veículos.

---

## 4. Ordem de Construção (Passadas)

```mermaid
flowchart TD
    A[1. Base técnica<br/>Aspire + defaults + contratos]
    B[2. Tenancy mínimo<br/>tenant dev + X-Tenant-Code]
    C[3. IAM mínimo<br/>OAuth Google + usuário + sessão]
    D[4. Supply Chain inicial<br/>entrada XML/NF-e]
    E[5. Inventory inicial<br/>saldo recebido e pendente]
    F[6. Supply Chain volta<br/>conferência cega e divergências]
    G[7. Inventory volta<br/>saldo disponível]
    H[8. Production inicial<br/>BOM e OP]
    I[9. Inventory volta<br/>reserva de material]
    J[10. Production volta<br/>consumo, scrap, qualidade, lote]
    K[11. Dashboard inicial<br/>leitura de dados reais]
    L[12. HR e Fleet base<br/>pessoas, horas, veículos]
    M[13. Logistics inicial<br/>expedição, frete, status B2B]
    N[14. Recursos avançados<br/>OEE, tracking, webhooks, relatórios]

    A --> B --> C --> D --> E --> F --> G --> H --> I --> J --> K --> L --> M --> N
```

*Cada domínio recebe uma primeira versão pequena; o projeto volta nele quando o fluxo exigir.*

---

## 5. Evolução por Domínio (Versões)

```mermaid
flowchart LR
    IAM1[IAM v1<br/>OAuth Google<br/>usuário + tenant dev]
    IAM2[IAM v2<br/>RBAC / API keys / MFA]

    Supply1[Supply v1<br/>entrada XML/NF-e]
    Supply2[Supply v2<br/>conferência cega]
    Supply3[Supply v3<br/>divergências / devoluções]

    Inv1[Inventory v1<br/>saldo pendente]
    Inv2[Inventory v2<br/>saldo disponível]
    Inv3[Inventory v3<br/>reserva / bloqueio / baixa]

    Prod1[Production v1<br/>BOM + OP]
    Prod2[Production v2<br/>reserva material]
    Prod3[Production v3<br/>consumo / scrap / qualidade / lote]

    Dash1[Dashboard v1<br/>consultas simples]
    Dash2[Dashboard v2<br/>OEE / custos / alertas]

    IAM1 --> Supply1
    Supply1 --> Inv1
    Inv1 --> Supply2
    Supply2 --> Inv2
    Inv2 --> Prod1
    Prod1 --> Inv3
    Inv3 --> Prod2
    Prod2 --> Prod3
    Prod3 --> Dash1

    IAM1 -. evolui .-> IAM2
    Supply2 -. evolui .-> Supply3
    Dash1 -. evolui .-> Dash2
```

---

## 6. Protocolos de Prevenção Elite

### 6.1 Identidade Propagada (Audit Chain)
- O BFF gera um **Internal Bearer JWT** após validar a sessão do cookie.
- Este JWT contém o e-mail do usuário e o `tenantCode`.
- Serviços internos validam que o `tenant` do token coincide com o `X-Tenant-Code` da requisição (Prevenção de Replay Cross-Tenant).

### 6.2 Backend-Driven UI (BFF for Statuses)
- APIs retornam um objeto `DisplayStatus`: `{ "key": "pending", "label": "Pendente", "color": "warning" }`.
- O Frontend usa o componente `StatusChip.tsx` para renderizar o que o backend enviou.
- Hardcoding de cores/labels em componentes de feature é **proibido**.

### 6.3 Value Objects (Identidade de Negócio)
Identificadores críticos são **Value Objects** em `BuildingBlocks`:
- `MaterialCode`: Uppercase + Trim.
- `FiscalId`: Somente dígitos (CNPJ/CPF).
- `EmailAddress`: Lowercase + Trim.

### 6.4 State Machine Hardening (Status Guards)
Toda alteração de `Status` no domínio deve ser protegida por uma guarda explícita:
```csharp
if (Status != MaterialReceiptStatus.Registered)
    throw new InvalidOperationException("Conferência só pode iniciar em recibos Registrados.");
```

---

## 7. Fluxo de Requisição Protegido

1. **Browser** envia request com **Cookie de Sessão** + **Header CSRF** para o **BFF**.
2. **BFF** valida o cookie, valida o CSRF e resolve o usuário.
3. **BFF** gera um **Internal JWT** assinado (expira em 5 min) e repassa ao **Gateway**.
4. **Gateway** roteia para o microserviço.
5. **Microserviço** valida o **Internal JWT**, verifica tenant e executa a lógica.

---

## 8. Estratégia de Dados e Persistência

- **Isolamento**: Cada serviço tem seu próprio banco de dados (ex: `supplydb`, `inventorydb`).
- **Connection Strings**: O serviço de **Tenancy** é o único que sabe onde o banco de cada tenant está.
- **Migrations**: Versionamento de schema via Entity Framework Migrations é obrigatório.

---

## 9. Comunicação Cross-Domain (Event-Driven)

1. **Outbox Pattern**: O evento é salvo na mesma transação do banco de dados do serviço de origem.
2. **Event Dispatcher**: Processo de background lê o outbox e **publica para RabbitMQ** (exchange direto, durable). O Inventory consome via `InventoryIntegrationConsumer`. Endpoints HTTP `/api/inventory/internal/*` continuam ativos como fallback mas não são utilizados.
3. **Idempotência**: O serviço de destino valida o `eventId` para evitar duplicidade.

### Contratos Entre Domínios

| Origem | Destino | Contrato | Observação |
|---|---|---|---|
| Supply Chain | Inventory | Criar saldo pendente | A entrada NÃO gera saldo disponível antes da conferência. |
| Supply Chain | Inventory | Liberar ou bloquear saldo | Resultado da conferência cega. |
| Supply Chain | Inventory | Registrar devolução | Usado quando há divergência/defeito. |
| Production | Inventory | Reservar material | Obrigatório antes de liberar OP. |
| Production | Inventory | Consumir reserva | Baixa acontece contra reserva, não contra saldo livre. |
| Production | Inventory | Registrar scrap | Deve afetar ledger e rastreabilidade. |
| Logistics | Inventory | Separar/baixar produto acabado | Entra na expedição base. |
| Dashboard | Inventory/Production/Supply | Ler read models ou consultas consolidadas | NÃO deve duplicar regra de saldo. |

---

## 10. Sistema de Medidas (UoM)

**Comportamento atual:**
- A BOM define a UoM.
- A reserva usa a UoM da BOM.
- Consumo e scrap usam a UoM da reserva.
- Conversão automática de UoM **não entra no primeiro ciclo** de Production.

**Evolução planejada:**
1. Permitir UoM de entrada no consumo/scrap.
2. Resolver regra de conversão ativa.
3. Calcular quantidade canônica.
4. Registrar regra aplicada e auditar conversão.

---

## 11. Fluxos de Sequência Principais

### 11.1 IAM + Entrada de Materiais

```mermaid
sequenceDiagram
    actor Usuário
    participant BFF
    participant Gateway
    participant IAM
    participant Google
    participant Supply
    participant Inventory

    Usuário->>BFF: Acessa o sistema
    BFF->>Gateway: Iniciar login
    Gateway->>IAM: Encaminhar login
    IAM->>Google: OAuth2
    Google-->>IAM: Identidade validada
    IAM-->>BFF: Sessão autenticada

    Usuário->>BFF: Enviar XML/NF-e
    BFF->>Gateway: Chamar Supply com tenant dev
    Gateway->>Supply: Encaminhar requisição
    Supply->>Supply: Validar dados da entrada
    Supply->>Inventory: Registrar saldo pendente
    Inventory-->>Supply: Saldo registrado
    Supply-->>BFF: Entrada criada
    BFF-->>Usuário: Entrada criada
```

### 11.2 Conferência Cega

```mermaid
sequenceDiagram
    actor Conferente
    participant BFF
    participant Gateway
    participant Supply
    participant Inventory

    Conferente->>BFF: Iniciar conferência cega
    BFF->>Gateway: Chamar Supply
    Gateway->>Supply: Encaminhar requisição
    Supply-->>BFF: Itens sem quantidades esperadas
    BFF-->>Conferente: Exibir itens sem quantidades
    Conferente->>BFF: Registrar contagem
    BFF->>Gateway: Enviar contagem
    Gateway->>Supply: Encaminhar contagem
    Supply->>Supply: Comparar contagem com XML

    alt Sem divergência
        Supply->>Inventory: Liberar saldo disponível
        Supply-->>Conferente: Recebimento aprovado
    else Com divergência
        Supply->>Inventory: Manter saldo bloqueado
        Supply-->>Conferente: Exigir decisão
    end
```

### 11.3 Ciclo de Produção

```mermaid
sequenceDiagram
    actor Planejador
    participant BFF
    participant Gateway
    participant Production
    participant Inventory

    Planejador->>BFF: Criar BOM e OP
    BFF->>Gateway: Chamar Production
    Planejador->>BFF: Liberar OP
    BFF->>Gateway: Chamar Production
    Gateway->>Production: Encaminhar liberação
    Production->>Inventory: Solicitar reserva de materiais

    alt Saldo suficiente
        Inventory-->>Production: Reserva confirmada
        Production-->>Planejador: OP liberada
    else Saldo insuficiente
        Inventory-->>Production: Reserva recusada
        Production-->>Planejador: OP bloqueada
    end
```

---

## 12. Critério para Implementar Agora vs. Depois

**Implementar agora quando:**
- Desbloqueia o próximo fluxo.
- Reduz retrabalho.
- Respeita tenant.
- Usa dados reais ou prepara dado que será usado logo.
- Pode ser entregue sem criar complexidade artificial.

**Adiar quando:**
- Depende de dados que ainda não existem.
- Exige integração externa ainda não validada.
- Só faz sentido com alto volume de uso.
- Seria apenas demonstrativo neste momento.

**Evitar na primeira passada:**
- Dashboard em tempo real / OEE completo.
- Webhooks externos / roteirização.
- RBAC extremamente granular.
- Outbox em todos os serviços antes de haver eventos reais suficientes.
- Separar serviços em excesso se o fluxo ainda não exigir.

---

*Este documento é evolutivo e deve ser atualizado a cada mudança na direção arquitetural.*
