# Grafos do Projeto

Este documento mostra o Rail-Factory como um conjunto de partes conectadas.

O objetivo dos grafos e ajudar a decidir a ordem de construcao sem perder a visao do sistema completo.

## 1. Visao Geral dos Dominios

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
    Production[Production<br/>Producao]
    Logistics[Logistics<br/>Expedicao]
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

Leitura simples:

- `IAM` e `Tenancy` sustentam o acesso ao sistema.
- `Supply Chain` alimenta o estoque.
- `Inventory` e uma fronteira propria e guarda saldos que outras areas usam.
- `Production` depende de estoque para reservar e consumir material.
- `Dashboard` depende dos eventos/dados gerados pelas operacoes.
- `HR` e `Fleet` entram antes da expedicao completa porque Logistics precisa de pessoas, motoristas e veiculos.

## 2. Ordem de Construcao

```mermaid
flowchart TD
    A[1. Base tecnica<br/>Aspire + defaults + contratos]
    B[2. Tenancy minimo<br/>tenant dev persistido + X-Tenant-Code]
    C[3. IAM minimo<br/>OAuth Google + usuario + sessao]
    D[4. Supply Chain inicial<br/>entrada XML/NF-e]
    E[5. Inventory inicial<br/>saldo recebido e pendente]
    F[6. Supply Chain volta<br/>conferencia cega e divergencias]
    G[7. Inventory volta<br/>saldo disponivel]
    H[8. Production inicial<br/>BOM e OP]
    I[9. Inventory volta<br/>reserva de material]
    J[10. Production volta<br/>consumo, sucata, qualidade, lote]
    K[11. Dashboard inicial<br/>leitura de dados reais]
    L[12. HR e Fleet base<br/>pessoas, horas, veiculos, capacidade]
    M[13. Logistics inicial<br/>expedicao, frete, status B2B]
    N[14. Recursos avancados<br/>OEE, tracking, webhooks, relatorios]

    A --> B --> C --> D --> E --> F --> G --> H --> I --> J --> K --> L --> M --> N
```

Essa ordem nao significa que um dominio fica completo antes do proximo.

Ela significa que cada dominio recebe uma primeira versao pequena e depois o projeto volta nele quando o fluxo exigir.

Status atual:

Consultar `CONTEXTO_ATUAL.md` para o estado real. Em 2026-05-01, P0 foi concluido como base inicial e P1 foi iniciado pelo Tenancy: tenant `dev` persistido no Tenant Catalog. A proxima aresta do grafo e o resolver `X-Tenant-Code`.

## 3. Passadas Por Dominio

```mermaid
flowchart LR
    IAM1[IAM v1<br/>OAuth Google<br/>usuario autenticado<br/>tenant dev]
    IAM2[IAM v2<br/>RBAC<br/>API keys<br/>MFA<br/>auditoria]

    Supply1[Supply v1<br/>entrada XML/NF-e]
    Supply2[Supply v2<br/>conferencia cega]
    Supply3[Supply v3<br/>divergencias<br/>devolucoes]

    Inv1[Inventory v1<br/>saldo pendente]
    Inv2[Inventory v2<br/>saldo disponivel]
    Inv3[Inventory v3<br/>reserva<br/>bloqueio<br/>baixa]

    Prod1[Production v1<br/>BOM<br/>OP]
    Prod2[Production v2<br/>reserva material]
    Prod3[Production v3<br/>consumo<br/>scrap<br/>qualidade<br/>lote]

    Dash1[Dashboard v1<br/>consultas simples]
    Dash2[Dashboard v2<br/>OEE<br/>custos<br/>alertas]

    Log1[Logistics v1<br/>expedicao simples]
    Log2[Logistics v2<br/>tracking<br/>webhooks<br/>frete]

    Fleet1[Fleet v1<br/>veiculos<br/>capacidade]
    Fleet2[Fleet v2<br/>motoristas<br/>manutencao<br/>telemetria]

    HR1[HR v1<br/>pessoas<br/>horas]
    HR2[HR v2<br/>competencias<br/>turnos<br/>contabil]

    IAM1 --> Supply1
    Supply1 --> Inv1
    Inv1 --> Supply2
    Supply2 --> Inv2
    Inv2 --> Prod1
    Prod1 --> Inv3
    Inv3 --> Prod2
    Prod2 --> Prod3
    Prod3 --> Dash1
    Dash1 --> HR1
    HR1 --> Fleet1
    Fleet1 --> Log1

    IAM1 -. volta .-> IAM2
    Supply2 -. volta .-> Supply3
    Dash1 -. volta .-> Dash2
    Log1 -. volta .-> Log2
    Fleet1 -. volta .-> Fleet2
    HR1 -. volta .-> HR2
```

