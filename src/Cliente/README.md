# Cliente (casca desktop)

Front-end do ERP — **Tauri 2 + React/TS**. A **casca visual é única** para todos os segmentos;
o que muda são os módulos ativos (gated por licença + permissão `func:<codigo>`). Padrão visual:
[`DESIGN_1.md`](../../DESIGN_1.md) na raiz do repo.

## Stack
- Vite + React 18 + TypeScript
- Tailwind CSS 3 (tokens do `DESIGN_1.md` em `src/styles/globals.css` + `tailwind.config.ts`)
- Radix UI (primitivos) no padrão shadcn/ui — componentes em `src/components/ui`
- react-router-dom · cmdk (⌘K) · lucide-react · class-variance-authority

## Rodar em desenvolvimento (web, sem Rust)
```bash
cd src/Cliente
npm install
npm run dev            # http://localhost:5173
```
Isso já sobe a casca completa (login, sidebar recolhível, topbar, ⌘K, tema claro/escuro, dashboard).

### Autenticação (login real contra o host .NET)
A tela de login consome `/acesso/*` do host `AgenteLocal`. Em dev, o Vite faz **proxy** de
`/acesso` para o host (padrão `http://localhost:5080`, configurável por `AGENTE_LOCAL_URL`) —
sem CORS. A sessão (usuário, `ConcedeTodas`, `func:<codigo>`) sai das claims do JWT + `/acesso/eu`;
tokens ficam em `localStorage` e são renovados via `/acesso/refresh`. Em produção/Tauri, defina
`VITE_API_URL`.

Suba o host numa porta fixa (segredos só por env, nunca versionados):
```bash
# na raiz do repo
ASPNETCORE_URLS=http://localhost:5080 \
Banco__ConnectionString="Host=localhost;Port=5432;Database=automacao;Username=postgres;Password=<senha>" \
Acesso__Jwt__ChaveAssinatura="<>=32 bytes>" \
Acesso__AdminInicial__Senha="<senha-admin>" \
dotnet run --project src/Hosts/AgenteLocal
```

## Rodar como app desktop (Tauri) — requer toolchain Rust
O binário nativo compila em **Rust**, que **ainda não está instalado** nesta máquina. Depois de
instalar o Rust (https://rustup.rs) e os ícones do app:
```bash
npm run tauri icon      # gera src-tauri/icons/* (uma vez)
npm run tauri dev       # abre a janela nativa apontando pro Vite
```

## Estrutura
```
src/
├─ styles/globals.css        tokens (claro/escuro) — DESIGN_1 §2
├─ lib/                      utils (cn), theme (claro/escuro), sessão (licença+func — MOCK)
├─ components/ui/            primitivos: button, card, badge, tooltip
├─ components/shell/         casca: sidebar, topbar, command-palette, theme-toggle, app-shell
├─ modulos/registro.ts       catálogo de navegação (gated por módulo + funcionalidade)
├─ paginas/                  dashboard (demo) + placeholder
└─ App.tsx / main.tsx        composição + rotas
src-tauri/                   projeto Tauri 2 (Rust) — casca nativa
```

## Próximos passos
- Plugar autenticação real: sessão sai das claims do JWT (`func:<codigo>`), substituindo o mock
  em `src/lib/sessao.tsx`.
- TanStack Query + cliente HTTP para o host `.NET` (`/acesso/*`, módulos).
- Mais primitivos (DataTable via TanStack Table, Drawer, Toast, formulários RHF+Zod) conforme
  as telas de módulo forem construídas — sempre seguindo o `DESIGN_1.md`.
