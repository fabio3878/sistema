import { useEffect } from 'react'
import { Command } from 'cmdk'
import { useNavigate } from 'react-router-dom'
import { NAV } from '@/modulos/registro'
import { podeVer, useSessao } from '@/lib/sessao'

interface CommandPaletteProps {
  aberto: boolean
  onAbrir: (v: boolean) => void
}

/** Busca global ⌘K (DESIGN_1 §3) — navega entre telas liberadas ao usuário. */
export function CommandPalette({ aberto, onAbrir }: CommandPaletteProps) {
  const navigate = useNavigate()
  const sessao = useSessao()
  const itens = NAV.filter((i) => podeVer(sessao, i.modulo, i.funcionalidade))

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault()
        onAbrir(!aberto)
      }
      if (e.key === 'Escape') onAbrir(false)
    }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [aberto, onAbrir])

  if (!aberto) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/40 pt-[15vh]"
      onClick={() => onAbrir(false)}
    >
      <Command
        className="w-full max-w-lg overflow-hidden rounded-xl border border-border bg-elevated shadow-drawer"
        onClick={(e) => e.stopPropagation()}
      >
        <Command.Input
          autoFocus
          placeholder="Buscar telas, ações, registros…"
          className="w-full border-b border-border bg-transparent px-4 py-3 text-body text-fg outline-none placeholder:text-fg-muted"
        />
        <Command.List className="max-h-80 overflow-y-auto p-2">
          <Command.Empty className="px-3 py-6 text-center text-small text-fg-muted">
            Nada encontrado.
          </Command.Empty>
          <Command.Group
            heading="Navegar"
            className="[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-1 [&_[cmdk-group-heading]]:text-caption [&_[cmdk-group-heading]]:text-fg-muted"
          >
            {itens.map((item) => {
              const Icone = item.icone
              return (
                <Command.Item
                  key={item.rota}
                  value={item.titulo}
                  onSelect={() => {
                    navigate(item.rota)
                    onAbrir(false)
                  }}
                  className="flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-2 text-body text-fg outline-none data-[selected=true]:bg-primary/10 data-[selected=true]:text-primary"
                >
                  <Icone className="size-4" />
                  {item.titulo}
                </Command.Item>
              )
            })}
          </Command.Group>
        </Command.List>
      </Command>
    </div>
  )
}
