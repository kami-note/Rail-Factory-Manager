# Arquitetura de Integrações e Plugins (App Store)

**Status:** Visão Estratégica e Planejamento
**Última Atualização:** 2026-05-31

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

*   🔌 **Plugin: PlugNotas (Recomendado)**
    *   **Funcionalidade:** Emissão de NFe, CTe e MDFe abstrata (envio via JSON e webhooks assíncronos).
    *   **Developer Experience:** Excelente. Possui um **Sandbox gratuito e público** (`api.sandbox.plugnotas.com.br`) com token genérico liberado, permitindo codificar e testar imediatamente sem contrato.
*   🔌 **Plugin: Focus NFe**
    *   **Funcionalidade:** Emissão fiscal.
    *   **Developer Experience:** Possui ambiente de homologação (`homologacao.focusnfe.com.br`) e Collections públicas do Postman, emitindo notas sem validade tributária para testes.

### 2.2. Financeiro e Pagamentos (Gateway)
*Responsabilidade: Novo Consumer no RabbitMQ escutando `logistics.shipment_dispatched`*

*   🔌 **Plugin: Asaas**
    *   **Funcionalidade:** Emissão de Boletos e Pix B2B, automatização de cobranças.
    *   **Developer Experience:** Excelente. Possui o `sandbox.asaas.com`, onde é possível criar uma conta grátis para simular pagamentos e testar webhooks sem cartão de crédito ou contrato ativo.
*   🔌 **Plugin: Iugu / Stripe**
    *   **Funcionalidade:** Faturamentos complexos, divisão de recebíveis (Split).

### 2.3. Logística e Gestão de Frete (TMS)
*Responsabilidade: `Logistics.Api`*

*   🔌 **Plugin: Melhor Envio**
    *   **Funcionalidade:** Cotação e geração de etiquetas para encomendas fracionadas (Correios/Jadlog).
    *   **Developer Experience:** Muito boa. Possui o `sandbox.melhorenvio.com.br` com saldo fictício. O sistema avança o status do pacote automaticamente (Postado -> Entregue) a cada 15 minutos para testarmos a rastreabilidade.
*   🔌 **Plugin: Intelipost / RoutEasy**
    *   **Funcionalidade:** Hub logístico corporativo e roteirização otimizada para a frota do módulo `Fleet`.
    *   **Developer Experience:** Acesso ao Sandbox geralmente restrito após acordo comercial.

### 2.4. Telemetria e IoT (Frota)
*Responsabilidade: `Fleet.Api`*

*   🔌 **Plugin: Cobli**
    *   **Funcionalidade:** Extração de hodômetro para engatilhar `VehicleMaintenancePlan` e webhooks de rastreamento de veículos em tempo real.
    *   **Developer Experience:** A API possui documentação aberta e extensa (`docs.cobli.co`), permitindo codificar "às cegas". Porém, não há um ambiente Sandbox público para gerar eventos fictícios de frota sem uma conta paga.
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

## 3. Guia de Implementação (Próximos Passos)

Sempre que a equipe for implementar uma nova categoria de Plugin, o protocolo será:
1. Criar a interface (Port) na camada Application do microsserviço relevante.
2. Criar a tabela `TenantIntegrations` em `RailFactory.Tenancy.Api`.
3. Construir os Adapters (ex: `PlugNotasAdapter`, `AsaasAdapter`) na camada de Infraestrutura do microsserviço.
4. Adicionar a "Chave de Liga/Desliga" na UI (Frontend) para o Administrador do Tenant.
5. Iniciar os testes utilizando os Sandboxes gratuitos (ex: Asaas e PlugNotas).
