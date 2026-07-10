import { useState } from 'react'
import * as Popover from '@radix-ui/react-popover'
import { Command } from 'cmdk'
import { Check, ChevronsUpDown } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface OpcaoCombo {
  value: string
  label: string
}

interface Props {
  value: string
  onChange: (value: string, label: string) => void
  options: OpcaoCombo[]
  placeholder?: string
  buscaPlaceholder?: string
  vazioTexto?: string
  disabled?: boolean
  className?: string
}

/**
 * Combobox com busca (Popover + cmdk). Base do seletor de cidade IBGE: as opções são carregadas
 * pelo pai (por UF) e a escolha devolve value+label (código IBGE + nome), sem digitação manual.
 */
export function Combobox({
  value,
  onChange,
  options,
  placeholder = 'Selecione…',
  buscaPlaceholder = 'Buscar…',
  vazioTexto = 'Nada encontrado.',
  disabled,
  className,
}: Props) {
  const [aberto, setAberto] = useState(false)
  const selecionada = options.find((o) => o.value === value)

  return (
    <Popover.Root open={aberto} onOpenChange={setAberto}>
      <Popover.Trigger asChild disabled={disabled}>
        <button
          type="button"
          className={cn(
            'flex h-9 w-full items-center justify-between gap-2 rounded-md border border-border bg-surface px-3 text-body',
            'transition-colors duration-fast focus:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            'disabled:cursor-not-allowed disabled:opacity-50',
            className,
          )}
        >
          <span className={cn('truncate', !selecionada && 'text-fg-muted')}>
            {selecionada?.label ?? placeholder}
          </span>
          <ChevronsUpDown className="size-4 shrink-0 text-fg-muted" />
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          className="z-[70] min-w-[15rem] max-w-[min(20rem,90vw)] overflow-hidden rounded-md border border-border bg-elevated shadow-popover"
        >
          <Command className="max-h-72">
            <Command.Input
              placeholder={buscaPlaceholder}
              className="w-full border-b border-border bg-transparent px-3 py-2 text-body text-fg outline-none placeholder:text-fg-muted"
            />
            <Command.List className="max-h-60 overflow-y-auto p-1">
              <Command.Empty className="px-3 py-4 text-center text-small text-fg-muted">{vazioTexto}</Command.Empty>
              {options.map((o) => (
                <Command.Item
                  key={o.value}
                  value={o.label}
                  onSelect={() => {
                    onChange(o.value, o.label)
                    setAberto(false)
                  }}
                  className="flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-body text-fg outline-none data-[selected=true]:bg-primary/10 data-[selected=true]:text-primary"
                >
                  <Check className={cn('size-4 shrink-0', o.value === value ? 'opacity-100' : 'opacity-0')} />
                  <span className="truncate">{o.label}</span>
                </Command.Item>
              ))}
            </Command.List>
          </Command>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
