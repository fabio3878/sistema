# Sistema de Automação Comercial — Diretriz de Design (Front-end)

Documento-regra de design, UX e front-end architecture para o ERP desktop. É a **fonte da
verdade visual**: toda tela nova nasce a partir daqui, como todo código nasce a partir do
`ARQUITETURA_1.md`. Escrito para o front **Tauri 2 + React/TS** (decisão de arquitetura, §1
linha 8 do `ARQUITETURA_1.md`).

> Princípio guia: **um produto, muitas verticais — uma só casca.** A estrutura visual é
> **idêntica** para loja, restaurante, clínica, autopeças ou material de construção. Só mudam
> os módulos disponíveis. O usuário sente **um único produto altamente configurável**, nunca
> vários sistemas diferentes. Consistência acima de novidade.

---

## 1. Conceito & filosofia

Não é "mais um ERP". É uma plataforma modular onde cada empresa **monta o próprio ERP**,
ativando só os módulos do seu segmento (ver `ARQUITETURA_1.md` §8 — Frentes + Licenciamento).
A experiência deve lembrar **Notion, Obsidian e Arc**: sensação de flexibilidade e
personalização, mas com consistência absoluta.

O design transmite: **modernidade, elegância, rapidez, organização, pouco ruído visual, alta
produtividade** — acessível a iniciantes e eficiente para avançados. Muito espaço em branco.

**Inspirações:** Linear · Stripe Dashboard · Raycast · Arc Browser · Notion · Figma ·
GitHub Desktop.

**Anti-padrões PROIBIDOS** (o visual de "ERP clássico"):
- Excesso de bordas, molduras e divisórias.
- Gradientes chamativos e sombras pesadas.
- Ícones grandes, botões grandes, telas densas e carregadas.
- Ruído visual competindo pela atenção. Na dúvida, **remova**.

---

## 2. Design tokens (fonte da verdade — CSS variables)

Tudo é token. Nenhum valor de cor/espaçamento/raio "solto" no componente — sempre a variável.
Os tokens vivem em CSS variables e são expostos no `tailwind.config` (ver §7).

### 2.1 Cores semânticas (~12) — tema claro / escuro

| Token | Light | Dark | Uso |
|---|---|---|---|
| `--bg` | `#F7F8FA` | `#0E0F11` | fundo do app |
| `--surface` | `#FFFFFF` | `#17181B` | cards, painéis, tabelas |
| `--elevated` | `#FFFFFF` | `#1F2023` | popover, drawer, menus, dropdowns |
| `--border` | `#E5E7EB` | `#26282C` | divisores e contornos sutis |
| `--fg` | `#1A1D21` | `#E6E7EA` | texto primário |
| `--fg-muted` | `#6B7280` | `#9AA0A6` | texto secundário, placeholder, ícones |
| `--primary` | `#5B5BD6` | `#7C7CF0` | **marca** / ação primária / estado ativo |
| `--primary-fg` | `#FFFFFF` | `#FFFFFF` | texto/ícone sobre `--primary` |
| `--success` | `#16A34A` | `#22C55E` | confirmação, saldo positivo |
| `--warning` | `#D97706` | `#F59E0B` | atenção, pendência |
| `--danger` | `#DC2626` | `#F87171` | erro, ação destrutiva, saldo negativo |
| `--info` | `#2563EB` | `#60A5FA` | informação (azul — **separado da marca**) |
| `--ring` | `rgba(91,91,214,.45)` | `rgba(124,124,240,.55)` | anel de foco (acessibilidade) |

**Regras de cor:**
- **Neutros dominam.** O accent (`--primary`) é usado **com parcimônia**: ação primária da tela,
  item ativo da sidebar, seleção, foco. Uma tela cheia de roxo está errada.
- **Marca ≠ info.** O índigo é a identidade; o azul (`--info`) é só o estado semântico de
  informação. Nunca use índigo para "mensagem informativa" nem azul para "botão principal".
- Cada cor de estado tem uma **variante tint de fundo** (ex.: `--success-bg`, `--warning-bg`,
  `--danger-bg`, `--info-bg`) — fundos suaves para badges, alertas e linhas destacadas, sem peso.
- O accent é **trocável numa variável** — a marca pode virar outra cor sem tocar componente.

### 2.2 Grid & espaçamento
Base **8px**, com meio-passo de **4px** para ajustes finos. Escala única:
`4 · 8 · 12 · 16 · 20 · 24 · 32 · 40 · 48 · 64`. Padding, gap e margens saem só daqui.

### 2.3 Raio de borda
`sm 6px · md 8px · lg 12px · xl 16px · full 9999px`. Inputs e botões usam `md`; cards e
drawers usam `lg`; avatares/pills usam `full`.

