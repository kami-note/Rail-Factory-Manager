# Respostas do TCC - Desenvolvimento Individual (Plataforma Rail-Factory-Fork)

Este documento contém o questionário de autoavaliação do desenvolvimento individual estruturado de forma limpa, direta e simplificada para fácil leitura, transcrição ou estudo.

---

### **1. Como você organizou suas etapas de trabalho? Adotou ciclos iterativos (construindo e testando pequenas partes por vez) ou seguiu uma linha rígida (primeiro documentou tudo, depois programou tudo)? Justifique como essa escolha ajudou ou atrapalhou o andamento do projeto.**

**Resposta:**
"Eu adotei ciclos iterativos e incrementais, dividindo o desenvolvimento em pequenas etapas funcionais (as chamadas 'passadas'). Em vez de documentar todo o sistema no início e programar tudo só no final (cascata), eu programava e testava uma parte completa de cada vez (como a entrada de notas fiscais, depois o estoque, e depois a produção). Essa escolha me ajudou muito, pois permitiu identificar bugs de integração logo no início, evitando que erros acumulados surgissem todos juntos na fase final do projeto."

---

### **2. Como foi a experiência de atuar simultaneamente em tarefas de gerente de requisitos, arquiteto/desenvolvedor e analista de testes? Quais ferramentas ou métodos você utilizou para não se perder nos prazos e organizar o seu próprio backlog de tarefas?**

**Resposta:**
"Atuar em múltiplos papéis foi um grande desafio de organização pessoal. Para não me perder com os prazos, eu utilizei um backlog rígido de tarefas dividido por fases no arquivo `PLANO_DE_TASKS.md`, onde cada funcionalidade tinha critérios claros de aceitação para ser considerada concluída. Na parte de desenvolvimento, utilizei o .NET Aspire, o que me permitiu rodar todo o ambiente de banco de dados, filas de mensagens e APIs locais com um único comando, economizando muito tempo na hora de alternar entre o papel de desenvolvedor e o de testador."

---

### **3. Como foi o seu fluxo de versionamento? Utilizou ramificações (branches) para organizar o desenvolvimento de novas funcionalidades ou concentrou tudo na linha principal?**

**Resposta:**
"Utilizei um fluxo de versionamento baseado em ramificações (branches) no Git (Feature Branch Workflow). Para cada funcionalidade nova que eu criava, abria uma branch separada (ex: uma branch específica para RabbitMQ ou outra para Logística). Eu só unia essa branch à linha principal (`main`) quando o código estava estável, sem erros de compilação no front-end e no back-end, e com todos os testes passando. Isso garantiu que a linha principal estivesse sempre funcional."

---

### **4. Qual foi o maior obstáculo técnico ou de prazo que você enfrentou sozinho? Como você contornou esse problema sem o apoio de uma equipe (ex: precisou pivotar alguma funcionalidade, simplificar o escopo, buscar fóruns/comunidades externas)?**

**Resposta:**
"O maior obstáculo técnico foi um conflito entre o Entity Framework Core 10 (ORM de persistência) e o PostgreSQL na hora de salvar coleções de sub-itens associados em massa. O sistema gerava erros de chave estrangeira difíceis de rastrear. Sem equipe de apoio, pesquisei a fundo nos fóruns e no GitHub das ferramentas e contornei o problema criando uma função específica para salvar esses itens escrevendo SQL puro direto na base de dados, pulando a automação do framework apenas nessa transação crítica."

---

### **5. Você conseguiu cumprir os prazos que estipulou inicialmente? Se houve atrasos, qual etapa (requisitos, banco de dados, front-end, back-end, bugs) consumiu mais tempo do que o esperado?**

**Resposta:**
"Sim, consegui cumprir a maior parte dos prazos gerais estimados, mas a etapa de mensageria assíncrona com RabbitMQ (comunicação entre os serviços de logística e estoque) consumiu muito mais tempo do que o esperado. Garantir que as mensagens fossem processadas na fila na ordem correta (FIFO) e que o sistema não duplicasse a baixa de estoque em caso de oscilações de rede exigiu muitas rodadas de testes manuais, ajustes nas tabelas e depuração de banco de dados."

---

### **6. Pensando no software rodando no mercado, quais custos de infraestrutura (hospedagem em nuvem, bancos de dados, consumo de APIs) você identificou como necessários para manter seu projeto ativo?**

