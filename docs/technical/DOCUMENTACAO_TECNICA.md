# 5. DOCUMENTAÇÃO TÉCNICA

Este capítulo apresenta a especificação técnica detalhada da infraestrutura tecnológica, da arquitetura de software, das estratégias de persistência de dados, das integrações de sistemas parceiros e das boas práticas que regem o desenvolvimento da plataforma **Rail-Factory-Fork**. O escopo desta documentação compreende a fundamentação das escolhas de design, os mecanismos operacionais das tecnologias selecionadas e o fluxo de interação entre os componentes do sistema.

---

## 5.1. ARQUITETURA DO SISTEMA

A plataforma **Rail-Factory-Fork** é um sistema de planejamento e controle de recursos (ERP) industrial, projetado de forma nativa sob a orientação da **Arquitetura Hexagonal (Ports & Adapters)**. Esta abordagem foi selecionada para garantir o desacoplamento estrito entre as regras de negócio centrais (o núcleo de domínio) e os agentes tecnológicos externos, tais como bancos de dados, barramentos de mensageria e APIs de terceiros.

A distribuição do sistema em microsserviços tenant-aware visa assegurar resiliência operacional. Desta forma, eventuais indisponibilidades em serviços secundários (como falhas de comunicação com portais de frete ou emissão de boletos) não impactam as operações críticas no chão de fábrica, resguardando a integridade das ordens de produção e a acurácia dos saldos em estoque.

### 5.1.1. Segmentação da Arquitetura

O ecossistema da plataforma é segmentado em camadas funcionais com responsabilidades estritamente delimitadas:

1.  **Camada de Apresentação (React Single Page Application):**
    Consiste na interface gráfica operada nos terminais industriais e escritórios administrativos. Desenvolvida em React e TypeScript sob a coordenação do compilador Vite, a aplicação cliente gerencia o estado da interface localmente e realiza a comunicação com os serviços de backend exclusivamente por meio de requisições HTTPS direcionadas ao servidor de borda do frontend. O design das páginas segue uma divisão de diretórios orientada a funcionalidades (*features*), garantindo conformidade conceitual com os microsserviços.

2.  **Edge Security e BFF (Backend-For-Frontend .NET 10):**
    O BFF atua como o único ponto de entrada exposto publicamente para o tráfego do navegador. Esta camada é responsável por realizar a autenticação inicial das requisições via Cookies criptografados com flags de segurança estritas e validar chaves antiforgery (CSRF). Após a validação da sessão com o microsserviço de identidade, o BFF gera um token **Internal JWT (Bearer)** assinado com algoritmo HMAC-SHA256, de curta duração, injetando-o nos cabeçalhos HTTP antes de encaminhar o fluxo de tráfego para a rede interna.

3.  **API Gateway (YARP Reverse Proxy):**
    Posicionado na rede interna, o Gateway realiza o roteamento reverso das requisições baseando-se em caminhos de URL pré-configurados. O YARP foi integrado ao mecanismo de Service Discovery do orquestrador de contêineres, de modo a dispensar a definição estática de endereços físicos para as APIs de domínio, permitindo que a infraestrutura se auto-organize de forma elástica.

4.  **Microsserviços de Domínio:**
    Conjunto de APIs independentes que executam a lógica de negócio sob limites de contexto demarcados (*Bounded Contexts*). O ecossistema é formado pelos seguintes serviços autônomos:
    *   **Tenancy API:** Responsável pelo gerenciamento do catálogo de organizações ativas (tenants), armazenamento seguro de credenciais e resolução de strings de conexão individuais.
    *   **IAM API:** Gerencia os fluxos de credenciamento (Google SSO), controle de acesso baseado em papéis (RBAC), emissão de chaves de API internas e rastreabilidade de auditoria.
    *   **Supply Chain API:** Controla o fluxo de entrada de mercadorias, importação e validação de arquivos XML de NF-e, mapeamento de insumos e condução do fluxo de conferência cega de cargas.
    *   **Inventory API:** Atua como o livro-razão (*ledger*) de estoque, sendo a única fonte de verdade para saldos físicos, lotes e reservas de materiais da fábrica.
    *   **Production API:** Responsável pela manutenção de fichas técnicas de engenharia (BOM), controle de capacidade de centros de trabalho e execução de ordens de produção.
    *   **Human Resources API:** Administra o cadastro básico de operadores e motoristas de frota logística.
    *   **Fleet API:** Gerencia o cadastro físico de veículos tracionados, capacidades de peso bruto e registros de licenças rodoviárias regulatórias.
    *   **Logistics API:** Centraliza o despacho de transportes, cotações de fretes externos, emissões fiscais de MDF-e e coordenação de entregas B2B.

