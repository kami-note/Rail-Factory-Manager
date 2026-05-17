# Guia de Estilo e Design: Rail-Factory-Fork

Este documento descreve os padrões visuais, de UX e de interface do projeto Rail-Factory-Fork. Ele serve como a única fonte de verdade para a implementação de novas funcionalidades e componentes no frontend.

## 1. Filosofia de Design
O Rail-Factory-Fork adota uma estética **industrial e utilitária**, inspirada em sistemas de controle modernos e no "Microsoft Fluent Design". O foco principal é a legibilidade de dados densos, eficiência operacional e uma hierarquia visual clara através de tipografia pesada e contraste moderado.

---

## 2. Paleta de Cores

### 2.1 Cores de Marca e Ação
| Cor | Hex | Uso |
| :--- | :--- | :--- |
| **Primary** | `#0078d4` | Botões principais, AppBar, Links, Estados ativos. |
| **Success** | `#107c10` | Confirmações, estados "Ok", Balanços positivos. |
| **Error** | `#d13438` | Alertas críticos, erros, balanços negativos, cancelamentos. |
| **Warning** | `#ffb900` | Pendências, avisos, processos em andamento. |
| **Info** | `#0078d4` | Informações neutras, links, dicas. |

### 2.2 Cores Neutras e de Sistema
| Cor | Hex | Uso |
| :--- | :--- | :--- |
| **Background Default** | `#f3f2f1` | Fundo principal da aplicação (cinza muito claro). |
| **Background Paper** | `#ffffff` | Fundo de Cards, Modais e áreas de conteúdo. |
| **Text Primary** | `#201f1e` | Texto principal, títulos. |
| **Text Secondary** | `#605e5c` | Subtítulos, labels, textos de apoio. |
| **Divider** | `#edebe9` | Linhas de separação, bordas de tabelas. |

---

## 3. Tipografia

- **Font Family:** `"Segoe UI", "Inter", -apple-system, sans-serif`.
- **Pesos (Weights):**
    - `400`: Texto padrão.
    - `600`: Texto semibold (botões, itens de lista).
    - `700`: Títulos e headers.
    - `800`/`900`: Ênfase extrema em KPIs e Branding.

### Escala Tipográfica
- **h1:** `1.5rem` (24px), Bold, Letter-spacing `-0.02em`.
- **h2:** `1.25rem` (20px), Bold.
- **body1:** `0.875rem` (14px) - Padrão do sistema.
- **caption:** `0.7rem` (11.2px), Extra-bold (`700`/`800`), Uppercase, Letter-spacing `0.05em`. *Usado extensivamente para labels e headers de módulos.*

---

## 4. Iconografia
- **Biblioteca Principal:** `lucide-react`.
- **Tamanho Padrão:** `18px` para ícones de navegação, `16px` para ícones em botões/tabelas.
- **Estilo:** Stroke width de `2` ou `1.5`. Cores devem seguir a `palette` (ex: `color="primary.main"` ou `opacity: 0.6` para ícones decorativos).

---

## 5. Layout e Estrutura

### 5.1 Navegação
- **AppBar (Top):** Altura fixa de `48px`. Fundo `primary.main`. Contém o Branding e informações do usuário.
- **Sidebar (Drawer):** Largura fixa de `220px`. Fundo `#ffffff`.
    - Itens de lista com `variant="caption"` e peso `600`/`800`.
    - Ícones alinhados à esquerda.
- **Conteúdo:** Fundo `#f3f2f1`, preenchimento generoso em volta das áreas de trabalho.

### 5.2 Grid e Espaçamento
- **Base:** `4px` (multiplos de 4 para `p`, `m`, `gap`).
- **Bordas:** `borderRadius: 4px` para componentes gerais. Cards podem usar `8px` para um visual mais moderno.

---

## 6. Componentes Core (Padrões de Interface)

### 6.1 StatusChip
Utilizado para representar o estado de qualquer entidade no sistema.
- **Estilo:** `variant="outlined"`, `size="small"`.
- **Tipografia:** `0.65rem`, Extra-bold (`800`).
- **Regra:** Deve sempre incluir um ícone à esquerda correspondente ao estado.

### 6.2 StatCard (KPIs)
Cards de métricas que aparecem no topo de dashboards ou workspaces.
- Exibe o valor em `h2` e o label em `caption` (uppercase).
- Possui um divisor vertical (`borderRight`) se houver múltiplos cards em linha.