### 2.4 Tipografia
- **Fonte UI:** **Inter** (fallback `system-ui`). Excelente legibilidade em telas densas.
- **Fonte mono:** JetBrains Mono / Geist Mono — para IDs (ULID), códigos e SKUs.
- **Números tabulares** (`font-feature-settings: 'tnum'`) obrigatórios em tabelas, valores
  monetários e KPIs — colunas de números sempre alinhadas.

| Nível | Size/Line | Peso | Uso |
|---|---|---|---|
| Display | 28 / 34 | 600 | título de dashboard, telas de destaque |
| H1 | 22 / 28 | 600 | título de página |
| H2 | 18 / 24 | 600 | seção |
| H3 | 16 / 22 | 600 | subseção, título de card |
| **Body** | **14 / 20** | 400 | **texto padrão** (UI desktop densa) |
| Small | 13 / 18 | 400/500 | labels, ajuda, meta |
| Caption | 12 / 16 | 500 | badges, timestamps, hints |

Pesos permitidos: **400 / 500 / 600**. Nada de 700+ em UI.

### 2.5 Elevação & motion
- **Elevação:** apenas **duas sombras sutis** (uma para dropdown/popover, uma maior para
  drawer/modal). Todo o resto separa-se por `--border` e `--surface`, não por sombra.
- **Motion:** `fast 120ms` (hover/press) · `base 180ms` (drawer/tab/fade) · `slow 240ms`
  (transições maiores). Easing padrão `cubic-bezier(0.2, 0, 0, 1)`. Animações **discretas**.
  **Sempre** respeitar `prefers-reduced-motion`.

### 2.6 Densidade
Desktop-first e **compacta**. Altura de controles: `sm 32px · md 36px · lg 40px`. Linhas de
DataTable **36–40px**. Alvo de toque não é prioridade (é desktop), velocidade é.

---

## 3. Layout & navegação (a casca única)

```
┌───────────────────────────────────────────────────────────┐
│  Topbar (~52px):  breadcrumb · ⌘K busca · notificações · usuário │
├──────────┬────────────────────────────────────────────────┤
│ Sidebar  │                                                │
│ ~240px   │            Conteúdo                            │
│ (⇄ 56px) │   (cards · DataTable · formulário)             │
│          │                                    ┌───────────┐│
│ módulos  │                                    │  Drawer   ││
│ ativos   │                                    │  edição   ││
│          │                                    └───────────┘│
└──────────┴────────────────────────────────────────────────┘
```

- **Sidebar recolhível** (~240px expandida ⇄ 56px só-ícones). Lista os módulos/recursos
  **ativos** para aquela empresa e usuário (ver §5). Estado recolhido persiste por usuário.
- **Topbar minimalista** (~52px): breadcrumb à esquerda, trigger de busca global ao centro,
  usuário/tema/notificações à direita. Sem clutter.
- **Busca global — `Ctrl/Cmd + K`** (Command Palette, `cmdk`): navegar entre telas, executar
  ações, buscar registros. É o caminho rápido primário do usuário avançado.
- **Drawer lateral** (da direita) é o padrão para **criar/editar** registros — mantém o contexto
  da lista atrás. **Modal só quando imprescindível** (confirmação destrutiva, bloqueio real).
- **Prioridade absoluta: teclado e menos cliques.** Toda ação frequente tem atalho; formulários
  navegáveis por Tab; Enter salva; Esc fecha.

---

## 4. Inventário de componentes

Todos seguem **a mesma linguagem visual** (tokens da §2). Base técnica recomendada:

| Componente | Base |
|---|---|
| Botões: Primary / Secondary / Ghost / Danger | custom sobre `<button>` + tokens |
| Input, Textarea | custom |
| Select, Combobox | Radix Select · Combobox sobre `cmdk` |
| DatePicker, Calendário | `react-day-picker` |
| Checkbox, Radio, Switch | Radix |
| Card, Badge, Tag, Avatar | custom (Avatar via Radix) |
| Drawer lateral | Radix Dialog (variante lateral) ou `vaul` |
| Modal, Confirmação/Alerta destrutivo | Radix Dialog / AlertDialog |
| Toast | Radix Toast ou `sonner` |
| Tooltip, Popover | Radix |
| Breadcrumb, Tabs, Accordion | custom · Radix (Tabs/Accordion) |
| Sidebar, Navbar | custom (composição do layout) |
| Command Palette (⌘K) | `cmdk` |
| **DataTable + Paginação** | **TanStack Table** (ordenar/filtrar/selecionar; linha 36–40px) |
| Empty State, Skeleton, Spinner | custom |
| Alertas (inline) | custom (usa cores de estado + tint) |
| Gráficos, KPIs | Recharts |
| Timeline | custom |
| Upload de arquivos | `react-dropzone` |