A interação integrada e o fluxo das requisições entre as camadas arquiteturais são representados no diagrama a seguir:

![Figura 01: Diagrama de Arquitetura Geral do Sistema](imagens/arquitetura.png)
*Figura 01: Diagrama de Arquitetura Geral do Sistema.*

#### Mecanismo de Isolamento Multitenant (Database-Per-Tenant)

A garantia de isolamento físico de dados entre diferentes organizações clientes é uma premissa mandatória da arquitetura. Optou-se pela estratégia de banco de dados independente por tenant, mitigando o risco de acesso cruzado indevido e viabilizando rotinas individualizadas de cópias de segurança (backup).

O fluxo de resolução dinâmica ocorre da seguinte forma:
1. O middleware de tenancy intercepta a requisição e captura o cabeçalho de identificação da organização ativa.
2. É realizada uma consulta na base de catálogo para recuperar as strings de conexão correspondentes ao microsserviço chamado.
3. O resolvedor de conexões intercepta a inicialização do contexto do Entity Framework Core em tempo de execução, injetando a string de conexão específica da organização.
4. Em ambiente de desenvolvimento local, a infraestrutura detecta variações de portas causadas pela reinicialização de contêineres e reescreve de forma dinâmica as strings de conexão com as credenciais vigentes, prevenindo erros de inicialização do barramento.

---

## 5.2. TECNOLOGIAS UTILIZADAS

### 5.2.1 Frontend

O desenvolvimento do cliente web baseia-se em tecnologias voltadas a reatividade e tipagem estrita de dados:

*   **React e TypeScript:** Utilizados como a fundação de apresentação do sistema. O React provê a reatividade necessária na renderização de painéis complexos e interativos de chão de fábrica, enquanto o TypeScript assegura a integridade estrutural das interfaces de dados e DTOs manipulados nas telas.
*   **Vite:** Adotado em substituição aos compiladores legados com vistas a otimizar o tempo de compilação (*Hot Module Replacement*) e gerar pacotes de distribuição estática otimizados.
*   **Material UI (MUI):** Empregado para padronizar o catálogo de componentes gráficos da interface (formulários, modais, painéis de controle). A estilização é regida por um tema corporativo centralizado, garantindo consistência visual de tipografia e paleta de cores.
*   **Biblioteca de Validação e Máscaras:** Desenvolvida nativamente para interceptar inscrições fiscais de CNPJ, CPF, CEP, telefones e placas Mercosul. O componente realiza a limpeza de caracteres especiais antes da submissão HTTP e bloqueia o acionamento de formulários caso os algoritmos de verificação matemática de dígitos detectem inconsistências.

### 5.2.2. Backend

Os serviços lógicos da plataforma apoiam-se em tecnologias de alta eficiência de processamento e baixo consumo de recursos de infraestrutura:

*   **C# 14 e .NET 10.0:** Escolhidos para sustentar o ecossistema backend em função de sua alta performance de tempo de execução, suporte nativo a fluxos assíncronos concorrentes e alinhamento com padrões modernos de compilação.
*   **Minimal APIs (ASP.NET Core):** Adotadas para simplificar o roteamento HTTP, reduzindo a latência de inicialização das APIs em ambientes de nuvem ao eliminar a sobrecarga de controllers MVC tradicionais.
*   **Entity Framework Core 10:** ORM oficial utilizado para gerenciar as operações de banco de dados, mapear relacionamentos entre entidades de domínio e converter tipos complexos do modelo por meio de conversores nativos.

### 5.2.3. Banco de Dados

*   **PostgreSQL 17:** Mecanismo de persistência relacional padrão de produção. A escolha é fundamentada no suporte otimizado a colunas do tipo JSONB, o que possibilita ao microsserviço de Estoque armazenar dados de auditoria estruturados sem enrijecer o schema do banco de dados relacional.
*   **SQLite (In-Memory nos Testes de Integração):** Empregado exclusivamente para isolar as suítes de testes lógicos no servidor de integração contínua (CI/CD). O banco de dados opera inteiramente em memória, isolando transações de forma paralela e contornando migrações PostgreSQL específicas por meio de verificações de provedor declaradas no startup do sistema.

### 5.2.4. Ferramentas de Apoio

