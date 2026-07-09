import { Bell } from 'lucide-react'
import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { Button } from '@/components/ui/button'
import { ThemeToggle } from './theme-toggle'
import { useSessao } from '@/lib/sessao'

interface TopbarProps {
  titulo: string
}

/** Barra superior minimalista (~52px): breadcrumb + ações + usuário. DESIGN_1 §3. */
export function Topbar({ titulo }: TopbarProps) {
  const { usuario } = useSessao()
  const iniciais = usuario.nome
    .split(' ')
    .map((p) => p[0])
    .slice(0, 2)
    .join('')

  return (
    <header
      className="flex items-center gap-3 border-b border-border bg-surface px-4"
      style={{ height: 52 }}
    >
      <div className="flex items-center gap-2 text-small text-fg-muted">
        <span>{usuario.empresa}</span>
        <span className="text-border">/</span>
        <span className="font-medium text-fg">{titulo}</span>
      </div>

      <div className="ml-auto flex items-center gap-1">
        <Button variant="ghost" size="icon" aria-label="Notificações">
          <Bell />
        </Button>
        <ThemeToggle />

        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <button
              className="ml-1 grid h-8 w-8 place-items-center rounded-full bg-primary/10 text-caption font-semibold text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              aria-label="Menu do usuário"
            >
              {iniciais}
            </button>
          </DropdownMenu.Trigger>
          <DropdownMenu.Portal>
            <DropdownMenu.Content
              align="end"
              sideOffset={8}
              className="z-50 min-w-52 rounded-lg border border-border bg-elevated p-1 shadow-popover"
            >
              <div className="px-2 py-1.5">
                <p className="text-small font-medium text-fg">{usuario.nome}</p>
                <p className="text-caption text-fg-muted">@{usuario.login}</p>
              </div>
              <DropdownMenu.Separator className="my-1 h-px bg-border" />
              <DropdownMenu.Item className="cursor-pointer rounded-md px-2 py-1.5 text-small text-fg outline-none data-[highlighted]:bg-black/5 dark:data-[highlighted]:bg-white/5">
                Trocar senha
              </DropdownMenu.Item>
              <DropdownMenu.Item className="cursor-pointer rounded-md px-2 py-1.5 text-small text-danger outline-none data-[highlighted]:bg-danger-bg">
                Sair
              </DropdownMenu.Item>
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
      </div>
    </header>
  )
}
