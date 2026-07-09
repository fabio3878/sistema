import { Construction } from 'lucide-react'
import { Button } from '@/components/ui/button'

/** Empty state de módulo ainda não construído (DESIGN_1 §6). */
export function PlaceholderPage({ titulo }: { titulo: string }) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center justify-center gap-3 py-24 text-center">
      <span className="grid size-12 place-items-center rounded-full bg-primary/10 text-primary">
        <Construction className="size-6" />
      </span>
      <h1 className="text-h2 text-fg">{titulo}</h1>
      <p className="text-small text-fg-muted">
        Este módulo ainda não foi construído. A casca já está pronta — a tela entra aqui
        seguindo o DESIGN_1.md.
      </p>
      <Button variant="secondary" size="sm">
        Ver documentação
      </Button>
    </div>
  )
}