*   **.NET Aspire 10:** Utilizado como a ferramenta de orquestração do ambiente de desenvolvimento. Ele centraliza a inicialização de dependências locais de bancos de dados, mensageria e armazenamento, provendo telemetria estruturada e logs agregados no console de monitoramento.
*   **RabbitMQ:** Barramento de mensagens assíncronas responsável por trafegar eventos entre os microsserviços. Garante o desacoplamento de execução, de forma que o envio e recebimento de informações operem sob o modelo de consistência eventual.
*   **Redis:** Cache distribuído em memória. Utilizado pelo microsserviço de identidade para armazenar tokens de sessão ativos. Quando o BFF intercepta a requisição do navegador, ele envia uma chamada interna de verificação para a API do IAM (via gateway Yarp). O IAM resolve essa chamada consultando o Redis em vez do banco de dados relacional, mantendo as verificações de autenticação sob extrema velocidade de resposta.

![Figura 02: Diagrama do Fluxo de Validação de Sessão](imagens/sessao.png)
*Figura 02: Diagrama do Fluxo de Validação de Sessão.*

*   **MinIO (Armazenamento S3):** Servidor de armazenamento de objetos compatível com o protocolo AWS S3. Utilizado para gerenciar uploads de mídias operacionais (fotos de perfil e fotos de peças catalogadas), particionando os dados fisicamente por organização cliente no bucket padrão de imagens da plataforma.
*   **Playwright:** Ferramenta de automação de testes de ponta a ponta (E2E). Permite simular a jornada de uso real em navegadores Chromium e capturar layouts operacionais de forma programática.

### 5.2.5. Padrões Adotados

A arquitetura de software da plataforma impõe padrões de projeto formais para assegurar escalabilidade e segurança:

1.  **Padrão Outbox:**
    Adotado para garantir integridade transacional na comunicação distribuída sem recorrer a mecanismos lentos de duas fases (2PC). Quando uma operação de alteração ocorre em um microsserviço (como a liberação de uma ordem de produção que demanda reserva de insumos), o registro de domínio e a mensagem de integração são gravados no banco de dados sob a mesma transação local.
    Posteriormente, despachantes em segundo plano buscam as mensagens pendentes utilizando o comando de concorrência pessimista `FOR UPDATE SKIP LOCKED`. Esta técnica previne race conditions impedindo que múltiplas instâncias de despacho processem ou dupliquem a entrega do mesmo evento para o RabbitMQ. Mensagens com falhas de schema ou dados inválidos são isoladas de forma automática em filas de erro (Dead Letter) com log de auditoria.

![Figura 03: Diagrama do Fluxo de Despacho de Eventos via Outbox](imagens/outbox.png)
*Figura 03: Diagrama do Fluxo de Despacho de Eventos via Outbox.*

2.  **Backend-For-Frontend (BFF):**
    Empregado para blindar a comunicação entre o cliente web e os serviços internos. O BFF gerencia a autenticação sob cookies criptografados não acessíveis a scripts do navegador (*HttpOnly*), previne ataques de injeção de requisições maliciosas (CSRF) e isola o roteamento interno por meio do reverse proxy.

3.  **Value Objects (Objetos de Valor):**
    Utilizados no modelo de domínio para garantir a integridade dos dados operacionais antes da gravação no banco de dados. Identificadores críticos como códigos de materiais, e-mails de operadores e inscrições fiscais de CNPJ e CPF são encapsulados em tipos ricos estruturados que forçam higienização automática (normalização para maiúsculas, remoção de espaços e extração de caracteres de formatação), impossibilitando colisões de identidade por formatação inconsistente.

4.  **State Machine Hardening (Guarda de Estados):**
    Padrão de validação de transição de status implementado diretamente nas entidades de domínio. O sistema proíbe modificações diretas em propriedades de status; todas as transições lógicas (como a aprovação de uma conferência ou o cancelamento de um despacho) exigem chamadas de métodos explícitos protegidos por cláusulas de guarda (*guards*), disparando exceções de negócio caso ocorra tentativa de transição inválida.

---

## 5.2.6. Boas Práticas e Convenções

O desenvolvimento do código-fonte segue diretrizes voltadas à manutenibilidade e qualidade de software:

