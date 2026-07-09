import { NavLink } from 'react-router-dom'
import { PanelLeftClose, PanelLeft, Command } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { NAV } from '@/modulos/registro'
import { podeVer } from '@/lib/sessao'
import { useSessao } from '@/lib/auth'

interface SidebarProps {
  recolhida: boolean
  onAlternar: () => void
  onAbrirBusca: () => void
}

/**
 * Casca única (DESIGN_1 §3/§5): itens só aparecem se o módulo estiver
 * licenciado E o usuário tiver a funcionalidade. Recolhível (240px ⇄ 56px).
 */
export function Sidebar({ recolhida, onAlternar, onAbrirBusca }: SidebarProps) {
  const sessao = useSessao()
  const itens = NAV.filter((i) => podeVer(sessao, i.modulo, i.funcionalidade))

  return (
    <aside
      className={cn(
        'flex h-full flex-col border-r border-border bg-surface transition-[width] duration-base ease-standard',
        recolhida ? 'w-14' : 'w-60',
      )}
    >
      {/* marca */}
      <div className="flex h-13 items-center gap-2 px-3" style={{ height: 52 }}>
        <div className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-primary text-primary-fg font-semibold">
          S
        </div>
        {!recolhida && (
          <span className="truncate text-body font-semibold text-fg">Sioux ERP</span>
        )}
      </div>

      {/* busca (⌘K) */}
      <div className="px-2 pb-2">
        <button
          onClick={onAbrirBusca}
          className={cn(
            'flex w-full items-center gap-2 rounded-md border border-border bg-bg px-2 text-fg-muted transition-colors duration-fast hover:text-fg',
            recolhida ? 'h-9 justify-center' : 'h-9',
          )}
        >
          <Command className="size-4 shrink-0" />
          {!recolhida && (
            <>
              <span className="text-small">Buscar…</span>
              <kbd className="ml-auto rounded border border-border px-1 text-caption">⌘K</kbd>
            </>
          )}
        </button>
      </div>

      {/* navegação */}
      <nav className="flex-1 space-y-0.5 overflow-y-auto px-2">
        {itens.map((item) => {
          const Icone = item.icone
          const link = (
            <NavLink
              key={item.rota}
              to={item.rota}
              end={item.rota === '/'}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2.5 rounded-md px-2 py-2 text-body font-medium transition-colors duration-fast',
                  recolhida && 'justify-center',
                  isActive
                    ? 'bg-primary/10 text-primary'
                    : 'text-fg-muted hover:bg-black/5 hover:text-fg dark:hover:bg-white/5',
                )
              }
            >
              <Icone className="size-4 shrink-0" />
              {!recolhida && <span className="truncate">{item.titulo}</span>}
            </NavLink>
          )
          return recolhida ? (
            <Tooltip key={item.rota}>
              <TooltipTrigger asChild>{link}</TooltipTrigger>
              <TooltipContent side="right">{item.titulo}</TooltipContent>
            </Tooltip>
          ) : (
            link
          )
        })}
      </nav>

      {/* recolher */}
      <div className="border-t border-border p-2">
        <Button
          variant="ghost"
          size="icon"
          onClick={onAlternar}
          aria-label={recolhida ? 'Expandir menu' : 'Recolher menu'}
          className="w-full"
        >
          {recolhida ? <PanelLeft /> : <PanelLeftClose />}
        </Button>
      </div>
    </aside>
  )
}