**Resposta:**
"Para colocar o sistema no mercado de forma viável no início, planejei a infraestrutura rodando em um servidor VPS único (como DigitalOcean, Hetzner ou Contabo) utilizando Docker Compose para organizar as APIs, o banco PostgreSQL, o cache Redis e o RabbitMQ em contêineres na mesma máquina. Isso reduz muito os custos no início:
*   **Hospedagem (Servidor VPS de 4GB a 8GB de RAM):** cerca de **USD 10,00 a USD 15,00 por mês** (em torno de R$ 50,00 a R$ 80,00).
*   **Banco de Dados, Cache e Mensageria:** rodando como contêineres Docker locais dentro da própria VPS, sem custo de licença ou serviços extras gerenciados.
*   **Frontend (Interface Web):** hospedado gratuitamente na Vercel ou Netlify (plano free para projetos iniciais).
*   **Custo Mensal Estimado Total:** aproximadamente **R$ 60,00 a R$ 80,00 por mês** para manter a plataforma totalmente ativa para os primeiros clientes."


---

### **7. Sendo você o próprio desenvolvedor, como garantiu a neutralidade na hora de testar o sistema? Realizou testes manuais seguindo cenários de uso ou implementou algum teste automatizado?**

**Resposta:**
"Para evitar o vício de testar apenas os caminhos que eu sabia que estavam funcionando, adotei duas estratégias:
1.  Implementei testes automatizados de ponta a ponta com a ferramenta Playwright, simulando o comportamento de um usuário real clicando e digitando na interface.
2.  Criei um middleware de desenvolvimento (bypass) no código do BFF que me permitia injetar sessões simuladas com diferentes permissões (ex: testar se um usuário comum conseguia ou não forçar o acesso a uma tela de admin), garantindo testes de segurança neutros."

---

### **8. Quais documentos ou diagramas criados por você foram cruciais para que você não se perdesse na própria lógica do sistema ao longo dos meses (ex: modelo de banco de dados, diagramas de rotas, documentação de endpoints)?**

**Resposta:**
"Três documentos criados ao longo do projeto foram cruciais:
*   A documentação de endpoints (`docs/CONTRATOS_API.md`), que me serviu de guia para saber exatamente quais dados enviar do front-end para as APIs.
*   O diagrama de arquitetura C4 (`docs/ARQUITETURA_GERAL.md`), fundamental para eu não me perder no fluxo de login e na geração de tokens internos de autenticação.
*   O mapeamento de processos (`docs/FLUXOS_DE_TRABALHO.md`), que ditou a lógica exata de produção no chão de fábrica."

---

### **9. Se você fosse reiniciar o desenvolvimento desse software hoje, sabendo o que sabe agora, o que mudaria na sua postura gerencial, no escopo ou na escolha das tecnologias?**

**Resposta:**
"Se eu recomeçasse hoje:
1.  **Escopo (Mono-função):** Eu reduziria drasticamente o escopo geral do projeto. Em vez de tentar construir um ERP completo com vários módulos satélites (como Recursos Humanos, Frota de Veículos e Logística de Despacho), eu focaria em uma única função central muito forte (uma 'mono-função'), como a coordenação de ordens de produção no chão de fábrica e a baixa de saldo em estoque. Isso teria concentrado meu esforço no que realmente importa e evitado desperdício de tempo com cadastros repetitivos.
2.  **Postura Gerencial:** Criaria testes automatizados de API logo nas primeiras semanas para evitar testes manuais demorados no console.
3.  **Tecnologias:** Usaria um micro-ORM (como o Dapper) para fazer listagens rápidas de tela, reduzindo a complexidade do Entity Framework Core que é muito pesado para leituras simples."


---

### **10. De que forma a experiência de planejar e executar um projeto de software de ponta a ponta, de forma totalmente individual, contribuiu para o seu amadurecimento como futuro profissional da área de tecnologia?**

**Resposta:**
"Desenvolver um projeto desse tamanho sozinho foi um grande choque de realidade. Eu percebi que o mundo do desenvolvimento de software é cruel e implacável: não existe ninguém para te salvar quando o banco de dados falha, os frameworks não funcionam de forma perfeita como nos tutoriais e qualquer atalho ou gambiarra que você faz no início volta para cobrar o preço logo em seguida. Essa experiência me amadureceu muito como profissional porque me forçou a ser autossuficiente e a entender que manter o código limpo, documentado e testado não é preciosismo de professor, mas sim a única forma de sobreviver e entregar um software real funcionando."