**Camadas de apoio (não visuais, mas parte da regra):**
- **Formulários:** React Hook Form + **Zod**. Os erros de validação vindos do backend
  (`Result` — ver `CLAUDE.md`) são mapeados **campo a campo** no formulário.
- **Dados/servidor:** **TanStack Query** (cache, revalidação, estados de loading/erro).
- **Roteamento:** TanStack Router (ou React Router).
- **Drag-and-drop:** `dnd-kit` (dashboard e reordenações).
- **Ícones:** `lucide-react` (traço fino, coerente com a estética). Ícones **pequenos e
  discretos** — nunca decorativos-grandes.

**shadcn/ui** é o método: os componentes Radix+Tailwind são **copiados para o repositório**
(código nosso, não dependência travada), permitindo o acabamento premium e a customização total.

---

## 5. Modularidade & permissões (o elo com o backend)

A UI **espelha exatamente** o modelo do backend. A casca é **dirigida por módulo**:

- **Sidebar, rotas e widgets** só aparecem se o módulo estiver **licenciado**
  (`licenca.ModuloAtivo(...)`) **e** o usuário tiver a **funcionalidade** correspondente.
- Permissões vêm das **claims `func:<codigo>`** do JWT (convenção `<modulo>.<recurso>.<acao>`,
  ex.: `cad.cliente.criar`) — mesma fonte da autorização de endpoint no host (ver `CLAUDE.md`
  e `ARQUITETURA_1.md` §6.8). O front **não inventa** permissão: consome o mesmo catálogo.
- Um botão/ação para o qual o usuário não tem `func` fica **oculto ou desabilitado**, nunca
  quebra. A tela do mesmo módulo é **idêntica** entre segmentos; o que muda é o conjunto ativo.

**Dashboard modular:** widgets (Receita, Vendas, Fluxo de caixa, Produtos em falta, Agenda,
Entregas, Pedidos recentes, Gráficos, Tarefas…) são **adicionáveis/removíveis** e
**reordenáveis por drag-and-drop** (`dnd-kit`). O layout é **persistido por empresa/usuário**.
Cada widget também respeita licença + `func:<codigo>`.

---

## 6. UX & performance

- **Poucos cliques, feedback imediato.** Toda ação confirma visualmente (toast, estado, foco).
- **Carregamento progressivo:** Skeleton no lugar de spinner sempre que possível; a estrutura
  aparece antes do dado.
- **Empty states cuidados:** primeira vez / sem resultado tem ilustração leve + ação primária,
  nunca uma tela morta.
- **Atalhos de teclado documentados** e visíveis (tooltip com a tecla, página de atalhos).
- **Acessibilidade:** foco **sempre visível** (`--ring`), navegação por teclado garantida pelos
  primitivos Radix, contraste **AA** nos dois temas, `aria-*` correto.
- **Percepção de velocidade** é requisito de produto: transições curtas, nada de spinners longos
  bloqueando a tela, otimismo de UI onde fizer sentido (TanStack Query).

---

## 7. Notas de implementação (quando o front for scaffolded)

> O app Tauri **ainda não existe**. Esta seção fixa o rumo para a etapa de scaffolding (fora do
> escopo de agora), não é código a escrever hoje.

- **Tema claro+escuro** via CSS variables num `:root` / `[data-theme="dark"]`; Tailwind lê os
  tokens (`colors: { bg: 'var(--bg)', ... }`). Alternância persistida por usuário.
- `tailwind.config` reflete os tokens da §2 (cores, spacing 8px, radius, fontSize da escala,
  fontFamily Inter/mono). Nada de valor mágico fora do config.
- Estrutura sugerida: um **pacote de UI compartilhado** (design system) consumido pelas frentes
  (`src/Frentes/Frente.<Vertical>`), garantindo a casca única.
- Componentes shadcn/ui copiados e **ajustados aos tokens** — não usar o default cru.

---

## Resumo inegociável (o que nunca muda)
1. **Uma casca visual para todos os segmentos**; módulos por licença + `func:<codigo>`.
2. **Tudo é token** (CSS variables); neutros dominam, accent índigo com parcimônia; claro+escuro.
3. **Tailwind + Radix (shadcn/ui)**; DataTable = TanStack Table; formulários = RHF+Zod; ⌘K sempre.
4. **Drawer > modal**; teclado e menos cliques; feedback imediato; muito espaço em branco.
5. **Zero visual de "ERP clássico"** — sem bordas/sombras/ícones pesados.