*   **Early Returns (Retornos Precoces):** Estruturação lógica que prioriza a validação de condições de erro ou saídas rápidas no início dos métodos. Esta prática reduz o nível de aninhamento de blocos de decisão, simplificando a depuração e leitura do fluxo.
*   **Decoupling of Microservice APIs (Desacoplamento de Contratos):** Para preservar a independência de implantação de cada microsserviço, as APIs não compartilham bibliotecas lógicas de DTOs. Os serviços que demandam comunicação síncrona declaram contratos privados e locais baseados nas especificações JSON esperadas do microsserviço chamado.
*   **Centralização da Identidade Visual de Status:** Para assegurar conformidade visual em toda a plataforma, a definição cromática, nomenclatura localizada em português e iconografia de status operacionais são isoladas em um componente React unificado no frontend. O cliente web renderiza os status consumindo diretamente essa especificação centralizada.

---

## 5.2.7. Requisitos de Infraestrutura

A compilação e execução da plataforma em ambientes locais ou de produção pressupõe a disponibilidade dos seguintes componentes:

1.  **Ferramentas de Desenvolvimento:**
    *   Ambiente de execução **.NET SDK 10.0** ou superior.
    *   Gerenciador de dependências e runtime **Node.js v20+** com gerenciador de pacotes **npm** ativo.
    *   Motor de execução de contêineres Docker para implantação de serviços locais.
2.  **Serviços de Infraestrutura (Containerizados):**
    *   Instância ativa do PostgreSQL 17 gerenciando as bases relacionais operacionais.
    *   Broker do RabbitMQ configurado para recepção e roteamento de tráfego de mensageria.
    *   Armazenador MinIO provisionado com credenciais administrativas locais.
    *   Banco de cache Redis active para gerenciamento temporário de sessões.
3.  **Segurança e Comunicação:**
    *   Certificado SSL configurado para viabilizar tráfego local seguro sobre HTTPS, além de credenciais cadastradas no provedor Google Identity Platform para habilitação do logon único (SSO).

---

## 5.2.8. APIs e Integrações

A plataforma realiza integrações sistêmicas nativas com provedores externos para conduzir tarefas críticas de autenticação, faturamento e logística brasileira:

### Provedor de Identidade
*   **Google Identity Platform (SSO):** Utilizado para descentralizar a gestão de senhas e identidades operacionais. A autenticação baseia-se no fluxo OAuth2 padrão, que repassa os dados de e-mail e identificação de usuário após consentimento do operador.

### Provedores de Serviços Fiscais
*   **PlugNotas (Grupo TecnoSpeed) & FocusNFe:** Adaptadores integrados para automação fiscal. O sistema consome as APIs destes provedores para assinar e emitir Manifestos Eletrônicos de Documentos Fiscais (MDF-e) e realizar consultas operacionais de NF-e na SEFAZ. O retorno síncrono ou assíncrono (via webhook) do status de aprovação fiscal atualiza a fila de expedição.

### Provedor de Cobranças e Faturamento
*   **Asaas Payment Gateway:** Utilizado para automatizar a geração de cobranças bancárias (boletos e Pix) de fretes faturados. O sistema envia os dados do despacho e recebe confirmações automáticas de liquidação financeira por meio de webhooks integrados.

### Provedor de Logística e Fretes
*   **Melhor Envio:** Integrado à expedição para cotação e contratação de fretes rodoviários. O fluxo realiza chamadas encadeadas que envolvem a cotação tarifária com transportadoras parceiras baseando-se em CEPs e dimensões dos lotes, inserção da entrega no carrinho do provedor, checkout financeiro, geração de manifestos físicos e retorno do link oficial do documento de postagem para impressão na doca de expedição.

---

## 5.2.9. Caracterização da API

Os microsserviços expõem endpoints em conformidade com as restrições da arquitetura RESTful:

### Estruturação de Respostas e Rastreabilidade
*   **Formato de Dados:** Comunicação padronizada em formato JSON para todas as requisições e retornos HTTP.
*   **Tratamento de Erros via RFC 9110 (Problem Details):** As respostas de erros são padronizadas com cabeçalho de mídia específico para falhas de requisição. A carga útil do erro descreve o título do erro, status HTTP, código de erro estável para tradução na interface e detalhes explicativos da ocorrência, prevenindo a exposição de vestígios técnicos de banco de dados para o usuário externo.
*   **ID de Correlação (Correlation ID):** A rastreabilidade de requisições na malha distribuída baseia-se no cabeçalho customizado de correlação. O identificador unifica os logs de tráfego iniciados no navegador e propagados pelos microsserviços, simplificando diagnósticos de fluxo.

---

## 5.3. REPOSITÓRIO E CÓDIGO-FONTE

