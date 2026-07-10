import * as CheckboxPrimitive from '@radix-ui/react-checkbox'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

interface Props {
  checked: boolean
  onChange: (v: boolean) => void
  label: string
  className?: string
}

/** Checkbox com rótulo (LGPD/marketing). DESIGN_1 — controle acessível via Radix. */
export function Checkbox({ checked, onChange, label, className }: Props) {
  return (
    <label className={cn('flex cursor-pointer select-none items-center gap-2', className)}>
      <CheckboxPrimitive.Root
        checked={checked}
        onCheckedChange={(v) => onChange(v === true)}
        className="flex size-4 shrink-0 items-center justify-center rounded border border-border bg-surface transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring data-[state=checked]:border-primary data-[state=checked]:bg-primary"
      >
        <CheckboxPrimitive.Indicator>
          <Check className="size-3 text-primary-fg" />
        </CheckboxPrimitive.Indicator>
      </CheckboxPrimitive.Root>
      <span className="text-small text-fg">{label}</span>
    </label>
  )
}
