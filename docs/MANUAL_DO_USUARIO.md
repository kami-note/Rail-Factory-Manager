# 6. MANUAL DO USUÁRIO

Este documento constitui o manual de operação oficial da plataforma **Rail Factory**, fornecendo diretrizes detalhadas para a execução de processos de manufatura, suprimentos, controle de estoques, logística, faturamento fiscal e administração de segurança e acessos (IAM/RBAC).

Os procedimentos descritos neste guia destinam-se a operadores de almoxarifado, planejadores de produção, faturistas, engenheiros de processo e administradores do sistema. A operação dos módulos deve seguir estritamente as regras de negócio e validações documentadas.

Para documentação técnica complementar, arquitetura de sistemas e contratos de rotas REST, consultar o [Mapeamento de Fluxos de Trabalho](file:///home/levi/Projects/Rail-Factory-Fork/docs/FLUXOS_DE_TRABALHO.md).

---

## 🧭 Índice dos Procedimentos Operacionais
- [6.1. Como Configurar a Base de Dados, Organizações e Acessar o Sistema](#61-como-configurar-a-base-de-dados-organizações-e-acessar-o-sistema)
- [6.2. Como Gerenciar Recebimentos de Notas Fiscais e Mapear SKUs](#62-como-gerenciar-recebimentos-de-notas-fiscais-e-mapear-skus)
- [6.3. Como Efetuar a Conferência Física Cega e Consultar Saldos em Estoque](#63-como-efetuar-a-conferência-física-cega-e-consultar-saldos-em-estoque)
- [6.4. Como Cadastrar Recursos de Fábrica, Fichas Técnicas (BOM) e Estimar Custos](#64-como-cadastrar-recursos-de-fábrica-fichas-técnicas-bom-e-estimar-custos)
- [6.5. Como Planejar, Executar e Apontar Ordens de Produção (Chão de Fábrica)](#65-como-planejar-executar-e-apontar-ordens-de-produção-chão-de-fábrica)
- [6.6. Como Cadastrar Transportadoras, Iniciar Ordens de Expedição e Despachar Embarques](#66-como-cadastrar-transportadoras-iniciar-ordens-de-expedição-e-despachar-embarques)
- [6.7. Como Fiscalizar Emissões de Documentos, Gerenciar Frota, RH e Administrar Perfis (IAM)](#67-como-fiscalizar-emissões-de-documentos-gerenciar-frota-rh-e-administrar-perfis-iam)
- [📋 FAQ — Resolução de Problemas Gerais](#-faq-resolução-de-problemas-gerais)

---

## 6.1. Como Configurar a Base de Dados, Organizações e Acessar o Sistema

Este fluxo compreende as etapas de inicialização estrutural da base de dados, a configuração de novas instâncias de organizações (*tenants*) e os procedimentos de login seguro.

### 6.1.1. Como Inicializar o Banco de Dados ( rota `/setup` )

A inicialização do banco de dados prepara a base de dados relacional para uso. O objetivo deste procedimento é executar as migrações estruturais da base de dados e carregar os dados cadastrais padrões (*seeds*) do sistema. Por se tratar de um assistente inicial de sistema, o acesso à tela é feito diretamente digitando a rota `/setup` na barra de endereço do navegador. Não há uma opção correspondente no menu lateral, uma vez que esta operação é restrita às etapas de implantação do sistema. A interface dispõe de um botão central rotulado como **Initialize Database**, encarregado de disparar a rotina de criação de tabelas e cargas preliminares no backend. Para realizar esta operação, siga o seguinte procedimento:

1. Digitar o endereço `/setup` na barra de navegação do navegador e pressionar Enter.
2. Clicar no botão **"Initialize Database"** localizado no painel central da tela.
3. Acompanhar a barra de progresso e aguardar o processamento completo exibido na tela. O sistema redirecionará o usuário à página de login automaticamente após o término.

Em caso de indisponibilidade ou falha de credenciais do servidor PostgreSQL, a operação será interrompida e uma mensagem de erro com o diagnóstico técnico correspondente será apresentada na tela.

---

### 6.1.2. Como Cadastrar e Configurar Organizações (Multitenancy) ( rota `/app/settings/tenants` )

O cadastro de organizações permite registrar novas filiais ou parceiras corporativas na base de dados para garantir o isolamento lógico dos dados. Para acessar esta tela sem a URL direta, deve-se expandir a seção **CONFIGURAÇÕES** no menu lateral esquerdo e clicar na opção **EMPRESAS**. A interface possui uma tabela de listagem geral de organizações e o botão rotulado como **Novo Tenant**, que abre a gaveta lateral direita contendo o formulário de cadastro. Esse formulário exige o preenchimento do **Nome da Organização** (representando a razão social da filial), o **Código da Organização** (um identificador curto, alfanumérico e exclusivo usado para a resolução de rotas, como `dev` ou `acme`) e a **String de Conexão** (dados adicionais de credenciais e host de banco de dados específico, caso necessário). O salvamento e teste de conectividade são ativados pelo botão **Salvar**. Para realizar este cadastro, siga o seguinte procedimento:

1. Expandir a seção **CONFIGURAÇÕES** na barra lateral esquerda e clicar na opção **EMPRESAS** (ou navegar até a rota `/app/settings/tenants`).
2. Clicar no botão **"Novo Tenant"** localizado no canto superior direito da tela.
3. Preencher o campo **Nome da Organização** com a razão social da filial, o campo **Código da Organização** com o código de acesso curto e o campo **String de Conexão** nos respectivos campos da gaveta lateral.
4. Clicar no botão **"Salvar"** para registrar a organização no banco de dados corporativo.

O código da organização fornecido deve ser único em relação aos demais tenants ativos, digitado obrigatoriamente em letras minúsculas e sem espaços ou caracteres especiais.

---

### 6.1.3. Como Realizar o Login e Autenticação ( rota `/` )

O login do usuário permite selecionar a organização de trabalho e autenticar o operador via protocolo OAuth. O acesso à tela ocorre digitando o endereço eletrônico principal do sistema (rota `/`). A interface apresenta o campo **Código da Organização** para informar o código da filial cadastrada e o botão **Entrar com Google** que redireciona a autenticação para os servidores do Google. Para realizar o acesso seguro, siga o seguinte procedimento:

1. Acessar a rota `/` no navegador.
2. Digitar o código de acesso da organização correspondente no campo **Código da Organização**.
3. Clicar no botão **"Entrar com Google"** para ser redirecionado à página de verificação de conta do Google.
4. Concluir a validação com as credenciais do Google para gerar o token JWT e retornar à tela inicial do painel logado do sistema.

O código de organização informado no formulário deve corresponder a um tenant ativo e inicializado previamente no banco de dados corporativo.

---

## 6.2. Como Gerenciar Recebimentos de Notas Fiscais e Mapear SKUs

Este fluxo descreve como importar arquivos XML faturados por fornecedores e configurar associações de códigos de materiais para alinhar com o catálogo interno da fábrica.

### 6.2.1. Como Monitorar Indicadores no Painel Geral (Dashboard) ( rota `/app` )

O painel de monitoramento operacional centraliza o andamento dos centros de trabalho e fornece indicadores visuais de saldos pendentes nas docas. Para acessar esta tela sem a URL direta, deve-se expandir a seção **GERAL** no menu lateral esquerdo e clicar na opção **INÍCIO**. O painel exibe cards estatísticos contendo a contagem total de itens em doca, eficiência geral da fábrica e ordens ativas. Para monitorar as informações, siga o seguinte procedimento:

1. Expandir a seção **GERAL** na barra lateral esquerda e clicar em **"INÍCIO"** (ou navegar até a rota `/app`).
2. Avaliar os gráficos de produtividade em tempo real e a barra de status de estoques pendentes de conferência.
3. Clicar nas métricas específicas para acessar o detalhamento do módulo correspondente.

Os dados exibidos na tela são atualizados de forma automática baseados nas transações de recebimentos e apontamentos de ordens efetuadas pelos operadores.

---

### 6.2.2. Como Acessar a Fila de Recebimento de Notas Fiscais (NF-e) ( rota `/app/receipts` )

A fila de recebimento de notas fiscais lista todos os documentos importados dos fornecedores. Para acessar este painel sem a URL direta, deve-se expandir a seção **SUPRIMENTOS** na barra lateral esquerda e selecionar a opção **RECEBIMENTOS**. A interface exibe uma tabela de registros contendo colunas como Número da NF-e, Nome do Emitente, Valor Total e o Status do Recebimento. Além disso, disponibiliza o botão **IMPORTAR XML** no topo direito. Para acessar as notas, siga o seguinte procedimento:

1. Expandir a seção **"SUPRIMENTOS"** na barra lateral esquerda e clicar em **"RECEBIMENTOS"** (ou acessar a rota `/app/receipts`).
2. Analisar o status operacional das notas na listagem (como Pendente, Em Conferência ou Aprovado).
3. Utilizar o campo de busca no topo da tabela para filtrar notas específicas por número ou CNPJ do emitente.

---

### 6.2.3. Como Realizar a Importação de Arquivos XML de NF-e ( rota `/app/import-xml` ou `/app/receipts` - Gaveta XML )

A gaveta lateral de importação realiza a carga e validação da Nota Fiscal Eletrônica (NF-e) emitida pelo fornecedor. Para acessar esta tela sem a URL direta, deve-se expandir a seção **SUPRIMENTOS** no menu lateral, clicar em **RECEBIMENTOS** e clicar no botão **IMPORTAR XML** (ou digitar diretamente a rota `/app/import-xml` no navegador). Ela possui a **Área de Upload** (região pontilhada destinada a receber o arrasto do arquivo `.xml`), o botão **Confirmar e Importar** que envia o arquivo para processamento no backend, e o botão **Cancelar** que fecha a gaveta sem realizar alterações. Para realizar a importação de um novo XML, siga o seguinte procedimento:

1. Na fila de recebimentos de notas fiscais (acessada em **SUPRIMENTOS** -> **RECEBIMENTOS**), clicar no botão **"IMPORTAR XML"** localizado no canto superior direito.
2. Arrastar o arquivo XML da nota fiscal correspondente e soltar sobre a área de upload delimitada (ou clicar na área para selecionar o arquivo localmente).
3. Clicar no botão **"Confirmar e Importar"** para processar a nota fiscal.

O arquivo XML da nota fiscal é validado pelo sistema. XMLs que não possuam assinatura digital válida ou cujo CNPJ destinatário seja diferente da organização de trabalho activa serão rejeitados na importação.

---

### 6.2.4. Como Associar SKUs de Fornecedores ao Catálogo de Materiais Internos ( rota `/app/supply-chain/association` )

O workbench de associação vincula os SKUs declarados nas notas de fornecedores aos itens catalogados no inventário da empresa e configura o fator de conversão de unidades de medida. Para acessar esta tela sem a URL direta, deve-se expandir a seção **SUPRIMENTOS** na barra lateral esquerda e clicar na opção **ASSOCIAÇÃO DE SKUs**, ou clicar no link vermelho **"Associar SKU"** ao lado da nota fiscal correspondente na fila de recebimentos de notas fiscais. A interface disponibiliza o campo de seleção **Material Interno** (dropdown para escolher o item do catálogo), o campo **Fator de Conversão** (campo numérico decimal que multiplica a unidade da nota fiscal, como 12 para caixas com 12 peças), o botão **Confirmar Associação** que grava o mapeamento, e o botão **Cadastrar Material** para incluir insumos novos. Para realizar o mapeamento do item, siga o seguinte procedimento:

1. Expandir a seção **SUPRIMENTOS** na barra lateral esquerda e clicar em **"ASSOCIAÇÃO DE SKUs"** (ou clicar no link vermelho **"Associar SKU"** localizado na linha de uma nota pendente na fila de **RECEBIMENTOS**).
2. Selecionar o item do fornecedor não mapeado na listagem disposta à esquerda da interface.
3. Escolher o insumo correspondente no seletor **Material Interno** e digitar o fator de conversão de unidades no campo **Fator de Conversão**.
4. Clicar no botão **"Confirmar Associação"** para gravar as definições do item.

O fator de conversão de unidades informado deve conter obrigatoriamente um valor decimal estritamente positivo (maior que zero).

---

## 6.3. Como Efetuar a Conferência Física Cega e Consultar Saldos em Estoque

Este fluxo descreve como realizar a conferência cega (física) das mercadorias descarregadas nas docas e consultar os saldos no inventário.

### 6.3.1. Como Processar a Contagem Blindada (Conferência Cega) ( rota `/app/receipts` - Fila )

A fila de recebimentos de notas fiscais permite iniciar e validar a conferência física cega dos insumos. Para acessar esta tela sem a URL direta, deve-se expandir a seção **SUPRIMENTOS** no menu lateral esquerdo e clicar em **RECEBIMENTOS**. O botão **Iniciar Conferência** altera o status da nota para `InConference` e bloqueia o documento para contagem do operador. No formulário de conferência, o campo **Quantidade Contada** recebe a quantidade contada nas docas (com valores originais do XML omitidos para garantir a neutralidade), e o botão **Concluir Conferência** finaliza a contagem. Para realizar a contagem física, siga o seguinte procedimento:

1. Expandir a seção **"SUPRIMENTOS"** no menu lateral esquerdo, clicar em **"RECEBIMENTOS"**, localizar a nota fiscal da carga nas docas e clicar no botão **"Iniciar Conferência"** na linha correspondente.
2. Efetuar a contagem física das mercadorias no armazém e digitar a quantidade contada no respectivo campo do formulário de conferência.
3. Clicar no botão **"Concluir Conferência"** para realizar o processamento de validação automática.

Se as quantidades contadas baterem com as informadas no XML original, o status da nota muda para `Approved` e as quantidades entram no estoque como disponíveis (`Available`). Se houver divergências, o status da nota passa a ser `Divergent` e os insumos entram no estoque retidos na quarentena sob o status de lote bloqueado (`Blocked`).

---

### 6.3.2. Como Consultar Lotes e Trilha Cronológica de Saldos de Estoque ( rota `/app/inventory` )

A página de estoques permite consultar saldos físicos, lotes em quarentena e rastrear o histórico de movimentações. Para acessar esta tela sem a URL direta, deve-se expandir a seção **SUPRIMENTOS** no menu lateral esquerdo e clicar na opção **ESTOQUE**. A interface apresenta o campo **Barra de Busca** (pesquisa de lotes por insumo, fabricante, nota fiscal ou fornecedor), o filtro **Ocultar estoques zerados** (esconde registros sem saldo físico), o **Seletor de Layout** (alterna entre tabela e cards de materiais), o botão de **Histórico** (ícone de relógio para ver o histórico cronológico de movimentação do lote) e o botão **Liberar Lote** (ação de liberação rápida para supervisores). Clicar no código do material redireciona o usuário para a rota de detalhamento `/app/inventory/materials/{código_do_material}`. Para gerenciar os lotes de estoque, siga o seguinte procedimento:

1. Expandir a seção **"SUPRIMENTOS"** na barra lateral esquerda e clicar em **"ESTOQUE"** (ou acessar a rota `/app/inventory`).
2. Informar os dados de busca na barra de filtros e aplicar os parâmetros de exibição desejados.
3. Clicar no ícone de relógio (**"Histórico"**) na linha do lote para visualizar a trilha de movimentações do material, ou no código do material para acessar os detalhes em `/app/inventory/materials/{código_do_material}`.
4. Clicar no botão **"Liberar Lote"** caso possua perfil de supervisor e precise autorizar o uso de um lote bloqueado.

A liberação de lotes bloqueados na quarentena só é permitida a usuários com cargo de supervisor ou superior cadastrados no IAM.

---

## 6.4. Como Cadastrar Recursos de Fábrica, Fichas Técnicas (BOM) e Estimar Custos

Este fluxo engloba as definições de Centros de Trabalho e fichas técnicas (BOM), com suporte a estimativa de custos teóricos de fabricação.

### 6.4.1. Como Cadastrar Centros de Trabalho e Suas Capacidades ( rota `/app/production/work-centers` )

O cadastro de centros de trabalho registra as linhas de produção da fábrica. Para acessar esta tela sem a URL direta, deve-se expandir a seção **PRODUÇÃO** na barra lateral esquerda e clicar em **CENTROS DE TRABALHO**. O botão **Novo Centro** abre a gaveta de cadastro contendo os campos **Nome do Centro** (código de identificação do recurso, como `WC-COR-01`), **Capacidade Horária** (volume nominal processado por hora de máquina), **Custo por Hora** (valor de operação do recurso) e **Eficiência Nominal** (rendimento de linha planejado, como 95%). O botão **Salvar** grava e valida o recurso. Para cadastrar um recurso, siga o seguinte procedimento:

1. Expandir a seção **"PRODUÇÃO"** na barra lateral esquerda, clicar em **"CENTROS DE TRABALHO"** e depois no botão **"Novo Centro"**.
2. Preencher o código do centro, a capacidade por hora, o custo direto por hora e a eficiência nominal da linha nos respectivos campos do formulário.
3. Clicar no botão **"Salvar"** para registrar o recurso de fábrica.

Os campos numéricos de custo operacional, eficiência e capacidade do recurso de fábrica não podem possuir valores nulos ou negativos.

---

### 6.4.2. Como Criar uma Nova Ficha Técnica (BOM) ( rota `/app/production/boms` - Modal )

A criação da ficha técnica (BOM) define a estrutura principal de insumos de um produto acabado e seu lote padrão de processo. Para abrir a interface de cadastro sem a URL direta, deve-se expandir a seção **PRODUÇÃO** na barra lateral esquerda, clicar em **ESTRUTURA DE PRODUTOS** e acionar o botão **Nova BOM** no canto superior direito. O modal requer a seleção do **Produto** (campo autocomplete que busca insumos cadastrados na categoria de Produto Acabado/FinishedGood) e a definição do **Lote Padrão (Batch Size)** (campo numérico decimal, com valor padrão de `1.0`, que representa a base quantitativa de rendimento da receita). Para criar a estrutura, siga o seguinte procedimento:

1. Expandir a seção **"PRODUÇÃO"** na barra lateral esquerda, clicar em **"ESTRUTURA DE PRODUTOS"** e clicar em **"Nova BOM"**.
2. Digitar o código ou nome do produto acabado no campo **Produto** e selecionar a sugestão exibida pelo autocomplete.
3. Informar o volume correspondente no campo **Lote Padrão (Batch Size)** (caso seja diferente de 1.0).
4. Clicar no botão **"Criar BOM"** para gravar o cabeçalho e disponibilizar a inserção de componentes.

---

### 6.4.2.1. Como Adicionar Componentes à Ficha Técnica (BOM) ( rota `/app/production/boms` - Componentes )

Após criar a ficha técnica, deve-se vincular as matérias-primas e dosagens necessárias. Na listagem de estruturas de produtos, clicar sobre o card da receita para expandir seu conteúdo e disponibilizar o painel de cadastro de componentes. A interface oferece os campos **Código do material** (autocomplete restrito a itens de matéria-prima/RawMaterial), **Qtd** (quantidade consumida para o lote padrão) e **Perda (%)** (fator de perda técnica esperada, limitado ao intervalo de 0% a 99.99%). O campo de unidade de medida (UM) é preenchido automaticamente com base no cadastro do catálogo. Para adicionar componentes, siga o seguinte procedimento:

1. Na tela de estruturas de produtos, expandir a BOM correspondente e acessar a área de adição de itens.
2. Informar o insumo no campo **Código do material** e selecionar a sugestão correspondente.
3. Digitar a quantidade líquida requerida no campo **Qtd** e a perda no campo **Perda (%)**.
4. Clicar no botão **"Salvar"** (ou pressionar Enter) para registrar o componente na estrutura da receita.
5. Após concluir a inserção de todos os itens, clicar no botão **"Ativar"** no card da receita para homologar a versão e liberá-la para as ordens de produção.

---

### 6.4.3. Como Estruturar e Clonar Versões de Fichas Técnicas ( rota `/app/production/boms` - Tabela )

O painel de receitas lista as estruturas ativas de produto acabado. Para acessar esta tela sem a URL direta, deve-se expandir a seção **PRODUÇÃO** na barra lateral esquerda e clicar em **ESTRUTURA DE PRODUTOS**. A interface disponibiliza uma listagem geral de estruturas e o botão **Clonar** localizado na linha de cada card. Esse botão duplica toda a estrutura de insumos e parâmetros de uma receita ativa para uma nova versão em status de rascunho. Para clonar a receita, siga o seguinte procedimento:

1. Expandir a seção **"PRODUÇÃO"** na barra lateral esquerda e clicar em **"ESTRUTURA DE PRODUTOS"**.
2. Localizar a receita correspondente do produto acabado na listagem geral.
3. Clicar no botão **"Clonar"** localizado no painel da receita ativa.
4. Acessar a nova versão gerada como rascunho na listagem para realizar as adequações necessárias.

---

### 6.4.4. Como Simular Custos de Insumos via Cost Rollup ( rota `/app/production/boms` - Cost Rollup )

A simulação de custos (Cost Rollup) estima o custo financeiro unitário do produto acabado com base nas notas fiscais faturadas. Para acessar esta tela sem a URL direta, deve-se expandir a seção **PRODUÇÃO** no menu lateral esquerdo, selecionar **ESTRUTURA DE PRODUTOS** e clicar no botão **Custo** (representado por um ícone de cifrão) no card da receita correspondente. O modal exibe a listagem de componentes, as quantidades reajustadas pela perda técnica, os valores unitários do último recebimento fiscal e o valor total acumulado do lote padrão. Para realizar a simulação, siga o seguinte procedimento:

1. Expandir a seção **"PRODUÇÃO"** na barra lateral esquerda, clicar em **"ESTRUTURA DE PRODUTOS"** e selecionar o produto acabado.
2. Clicar no botão **"Custo"** (ícone de cifrão) localizado no card do produto.
3. Analisar o detalhamento dos custos individuais e consolidados exibidos no modal.

---

## 6.5. Como Planejar, Executar e Apontar Ordens de Produção (Chão de Fábrica)

Este fluxo gerencia o ciclo de vida das Ordens de Produção (OP), o consumo de materiais e a inspeção de qualidade de peças finais.

### 6.5.1. Como Planejar e Registrar uma Nova Ordem de Produção (OP) ( rota `/app/production/orders` - Modal )

A fila de ordens gerencia o planejamento e execução no chão de fábrica. Para acessar esta tela sem a URL direta, deve-se expandir a seção **PRODUÇÃO** na barra lateral esquerda e selecionar **ORDENS DE PRODUÇÃO**. O botão **Nova Ordem** abre o modal com os campos **BOM** (dropdown contendo as fichas técnicas em status ativo), **Centro de Trabalho** (dropdown com linhas produtivas disponíveis em status ativo) e **Qtd Planejada** (volume da tiragem, com valor mínimo de 1). Ao selecionar a BOM, o modal exibe um resumo contendo a lista de matérias-primas e proporções planejadas no rodapé. Para planejar uma nova OP, siga o seguinte procedimento:

1. Expandir a seção **"PRODUÇÃO"** na barra lateral esquerda e clicar em **"ORDENS DE PRODUÇÃO"** (ou acessar a rota `/app/production/orders`).
2. Clicar no botão **"Nova Ordem"** no canto superior direito para abrir o modal de cadastro.
3. Escolher a ficha técnica no seletor **BOM**, a linha de manufatura no seletor **Centro de Trabalho** e informar a tiragem no campo **Qtd Planejada**.
4. Clicar em **"Criar Ordem"** para registrar a nova ordem no status `Created`.

---

### 6.5.1.1. Como Alocar Estoque e Liberar Ordem de Produção ( rota `/app/production/orders` - Detalhes da OP )

A liberação de uma OP criada realiza o empenho e reserva física dos insumos e componentes estruturados na BOM para garantir o suprimento da linha. Para realizar esta operação, o planejador deve acessar a fila de ordens e clicar sobre o registro desejado para abrir o painel de detalhes da ordem de produção. A interface disponibiliza as ações de ciclo de vida e um fluxo indicador de etapas de status da ordem. Para liberar a ordem, siga o seguinte procedimento:

1. Na fila de ordens de produção, clicar sobre a linha da OP correspondente que se encontre em status de rascunho (`Created`).
2. No painel de detalhes, clicar no botão **"Liberar"** (o status da OP passará para `Released` se houver saldo suficiente no estoque).

A liberação da Ordem de Produção falhará se o estoque de insumos e matérias-primas disponíveis (`Available`) no almoxarifado for menor do que as demandas obrigatórias da receita.

---

### 6.5.1.2. Como Iniciar a Execução da Ordem de Produção ( rota `/app/production/orders` - Detalhes da OP )

O início de execução indica que os insumos reservados foram fisicamente retirados do estoque e a máquina/linha de montagem iniciou o processamento das peças. A operação é realizada no mesmo painel de detalhes da ordem e é permitida apenas para documentos que se encontrem em status de liberação ativa (`Released`). Para iniciar o processo, siga o seguinte procedimento:

1. Acessar o painel de detalhes da ordem de produção liberada na fila.
2. Clicar no botão **"Iniciar"** (o status da OP passará para `InExecution`, desbloqueando as ferramentas de apontamento e consumo de materiais).

---

### 6.5.1.3. Como Apontar Consumo e Scrap de Materiais ( rota `/app/production/orders` - Aba Ajustes )

Durante a execução da OP, o operador pode apontar consumos reais e perdas por refugo/descarte (scrap). Esta operação é realizada no painel de detalhes da OP ativa, navegando até a aba **Ajustes**. A interface oferece **chips de preenchimento rápido** contendo os códigos das matérias-primas cadastradas na BOM da ordem, além de campos para **Código do material**, **Quantidade** e, no caso de scrap, um campo multiline para **Motivo do scrap**. O consumo apontado manualmente substitui a baixa automática (*backflush*) do material correspondente no fechamento da ordem. Para realizar os apontamentos, siga o seguinte procedimento:

#### Para registrar consumo de material:
1. No painel de detalhes da ordem em execução, acessar a aba **"Ajustes"** e localizar a seção **Consumo Manual**.
2. Clicar em um dos chips de matérias-primas da BOM para preencher os dados automaticamente (ou digitar no campo de busca de material).
3. Confirmar a quantidade real consumida no campo **Quantidade** e clicar no botão **"Registrar Consumo"**.

#### Para registrar descarte (scrap) de material:
1. No painel de detalhes da ordem em execução, acessar a aba **"Ajustes"** e localizar a seção **Registrar Scrap**.
2. Selecionar o material utilizando os chips rápidos ou o campo de busca.
3. Informar o volume no campo **Quantidade** e preencher o motivo do refugo no campo **Motivo do scrap**.
4. Clicar no botão **"Registrar Scrap"** para computar o desperdício técnico no sistema.

---

### 6.5.1.4. Como Realizar Inspeção de Qualidade e Concluir a Ordem ( rota `/app/production/orders` - Aba Concluir )

A conclusão de uma ordem exige o apontamento de qualidade e consolida a entrada do produto acabado no estoque. No painel de detalhes da OP, acessar a aba **Aprovar & Concluir**. A interface exibe a tabela de materiais que serão baixados automaticamente por *backflush* (excluindo os materiais que receberam apontamento de consumo manual na aba Ajustes). Apresenta os botões **Aprovado** e **Reprovado** para a inspeção de qualidade, o campo de texto de **Observações** e o botão **Aprovar e Concluir Ordem** ou **Registrar Reprovação**. O nome do usuário autenticado no sistema é registrado automaticamente no campo **Inspecionado por**. Para realizar a operação, siga o seguinte procedimento:

1. No painel de detalhes da ordem em execução, acessar a aba **"Aprovar & Concluir"**.
2. Analisar a tabela de materiais que sofrerão baixa automática por backflush.
3. Selecionar o resultado da inspeção clicando no botão **"Aprovado"** ou **"Reprovado"**.
4. Digitar os detalhes do lote ou defeitos encontrados no campo **Observações**.
5. Clicar no botão **"Aprovar e Concluir Ordem"** (se aprovado, finalizando a OP e registrando a entrada física no estoque) ou no botão **"Registrar Reprovação"** (se reprovado, registrando a perda da produção).

---

## 6.6. Como Cadastrar Transportadoras, Iniciar Ordens de Expedição e Despachar Embarques

Este fluxo engloba o credenciamento de parceiros logísticos, expedição de vendas faturadas e embarque rodoviário de cargas.

### 6.6.1. Como Registrar e Visualizar Transportadoras Credenciadas ( rota `/app/logistics/carriers` )

A fila logística lista as transportadoras homologadas na empresa. Para acessar esta listagem sem a URL direta, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **TRANSPORTADORAS**. A interface apresenta uma listagem geral de parceiros com colunas de Razão Social e CNPJ e o botão **Nova Transportadora**, encarregado de abrir a gaveta lateral de cadastro de parceiro logístico. Para gerenciar as transportadoras, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda e clicar em **"TRANSPORTADORAS"** (ou acessar a rota `/app/logistics/carriers`).
2. Analisar os registros das transportadoras habilitadas para atendimento.
3. Clicar no botão **"Nova Transportadora"** no canto superior direito para abrir o formulário de cadastro.

---

### 6.6.2. Como Efetuar o Cadastro Rápido de Novas Transportadoras ( rota `/app/logistics/carriers` - Modal )

O formulário de credenciamento registra os dados corporativos da nova transportadora. Ele é acessado clicando no botão **Nova Transportadora** na fila de transportadoras. O formulário exige o preenchimento dos campos **Razão Social** (nome comercial), **CNPJ** (cadastro nacional, digitando apenas números), **Endereço** (localização física da sede) e **Webhook URL** (endereço de callback para sincronização de tracking de entrega). O botão **Salvar** grava os dados informados. Para realizar o cadastro da transportadora, siga o seguinte procedimento:

1. Na tela de transportadoras (acessada em **LOGÍSTICA** -> **TRANSPORTADORAS**), clicar no botão **"Nova Transportadora"** no canto superior direito para abrir a gaveta lateral.
2. Informar a razão social no campo **Razão Social**, CNPJ numérico limpo (apenas os 14 dígitos) no campo **CNPJ**, endereço completo no campo **Endereço** e a URL de webhook no campo **Webhook URL**.
3. Clicar no botão **"Salvar"** para confirmar a gravação dos dados.

O CNPJ inserido passa por limpeza e validação matemática de dígitos verificadores. Formatos com tamanho de string incorreto ou dígitos inválidos bloquearão a gravação.

---

### 6.6.3. Como Cadastrar Nova Ordem de Expedição ( rota `/app/logistics/shipment-orders` - Modal )

A criação de ordens de expedição registra os pedidos de venda que demandam despacho físico e faturamento fiscal. Para acessar esta tela sem a URL direta, deve-se expandir a seção **LOGÍSTICA** no menu lateral esquerdo, clicar em **EXPEDIÇÃO** e acionar o botão **Nova Ordem** no canto superior direito. O processo de criação é dividido em duas fases distintas no modal:

#### Fase 1: Informações de Cabeçalho e Destinatário
A primeira fase do formulário exige o preenchimento dos seguintes dados cadastrais e fiscais do destinatário da carga:
- **Destinatário:** Nome completo ou Razão Social do cliente no campo **Nome** e o cadastro correspondente no campo **CPF / CNPJ** (validado quanto à integridade matemática de dígitos).
- **Contato:** E-mail de notificação de rastreamento no campo **E-mail** (validado no formato correspondente) e a identificação estadual opcional no campo **Inscrição Estadual**.
- **Endereço:** Campos obrigatórios para **Rua**, **Número**, **Bairro**, **Cidade**, **Estado** (seletor alfanumérico restrito a 2 caracteres da UF correspondente) e **CEP** (limpo de formatação).
- **Regras Fiscais:** Campo **Natureza da Operação** (default "Venda de mercadoria") e o seletor **Modalidade do Frete** (opções numéricas correspondentes aos códigos oficiais).

#### Fase 2: Inserção de Itens da Ordem
Após salvar o cabeçalho, a tela habilita a inserção de produtos no pedido:
- **Dados do Produto:** Campo de pesquisa de produto (busca autocomplete de materiais do catálogo) e o campo **Quantidade** (unidade de medida derivada automaticamente).
- **Parâmetros de Carga:** Definição física de **Peso (Kg)** e **Volume (m³)** unitário do item.
- **Tributação Fiscal:** Campos para **CFOP**, **NCM**, **Valor Unitário**, **Base ICMS**, **Alíquota ICMS (%)**, **Origem ICMS**, **CST ICMS**, **CST PIS**, **CST COFINS**, **Alíquota IPI (%)** e **CST IPI**.

Para realizar este cadastro de duas fases, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda, clicar na opção **"EXPEDIÇÃO"** e clicar em **"Nova Ordem"**.
2. Preencher todos os dados cadastrais, de endereço e regras fiscais do destinatário no formulário da Fase 1 e clicar em **"Salvar Ordem"** para liberar a Fase 2.
3. No formulário de inserção de itens, selecionar o material acabado e preencher os dados de pesos, volumes e tributação do item.
4. Clicar no botão **"Salvar Item"** (ou **"Adicionar"**) para anexar o produto na tabela inferior da ordem. Repetir para outros produtos se necessário.
5. Clicar no botão **"Concluir Ordem"** para salvar o documento em status de Rascunho (`Draft`).

---

### 6.6.3.1. Como Gerenciar a Separação, Embalagem e Liberação de Ordens de Expedição ( rota `/app/logistics/shipment-orders` )

As ordens de expedição cadastradas seguem um fluxo estruturado de separação física no estoque antes da liberação final para transporte. Para acessar a fila, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **EXPEDIÇÃO**. Ao clicar sobre um pedido, a gaveta lateral de detalhes é aberta, exibindo os dados fiscais e as ações de transição de status disponíveis. O documento percorre a seguinte máquina de estados:
- `Draft` (Rascunho): Pedido recém-cadastrado. Ação **"Iniciar Separação"** altera o status para `Picking`.
- `Picking` (Separação): Insumos estão sendo recolhidos. Ação **"Iniciar Embalagem"** altera o status para `Packing`.
- `Packing` (Embalagem): Produtos são paletizados. Ação **"Marcar Pronto"** altera o status para `ReadyToShip`.
- `ReadyToShip` (Pronto para Despacho): Disponibiliza o pedido para carregamento e consolidação logísticos na fila de despachos.

Para transitar o status das ordens de expedição, siga o seguinte procedimento:

1. Na tela de **EXPEDIÇÃO**, localizar o pedido correspondente e clicar sobre sua linha para abrir o painel de detalhes.
2. No painel, acionar a respectiva ação de transição de status de acordo com a etapa de preparação do material (ex: **"Iniciar Separação"**).
3. Confirmar a operação na caixa de diálogo de segurança exibida na tela.
4. Caso necessite cancelar a expedição, clicar no botão **"Cancelar Ordem"** (ação permitida apenas antes do envio definitivo).

---

### 6.6.4. Como Liberar e Faturar Ordens de Expedição da Fila ( rota `/app/logistics/shipment-orders` )

A consolidação de cargas permite unificar múltiplos pedidos de expedição prontos em uma mesma viagem logísticas. Para acessar esta fila sem a URL direta, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **EXPEDIÇÃO**. A tabela de pedidos disponibiliza checkboxes de seleção rápida na coluna esquerda dos registros em status `ReadyToShip`. Para selecionar os embarques, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda e clicar em **"EXPEDIÇÃO"** (ou acessar a rota `/app/logistics/shipment-orders`).
2. Filtrar e localizar os pedidos prontos para expedição na listagem geral.
3. Marcar os checkboxes correspondentes localizados na coluna esquerda da tabela para agrupar as ordens destinadas à mesma viagem de entrega.
4. Clicar no botão **"Novo Despacho"** no topo esquerdo da tabela para iniciar a consolidação dos dados de trânsito.

---

### 6.6.5. Como Emitir Despachos Logísticos e Visualizar o DAMDFE ( rota `/app/logistics/dispatches` )

O painel de despachos gerencia a consolidação física e fiscal de saídas. Para acessar este painel sem a URL direta, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **DESPACHOS**. O botão **Novo Despacho** abre o modal de alocação de recursos rodoviários. O botão **Expedir** altera o status do despacho para enviado (`Shipped`), realizando a baixa no inventário, enviando a NF-e à SEFAZ e disparando callbacks de webhooks. O botão **Imprimir DAMDFE** abre o manifesto de trânsito A4 na tela. Para processar o despacho, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda e clicar em **"DESPACHOS"** (ou acessar a rota `/app/logistics/dispatches`).
2. Clicar no botão **"Novo Despacho"** no topo direito para abrir o modal de alocação de frota e criar a viagem.
3. Localizar o manifesto criado na fila e clicar no botão **"Expedir"** na linha correspondente para realizar a baixa no estoque de acabados.
4. Clicar no botão **"Imprimir DAMDFE"** na linha correspondente para obter a guia de trânsito física para transporte.

---

### 6.6.6. Como Consolidar Dados de Viagem (Carga, Veículo e Motorista) ( rota `/app/logistics/dispatches` - Modal )

O modal de consolidação vincula os recursos físicos e terceiros ao despacho de entrega. Ele abre automaticamente ao clicar no botão **Novo Despacho** na tela de despachos. Requer a seleção da **Transportadora** (dropdown de parceiros credenciados), o **Veículo** (dropdown contendo a placa do caminhão alocado) e o **Motorista** (dropdown com o condutor selecionado no RH). O botão **Salvar** consolida as informações. Para realizar a alocação do frete, siga o seguinte procedimento:

1. Na tela de despachos (acessada em **LOGÍSTICA** -> **DESPACHOS**), clicar no botão **"Novo Despacho"** para abrir o modal central.
2. Selecionar a transportadora credenciada no dropdown **Transportadora**.
3. Alocar o veículo correspondente no dropdown **Veículo** e o motorista habilitado correspondente no dropdown **Motorista**.
4. Clicar no botão **"Salvar"** para consolidar o despacho logístico.

---

## 6.7. Como Fiscalizar Emissões de Documentos, Gerenciar Frota, RH e Administrar Perfis (IAM)

Este fluxo descreve como monitorar pendências fiscais da SEFAZ, gerenciar a frota operacional própria, o RH e controlar acessos de segurança (IAM).

### 6.7.1. Como Transmitir e Fiscalizar Notas Fiscais Eletrônicas junto à SEFAZ ( rota `/app/logistics/nfe-monitor` )

O monitor fiscal rastreia a transmissão das notas fiscais eletrônicas de venda enviadas à receita federal. Para acessar a tela sem a URL direta, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **MONITOR NF-e**. A interface disponibiliza o status de aprovação de cada documento e conta com o botão **Reenviar**, que realiza a retransmissão de notas fiscais que falharam por quedas de comunicação. Para processar a retransmissão, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda, clicar em **"MONITOR NF-e"** (ou acessar a rota `/app/logistics/nfe-monitor`) e localizar a nota com falha na listagem.
2. Clicar no botão **"Reenviar"** localizado no final da linha do registro correspondente para restabelecer a comunicação com o servidor da SEFAZ.
3. Aguardar a resposta da receita federal e certificar a mudança do status da NF-e para autorizado na tabela.

---

### 6.7.2. Como Definir Perfis Tributários e CFOPs da Organização ( rota `/app/logistics/fiscal-settings` )

A parametrização fiscal define as regras tributárias globais do tenant. Para acessar a tela sem a URL direta, deve-se expandir a seção **LOGÍSTICA** na barra lateral esquerda e selecionar a opção **CONFIG. FISCAL**. Ela requer o preenchimento dos campos **CFOP Vendas** (código fiscal de operação padrão para saídas da fábrica) e **Alíquota ICMS (%)** (percentual padrão de cobrança do imposto). O botão **Salvar** atualiza as regras fiscais. Para configurar as regras, siga o seguinte procedimento:

1. Expandir a seção **"LOGÍSTICA"** na barra lateral esquerda e clicar em **"CONFIG. FISCAL"** (ou acessar a rota `/app/logistics/fiscal-settings`).
2. Digitar o CFOP padrão de saídas industriais no campo **CFOP Vendas** e o percentual correspondente no campo **Alíquota ICMS (%)**.
3. Clicar no botão **"Salvar"** para salvar as parametrizações fiscais.

---

### 6.7.3. Como Cadastrar Veículos Próprios e Gerenciar Registros RNTRC ( rota `/app/fleet` )

O gerenciamento de frota cadastra os veículos próprios da empresa. Para acessar a tela sem a URL direta, deve-se expandir a seção **FROTA** na barra lateral esquerda e selecionar a opção **FROTA**. A interface apresenta uma tabela listando os veículos com colunas de Placa, Tipo, Status, RNTRC, Carga Máxima (kg), Volume Máximo (m³) e Vencimento do CRLV. O botão **Novo Veículo** abre a gaveta lateral de cadastro de veículo. Esse formulário exige o preenchimento dos seguintes campos:
- **Placa:** Identificação alfanumérica do veículo (deve conter a máscara de formatação padrão `ABC-1234` ou `ABC1D23` para o padrão Mercosul).
- **Tipo:** Dropdown para selecionar a categoria do veículo (opções: Carro, Caminhão, Van ou Moto).
- **Dados Técnicos:** Valores numéricos obrigatórios para os limites de **Carga Máxima (kg)** e **Volume Máximo (m³)**.
- **Documentação Legal:** Número de registro ANTT no campo **RNTRC** (deve conter exatamente 8 dígitos numéricos), cadastro no campo **RENAVAM** (deve possuir de 9 a 11 dígitos numéricos), identificação de **Chassi** (exatamente 17 caracteres alfanuméricos em maiúsculas) e a data de vencimento da licença no campo **Vencimento CRLV**.

Para cadastrar o veículo, siga o procedimento:

1. Expandir a seção **"FROTA"** na barra lateral esquerda, clicar em **"FROTA"** (ou acessar a rota `/app/fleet`) e clicar no botão **"Novo Veículo"** no canto superior direito.
2. Inserir a placa, selecionar a categoria de tipo de veículo e preencher o código RENAVAM correspondente nos respectivos campos.
3. Inserir a identificação do chassi (17 caracteres), o código ANTT (8 dígitos) e os valores de carga útil e cubagem máxima da carroceria.
4. Selecionar a data de expiração da licença anual e clicar no botão **"Salvar"** para confirmar a gravação do veículo.
5. Caso necessite inativar ou ativar o veículo posteriormente, localizar o registro na tabela de listagem geral e clicar no ícone de inativação (botão laranja **"Inativar"**) ou ativação (botão verde **"Ativar"**).

---

### 6.7.3.1. Como Alocar Motoristas ao Veículo ( rota `/app/fleet` - Detalhes do Veículo )

O vínculo de motoristas aloca um profissional cadastrado no RH a um caminhão ou veículo de frota ativo. Ao clicar sobre um veículo na listagem geral da frota, abre-se a gaveta lateral de detalhes exibindo a aba **Motorista**. A tela exibe o motorista ativo associado à placa e o histórico de alocações cronológicas. O formulário de alocação de motoristas exige o preenchimento do **Motorista** (dropdown listando profissionais cadastrados com o cargo de Motorista), a **Data de Início da alocação** (data de retirada da chave), o campo opcional **Fim da alocação** (data prevista para devolução) e as **Observações**. Para alocar o profissional, siga o seguinte procedimento:

1. Na listagem de veículos da frota, clicar sobre o registro desejado para abrir a gaveta lateral de detalhes.
2. Acessar a aba **"Motorista"** e clicar no botão **"Nova Alocação"**.
3. Selecionar o condutor habilitado no seletor **Motorista** e informar a data de retirada no campo **Início da alocação**.
4. Se houver data de devolução definida, preencher o campo **Fim da alocação (opcional)** e adicionar notas de controle.
5. Clicar no botão **"Alocar"** para confirmar o vínculo do condutor com o veículo.

---

### 6.7.3.2. Como Agendar e Concluir Manutenções de Veículos ( rota `/app/fleet` - Aba Manutenção )

A programação de manutenções planeja revisões e reparos técnicos na frota. Na tela principal de gerenciamento de frota, deve-se navegar até a aba geral **Manutenção**. A interface disponibiliza uma barra de filtros por veículo, status (Agendada, Concluída, Cancelada) e tipo (Preventiva, Corretiva) e exibe a tabela de registros cadastrados. O formulário de agendamento exige os campos **Veículo** (dropdown de seleção, obrigatório apenas em modo frota geral), **Tipo** (Preventiva ou Corretiva), **Data** (data agendada para parada), **Descrição** e o campo opcional **Observações**. Para realizar o agendamento e o controle da manutenção, siga o seguinte procedimento:

#### Para agendar manutenção preventiva ou corretiva:
1. Na tela de frota, acessar a aba **"Manutenção"** e clicar no botão **"Agendar"** (ou **"Agendar Manutenção"** caso esteja com a gaveta de detalhes do veículo aberta).
2. Selecionar a placa correspondente no seletor **Veículo** e definir a categoria do reparo no seletor **Tipo**.
3. Escolher o dia da parada no campo **Data**, descrever o escopo do serviço no campo **Descrição** e preencher as anotações no campo **Observações**.
4. Clicar no botão **"Agendar"** (ou **"Confirmar"**) para salvar a programação.

#### Para concluir ou cancelar uma manutenção:
1. Localizar o registro em status agendado na tabela de manutenções.
2. Para registrar a execução física do serviço, clicar no botão de conclusão (ícone de marca de seleção verde **"Concluir"**), confirmando a gravação com a data de conclusão atual.
3. Para anular o agendamento por motivos operacionais, clicar no botão de cancelamento (ícone de exclusão vermelho **"Cancelar"**).

---

### 6.7.3.3. Como Registrar Abastecimentos de Combustível ( rota `/app/fleet` - Aba Abastecimento )

O registro de abastecimentos controla o consumo de combustíveis, as despesas diretas de trânsito e o consumo médio por veículo. Na tela principal de gerenciamento de frota, deve-se navegar até a aba geral **Abastecimento**. A interface exibe indicadores do total de litros fornecidos, o custo em reais acumulado para o período e a listagem geral de transações. O formulário de registro requer os dados de **Veículo**, **Data**, **Litros** (volume decimal fornecido), **Preço por Litro** (valor unitário cobrado na bomba), **Odômetro** (número do hodômetro no momento da parada) e **Fornecedor** (nome ou rede do posto). Para registrá-lo, siga o seguinte procedimento:

1. Na tela de frota, acessar a aba **"Abastecimento"** e clicar no botão **"Registrar"** (ou **"Registrar Abastecimento"** caso esteja com a gaveta de detalhes do veículo aberta).
2. Selecionar a placa no seletor **Veículo** e definir o dia do abastecimento no campo **Data**.
3. Informar o volume no campo **Litros**, o valor cobrado na bomba no campo **Preço por Litro**, a quilometragem no campo **Odômetro** e o nome comercial da parada no campo **Fornecedor**.
4. Clicar no botão **"Registrar"** para confirmar a gravação. O sistema computará o valor total pago automaticamente.

---

### 6.7.3.4. Como Emitir Relatórios e Consultar KPIs da Frota ( rota `/app/fleet` - Aba Relatórios )

A consolidação de dados de frota exibe relatórios integrados para monitorar despesas, planejamentos e motoristas da empresa. Na tela principal de frota, deve-se navegar até a aba geral **Relatórios**. O painel de visualização é dividido em três categorias principais de relatórios selecionados na barra de tabs interna:
- **Consumo:** Exibe os KPIs de "Total Litros", "Custo Total" e total de "Abastecimentos" do período. A tabela detalha o número de registros, total de litros, despesa em BRL e o preço médio pago por litro para cada placa de veículo.
- **Manutenção:** Exibe os KPIs de manutenções "Agendadas", "Concluídas" e "Canceladas". A tabela detalha a contagem total de serviços programados, concluídos, cancelados e a divisão do reparo (Preventiva ou Corretiva) por veículo.
- **Motoristas:** Rastreia o histórico completo de alocação de condutores por veículo, indicando a placa do veículo ativa associada ao condutor e o período de trânsito.

Para consultar as métricas, siga o seguinte procedimento:

1. Na tela de frota, acessar a aba geral **"Relatórios"**.
2. Clicar na aba correspondente ao relatório desejado (**Consumo**, **Manutenção** ou **Motoristas**).
3. Analisar os cards estatísticos de cabeçalho e as tabelas com os dados consolidados.

---

### 6.7.4. Como Efetuar o Cadastro de Funcionários e Vincular CNH de Motoristas ( rota `/app/hr/people` )

O cadastro de recursos humanos gerencia colaboradores e motoristas. Para acessar a tela, expanda o menu **Recursos Humanos** na barra lateral esquerda e selecione **Funcionários**. Contém os campos **Nome Completo** (nome civil do profissional), **CPF** (registro de pessoa física limpo), **Cargo** (dropdown de funções entre Operador, Supervisor e Motorista), **CNH** (número da habilitação para cargos de motorista) e **Categoria CNH** (dropdown para categorias C, D ou E). O botão **Novo Funcionário** abre a gaveta e **Salvar** confirma os dados. Para cadastrar o profissional, siga o seguinte procedimento:

1. Expandir a seção **"EQUIPE"** na barra lateral esquerda, clicar em **"FUNCIONÁRIOS"** (ou acessar a rota `/app/hr/people`) e depois no botão **"Novo Funcionário"** no canto superior direito.
2. Preencher o nome civil no campo **Nome Completo**, o CPF numérico limpo (sem pontos ou traço) no campo **CPF** e definir o cargo correspondente do colaborador no dropdown **Cargo**.
3. Se o cargo selecionado for motorista, informar os dados obrigatórios no campo **CNH** e selecionar a classe de condução correspondente no dropdown **Categoria CNH**.
4. Clicar no botão **"Salvar"** para registrar o profissional no banco de RH.

O CPF inserido passa pela verificação do dígito verificador. CPFs inválidos bloquearão o salvamento do formulário de RH.

---

### 6.7.4.1. Como Registrar Apontamentos de Horas Trabalhadas ( rota `/app/hr/people` - Detalhes da Pessoa )

O controle de horas trabalhadas registra a presença diária e horas acumuladas para controle de ponto e custos industriais. Ao clicar sobre um profissional cadastrado na listagem geral do RH, abre-se a gaveta lateral de detalhes exibindo a aba **Horas**. O formulário exige o preenchimento da **Data** do expediente, a quantidade de **Horas trabalhadas** (campo numérico decimal que aceita valores de `0.5` a `24.0`, com intervalo de meio em meio ponto, como `8.5` para 8 horas e 30 minutos) e o campo opcional **Descrição**. Para apontar as horas, siga o seguinte procedimento:

1. Na listagem de funcionários da equipe, clicar sobre a linha do colaborador para abrir a gaveta lateral de detalhes.
2. Acessar a aba **"Horas"** e localizar a seção **Registrar Horas**.
3. Preencher a data correspondente, informar as horas cumpridas e registrar uma descrição do serviço.
4. Clicar no botão **"Registrar"** para gravar o apontamento e atualizar a listagem e o totalizador de horas do operador.

---

### 6.7.4.2. Como Gerenciar Competências e Habilidades ( rota `/app/hr/people` - Detalhes da Pessoa )

A matriz de competências do operador mapeia habilidades técnicas e certificações industriais na fábrica. Na gaveta lateral de detalhes da pessoa selecionada no RH, deve-se navegar até a aba **Competências**. O formulário exige o preenchimento do **Nome da competência** (descrição da habilidade, como Operação de Solda), o **Nível de proficiência** (escala de 1 a 5 estrelas selecionada graficamente), o campo opcional **Certificado em** (data em que obteve o diploma técnico) e as **Observações**. Para cadastrar habilidades, siga o seguinte procedimento:

1. Na gaveta lateral de detalhes do funcionário, acessar a aba **"Competências"** e localizar a seção **Adicionar Competência**.
2. Digitar o nome da habilidade técnica e definir o nível de domínio clicando sobre a estrela correspondente (1 a 5).
3. Caso possua certificado oficial de qualificação, informar o dia de emissão no campo **Certificado em (opcional)** e adicionar notas descritivas.
4. Clicar no botão **"Adicionar"** para salvar a competência no prontuário profissional.
5. Caso necessite remover uma competência cadastrada, localizar a habilidade correspondente na listagem e clicar no ícone de lixeira (**"Remover"**).

---

### 6.7.4.3. Como Cadastrar Turnos de Trabalho ( rota `/app/hr/people` - Detalhes da Pessoa )

O cadastro de turnos de trabalho agenda a escala horária de trabalho e a escala produtiva do funcionário. Na gaveta lateral de detalhes da pessoa selecionada no RH, deve-se navegar até a aba **Turnos**. O formulário exige o preenchimento da **Data** planejada de escala, o horário de **Início** (hora no formato de 24h, default `08:00`), o horário de **Fim** (default `17:00`) e as **Observações**. Para agendar a escala, siga o seguinte procedimento:

1. Na gaveta lateral de detalhes do funcionário, acessar a aba **"Turnos"** e localizar a seção **Adicionar Turno**.
2. Definir o dia planejado no campo **Data**.
3. Preencher os horários de entrada e saída correspondentes no campo **Início** e **Fim** e adicionar notas de turno.
4. Clicar no botão **"Adicionar"** para confirmar a gravação da escala no calendário do colaborador.
5. Caso necessite apagar a escala cadastrada, localizar o registro correspondente na listagem e clicar no ícone de lixeira (**"Deletar"**).

---

### 6.7.5. Como Cadastrar Usuários na Plataforma e Conceder Perfis ( rota `/app/iam/users` )

O controle de usuários IAM gerencia as contas ativas de acesso à plataforma. Para acessar esta tela sem a URL direta, deve-se expandir a seção **ACESSO E SEGURANÇA** na barra lateral esquerda e clicar em **USUÁRIOS**. A tela possui uma tabela com os usuários e o botão de adicionar novo usuário que abre o modal com os campos **Nome Completo** (nome civil da conta), **E-mail** (e-mail corporativo válido, normalizado para minúsculas) e **Papéis (Roles)** (checkboxes de vinculação de papéis RBAC). O botão **Salvar** cria a conta. Para incluir o colaborador, siga o seguinte procedimento:

1. Expandir a seção **"ACESSO E SEGURANÇA"** na barra lateral esquerda, clicar em **"USUÁRIOS"** (ou acessar a rota `/app/iam/users`) e clicar no botão **"Novo Usuário"** (ou ícone de adição) no topo direito da tela.
2. Preencher o nome civil no campo **Nome Completo**, o e-mail corporativo no campo **E-mail** e selecionar os papéis de privilégios RBAC adequados no formulário.
3. Clicar no botão **"Salvar"** para concluir o convite de acesso.

---

### 6.7.6. Como Mapear Níveis de Acesso e Permissões (RBAC) ( rota `/app/iam/roles` )

A parametrização do RBAC configura a matriz de permissões granulares por papel de usuário. Para acessar esta tela sem a URL direta, deve-se expandir a seção **ACESSO E SEGURANÇA** na barra lateral esquerda e selecionar a opção **NÍVEIS DE ACESSO**. A listagem exibe as ações por papel funcional, e o botão **Salvar Permissões** grava a nova configuração. Para parametrizar as permissões, siga o seguinte procedimento:

1. Expandir a seção **"ACESSO E SEGURANÇA"** na barra lateral esquerda e clicar em **"NÍVEIS DE ACESSO"** (ou acessar a rota `/app/iam/roles`).
2. Escolher o papel funcional que deseja configurar na listagem de papéis na coluna esquerda.
3. Marcar ou desmarcar os privilégios de leitura, escrita e execução em cada módulo da tabela.
4. Clicar no botão **"Salvar Permissões"** para persistir as definições globais no banco.

---

### 6.7.7. Como Rastrear e Auditar Ações pelo Log de Trilha ( rota `/app/iam/audit` )

O painel de trilha de auditoria monitora e rastreia mutações na base de dados. Para acessar esta tela sem a URL direta, deve-se expandir a seção **ACESSO E SEGURANÇA** na barra lateral esquerda e selecionar a opção **AUDITORIA**. Apresenta o campo de filtro **CorrelationId** (rastreador hexadecimal de transação único do backend) e **Tipo de Ação** (dropdown de busca contendo tipos como Login, Mutation e RoleAssignment). Para auditar, siga o seguinte procedimento:

1. Expandir a seção **"ACESSO E SEGURANÇA"** na barra lateral esquerda e clicar em **"AUDITORIA"** (ou acessar a rota `/app/iam/audit`).
2. Digitar o correlationId da chamada do backend na barra de filtros.
3. Selecionar o tipo de ação desejada para refinar a busca no painel.
4. Analisar a listagem de registros contendo data, e-mail do operador e a ação mutada no sistema.

---

### 6.7.8. Como Parametrizar Chaves de API e Integrações ( rota `/app/settings/integrations` )

A tela de integrações externas gerencia os tokens de integração ERP. Para acessar a tela sem a URL direta, deve-se expandir a seção **CONFIGURAÇÕES** na barra lateral esquerda e selecionar a opção **INTEGRAÇÕES**. A tela apresenta a lista de integrações e o campo **Descrição da Chave** (texto descritivo da integração) e o botão **Gerar Nova Chave** que emite a hash secreta de autenticação. Para gerar a chave, siga o seguinte procedimento:

1. Expandir a seção **"CONFIGURAÇÕES"** na barra lateral esquerda, clicar em **"INTEGRAÇÕES"** (ou acessar a rota `/app/settings/integrations`) e selecionar o serviço correspondente na lista.
2. Digitar uma descrição clara para o sistema externo que se conectará à API no campo **Descrição da Chave**.
3. Clicar no botão **"Gerar Nova Chave"** para expor o token de autenticação integrado.
4. Copiar o token gerado (lembrando que ele é exibido apenas uma única vez na criação).

---

## 📋 FAQ — Resolução de Problemas Gerais

### 1. Ocorreu erro de "Saldo Insuficiente" ao liberar uma Ordem de Produção (OP).
- **Causa:** A quantidade demandada de matérias-primas pela receita (BOM) para o lote planejado excede o saldo disponível (`Available`) nas prateleiras do almoxarifado.
- **Resolução:**
  1. Vá ao painel de **Estoque** e filtre pelo material pendente.
  2. Verifique se há lotes retidos em quarentena (`Blocked`) e mude seu status se a qualidade autorizar.
  3. Caso contrário, aguarde a chegada de novas compras do fornecedor.

### 2. A conferência cega foi salva como divergente. Onde está o estoque?
- **Causa:** A contagem física digitada pelo almoxarife divergiu das quantidades oficiais declaradas no XML faturado pelo fornecedor.
- **Resolução:** O sistema salva o saldo correspondente na área de quarentena com status de lote `Blocked` (Bloqueado) para evitar que a linha de produção consuma material divergente por acidente. O supervisor de almoxarifado deve avaliar a nota fiscal na fila e liberar os saldos após inspeção manual.
