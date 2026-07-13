import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { Maximize2, Minimize2, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useJanelaArrastavel } from '@/lib/use-janela-arrastavel'
import { FOCAVEIS } from '@/lib/enter-como-tab'
import { Button } from '@/components/ui/button'

interface DrawerProps {
  aberto: boolean
  onAbrir: (v: boolean) => void
  titulo: string
  descricao?: string
  children: ReactNode
  /** Rodapé fixo (ações). Fica colado embaixo, fora da área rolável. */
  rodape?: ReactNode
  className?: string
}

const DrawerContext = createContext(false)

/** Indica se o drawer está maximizado (modal central). Use no formulário para reflowar em colunas. */
export function useDrawerMaximizado() {
  return useContext(DrawerContext)
}

/**
 * Painel lateral direito (DESIGN_1 §3: "drawer > modal" para criar/editar), com botão de
 * maximizar ⇄ restaurar (alterna para um modal central grande). Cabeçalho e rodapé fixos, corpo
 * rolável. Base no Radix Dialog (acessível, foco preso, Esc fecha).
 */
export function Drawer({ aberto, onAbrir, titulo, descricao, children, rodape, className }: DrawerProps) {
  // Abre maximizado (modal central) por padrão; o usuário pode restaurar para lateral na sessão.
  const [maximizado, setMaximizado] = useState(true)
  // Arraste pela barra de título só no modo maximizado (flutuante); recentra ao alternar o modo.
  const { alvoRef, estilo, propsBarra } = useJanelaArrastavel(aberto, maximizado)
  const corpoRef = useRef<HTMLDivElement>(null)

  // Ao (re)abrir, volta ao modo maximizado.
  useEffect(() => {
    if (aberto) setMaximizado(true)
  }, [aberto])

  return (
    <Dialog.Root open={aberto} onOpenChange={onAbrir}>
      <Dialog.Portal>
        <Dialog.Overlay className="ac-overlay-in fixed inset-0 z-50 bg-black/40" />
        <Dialog.Content
          ref={alvoRef}
          style={maximizado ? estilo : undefined}
          // Ao abrir, foca o 1º campo do corpo (ex.: Cliente) em vez do botão de maximizar/fechar do cabeçalho.
          onOpenAutoFocus={(e) => {
            const alvo = corpoRef.current?.querySelector<HTMLElement>(FOCAVEIS)
            if (alvo) {
              e.preventDefault()
              alvo.focus()
            }
          }}
          // Fechar só pelo X (ou Cancelar): clicar fora e Esc não fecham, para não perder o formulário.
          onEscapeKeyDown={(e) => e.preventDefault()}
          onPointerDownOutside={(e) => e.preventDefault()}
          onInteractOutside={(e) => e.preventDefault()}
          className={cn(
            'fixed z-50 flex flex-col border-border bg-elevated shadow-drawer focus:outline-none',
            maximizado
              ? 'ac-modal-in left-1/2 top-1/2 h-[90vh] w-[95vw] max-w-6xl rounded-xl border'
              : 'ac-drawer-in right-0 top-0 h-full w-full max-w-xl border-l',
            className,
          )}
        >
          <div
            {...(maximizado ? propsBarra : {})}
            className={cn(
              'flex items-start justify-between gap-4 border-b border-border px-6 py-4',
              maximizado && 'cursor-move select-none',
            )}
          >
            <div className="min-w-0">
              <Dialog.Title className="text-h3 text-fg">{titulo}</Dialog.Title>
              {descricao && (
                <Dialog.Description className="text-small text-fg-muted">{descricao}</Dialog.Description>
              )}
            </div>
            <div className="flex shrink-0 items-center gap-1">
              <Button
                variant="ghost"
                size="icon"
                aria-label={maximizado ? 'Restaurar' : 'Maximizar'}
                onClick={() => setMaximizado((m) => !m)}
              >
                {maximizado ? <Minimize2 /> : <Maximize2 />}
              </Button>
              <Dialog.Close asChild>
                <Button variant="ghost" size="icon" aria-label="Fechar">
                  <X />
                </Button>
              </Dialog.Close>
            </div>
          </div>

          <div ref={corpoRef} className="min-h-0 flex-1 overflow-y-auto px-6 py-5">
            <DrawerContext.Provider value={maximizado}>{children}</DrawerContext.Provider>
          </div>

          {rodape && (
            <div className="flex justify-end gap-2 border-t border-border px-6 py-4">{rodape}</div>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

/**
 * Botão "Cancelar" padrão do rodapé: confirma antes de fechar, para não descartar o formulário por
 * engano. O X do cabeçalho fecha direto; só o Cancelar pergunta.
 */
export function DrawerCancelar({
  onAbrir,
  rotulo = 'Cancelar',
}: {
  onAbrir: (v: boolean) => void
  rotulo?: string
}) {
  return (
    <Button
      variant="secondary"
      type="button"
      onClick={() => {
        if (window.confirm('Descartar as alterações e fechar?')) onAbrir(false)
      }}
    >
      {rotulo}
    </Button>
  )
}