### 6.3 ModuleHeader
O cabeçalho padrão para qualquer módulo ou seção da página.
- Combina um ícone, um label uppercase (caption) e uma ação opcional (ex: botão de adicionar).

### 6.4 MaterialAvatar
Identificador visual para materiais e produtos.
- **Determinismo:** A cor de fundo é gerada via hash do código do material. Isso garante consistência visual (o mesmo produto tem sempre a mesma cor em todo o sistema).

---

## 7. UX e Localização

1.  **Idioma:**
    - **Código/Documentação Técnica:** Inglês.
    - **Interface (Labels, Mensagens, Erros):** Português (Brasil).
2.  **Acessibilidade:**
    - Uso obrigatório de `aria-label` em componentes puramente visuais.
    - Contraste elevado para estados de erro e sucesso.
3.  **Feedback:**
    - Botões em estado de carregamento devem usar spinners de `20px` ou `16px`.
    - Modais devem ser centralizados, responsivos e possuir uma borda superior de destaque (`borderTop: 5px solid primary.main`) para reforçar a identidade visual.
    - Utilizar o componente `Dialog` do MUI como base para modais complexos.

---

## 8. Padrões de Implementação (Frontend)

- **Framework:** React 19 + Material UI (MUI) 9.
- **Estilização:** Preferencialmente via `sx` prop do MUI para layouts rápidos ou Emotion `styled` para componentes reutilizáveis complexos.
---

## 9. Catálogo de Componentes Compartilhados (`src/shared/components`)

Estes componentes são os blocos de construção fundamentais da interface e devem ser reutilizados em todas as features.

### 9.1 StatusChip
**Propósito:** Exibir o estado atual de uma entidade (Pedido, NF, Material).
- **Design:** Chip com borda, ícone contextual e tipografia extra-bold em tamanho reduzido.
- **Invariantes:** Mapeia chaves de status do backend para cores e ícones pré-definidos para garantir consistência visual em todo o sistema.

### 9.2 StatCard
**Propósito:** Exibir indicadores de performance (KPIs) de forma rápida.
- **Design:** Stack horizontal com ícone à esquerda, seguido de label em `caption` e valor em `h2`.
- **Uso:** Ideal para o topo de Dashboards ou Workspaces para mostrar totais, contagens ou saldos.

### 9.3 ModuleHeader
**Propósito:** Padronizar o título de seções e módulos dentro de uma página.
- **Design:** Título em `caption` (uppercase/bold) acompanhado de um ícone e um espaço para ações (botões) no lado direito.
- **Invariantes:** Garante que todas as seções tenham o mesmo espaçamento e hierarquia visual.

### 9.4 MaterialAvatar
**Propósito:** Identificador visual determinístico para produtos/materiais.
- **Design:** Avatar quadrado (rounded) com ícone de pacote ou imagem real.
- **Invariantes:** A cor de fundo é derivada do `materialCode`, garantindo que o mesmo material sempre tenha a mesma cor, independente de onde apareça na aplicação.

### 9.5 ResponsiveCenteredModal
**Propósito:** Container padrão para diálogos e formulários complexos.
- **Design:** Modal centralizado com largura dinâmica, borda superior na cor primária e botão de fechar proeminente.
- **Recursos:** Suporta conteúdo com scroll interno e ajuste de largura máxima baseado em breakpoints.

### 9.6 TenantSelector
**Propósito:** Seleção de organização no login e troca de contexto.
- **Design:** Dropdown (Select) integrado com a API de catálogo de tenants.
- **Invariantes:** Carrega automaticamente a lista de organizações disponíveis e gerencia o estado de seleção global.

### 9.7 InlineError & PageError
**Propósito:** Exibição de mensagens de erro de forma controlada.
- **InlineError:** Utiliza o componente `Alert` do MUI para erros em formulários ou seções.
- **PageError:** Wrapper que centraliza um `InlineError` em uma página vazia, ideal para erros de carregamento total.

### 9.8 ProtectedDashboardLayout
**Propósito:** Estrutura mestre da aplicação autenticada.
- **Design:** Sidebar lateral persistente + Top Bar (AppBar).
- **Recursos:** Gerencia a responsividade (mobile vs desktop), navegação via permissões (RBAC) e logout.