O código-fonte do Rail-Factory-Fork é organizado sob o modelo de **Monorepo**, mantendo alinhados o histórico de deploy da interface cliente e das APIs de backend.

### Estrutura Geral de Diretórios do Monorepo

```
/ (Raiz do Repositório)
├── makefile                         # Automação de tarefas de compilação, execução e testes
├── package.json                     # Scripts globais de teste e configurações do ecossistema local
├── wireframes.html                  # Simulador local interativo para navegação offline de telas
├── docs/                            # Documentação técnica e histórico arquitetural do projeto
│   ├── actual_screenshots/          # Registro de capturas reais widescreen da interface operacional
│   ├── imagens/                     # Imagens estáticas compiladas dos diagramas arquiteturais
│   ├── wireframes/                  # Screenshots capturados de forma automatizada do simulador
│   ├── ARQUITETURA_GERAL.md         # Manual descritivo de fluxos de eventos e sequence diagrams
│   ├── CONTRATOS_API.md             # Especificações de payloads de requisição e resposta das APIs
│   └── DOCUMENTACAO_TECNICA.md      # Esta documentação de infraestrutura e padrões técnicos
├── src/                             # Código-Fonte do Ecossistema
│   ├── Directory.Build.props        # Configuração global de diretivas de build C#
│   ├── Directory.Packages.props     # Centralização do controle de pacotes NuGet do backend (.NET 10)
│   ├── RailFactory.Fork.sln         # Solução geral de microsserviços e bibliotecas compartilhadas
│   ├── RailFactory.AppHost/         # Projeto orquestrador responsável pela execução local (.NET Aspire)
│   ├── RailFactory.BuildingBlocks/  # Biblioteca de Value Objects, Tenancy e utilitários de domínio
│   ├── RailFactory.ServiceDefaults/ # Extensões padrão de telemetria, health checks e middlewares de borda
│   ├── RailFactory.Gateway/         # Gateway interno de microsserviços (YARP Reverse Proxy)
│   ├── RailFactory.Frontend/        # BFF e hospedagem de arquivos estáticos compilados do cliente
│   │   ├── Api/                     # Endpoints locais de validação de cookies, CSRF e uploads
│   │   └── App/                     # SPA React (features, shared, componentes, testes)
│   │       ├── src/
│   │       │   ├── features/        # Módulos e telas divididos por limites de microsserviço
│   │       │   └── shared/          # Temas, máscaras centralizadas de validação e componentes de status
│   │       └── vite.config.ts       # Configurações do Vite e proxy reverso local de desenvolvimento
│   ├── RailFactory.Tenancy.Api/     # Catálogo geral de Tenants e gerenciamento de bases físicas
│   ├── RailFactory.Iam.Api/         # Serviço de autorizações, papéis, permissões de usuários (RBAC)
│   ├── RailFactory.SupplyChain.Api/ # Ingestão de NF-e XML, conferência e gestão de devoluções
│   ├── RailFactory.Inventory.Api/   # Gestão de saldos físicos de materiais, reservas e ledger
│   ├── RailFactory.Production.Api/  # Microsserviço de Engenharia de Produção, BOM e OPs
│   ├── RailFactory.HumanResources.Api/# Microsserviço de Pessoas e Organizações de Trabalho
│   ├── RailFactory.Fleet.Api/       # Microsserviço de Veículos e Frota de Distribuição
│   └── RailFactory.Logistics.Api/   # Microsserviço de Despacho, Faturamento e Emissão de MDF-e
└── tests/                           # Suíte de testes automatizados unitários, de integração e E2E
```

### Comandos de Automação do Repositório

As diretivas locais de desenvolvimento estão automatizadas no arquivo `makefile` na raiz do monorepo:

*   **Compilação do Backend (.NET):**
    `dotnet build src/RailFactory.Fork.sln`
*   **Compilação de Distribuição do Frontend (React):**
    `npm run build --prefix src/RailFactory.Frontend/App`
*   **Execução Integrada da Solução via .NET Aspire:**
    `dotnet run --project src/RailFactory.AppHost/RailFactory.AppHost.csproj`
*   **Execução de Testes C# (xUnit):**
    `dotnet test src/RailFactory.Fork.sln`
*   **Execução de Testes Unitários de Frontend (Vitest):**
    `npm run test --prefix src/RailFactory.Frontend/App`
*   **Execução de Testes de Automação E2E (Playwright):**
    `npm run test:e2e --prefix src/RailFactory.Frontend/App`