## 4. Fluxo Inicial: IAM + Entrada de Materiais

```mermaid
sequenceDiagram
    actor Usuario
    participant BFF
    participant Gateway
    participant IAM
    participant Google
    participant Supply
    participant Inventory

    Usuario->>BFF: Acessa o sistema
    BFF->>Gateway: Iniciar login
    Gateway->>IAM: Encaminhar login
    IAM->>Google: OAuth2
    Google-->>IAM: Identidade validada
    IAM-->>Gateway: Identidade autenticada
    Gateway-->>BFF: Sessao autenticada

    Usuario->>BFF: Enviar XML/NF-e ou importar entrada
    BFF->>Gateway: Chamar Supply com tenant dev
    Gateway->>Supply: Encaminhar requisicao normalizada
    Supply->>Supply: Validar dados da entrada
    Supply->>Inventory: Registrar saldo pendente
    Inventory-->>Supply: Saldo registrado
    Supply-->>Gateway: Entrada criada
    Gateway-->>BFF: Entrada criada
    BFF-->>Usuario: Entrada criada
```

## 5. Fluxo Depois Da Conferencia Cega

```mermaid
sequenceDiagram
    actor Conferente
    participant BFF
    participant Gateway
    participant Supply
    participant Inventory

    Conferente->>BFF: Iniciar conferencia cega
    BFF->>Gateway: Chamar Supply
    Gateway->>Supply: Encaminhar requisicao
    Supply-->>Gateway: Itens sem quantidades esperadas
    Gateway-->>BFF: Itens sem quantidades esperadas
    BFF-->>Conferente: Mostrar itens sem quantidades esperadas
    Conferente->>BFF: Registrar contagem
    BFF->>Gateway: Enviar contagem
    Gateway->>Supply: Encaminhar contagem
    Supply->>Supply: Comparar contagem com XML

    alt Sem divergencia
        Supply->>Inventory: Liberar saldo disponivel
        Inventory-->>Supply: Saldo disponivel
        Supply-->>Gateway: Recebimento aprovado
        Gateway-->>BFF: Recebimento aprovado
        BFF-->>Conferente: Recebimento aprovado
    else Com divergencia
        Supply->>Inventory: Manter saldo bloqueado
        Supply-->>Gateway: Exigir decisao
        Gateway-->>BFF: Exigir decisao
        BFF-->>Conferente: Exigir decisao
    end
```

## 6. Fluxo De Producao

```mermaid
sequenceDiagram
    actor Planejador
    participant BFF
    participant Gateway
    participant Production
    participant Inventory
    participant Dashboard

    Planejador->>BFF: Criar BOM e OP
    BFF->>Gateway: Chamar Production
    Gateway->>Production: Encaminhar requisicao
    Planejador->>BFF: Liberar OP
    BFF->>Gateway: Chamar Production
    Gateway->>Production: Encaminhar liberacao
    Production->>Inventory: Solicitar reserva de materiais

    alt Saldo suficiente
        Inventory-->>Production: Reserva confirmada
        Production-->>Gateway: OP liberada
        Gateway-->>BFF: OP liberada
        BFF-->>Planejador: OP liberada
        Production->>Dashboard: Informar evento operacional
    else Saldo insuficiente
        Inventory-->>Production: Reserva recusada
        Production-->>Gateway: OP bloqueada
        Gateway-->>BFF: OP bloqueada
        BFF-->>Planejador: OP bloqueada
    end
```

## 7. O Que Evitar No Inicio

Evitar na primeira passada:

- dashboard em tempo real;
- OEE completo;
- webhooks externos;
- roteirizacao inteligente;
- telemetria;
- RBAC extremamente granular;
- Outbox em todos os servicos antes de haver eventos reais suficientes;
- separar servicos em excesso se o fluxo ainda nao exigir.

Nao significa que esses itens sairam do projeto. Significa apenas que eles entram quando houver base real para usa-los.
