import { useState } from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useAuth } from '@/lib/auth'
import { ApiError } from '@/lib/api'

const schema = z
  .object({
    senhaAtual: z.string().min(1, 'Informe a senha atual'),
    senhaNova: z.string().min(8, 'Mínimo de 8 caracteres'),
    confirmar: z.string(),
  })
  .refine((d) => d.senhaNova === d.confirmar, {
    path: ['confirmar'],
    message: 'As senhas não conferem',
  })
type Campos = z.infer<typeof schema>

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
}

/**
 * Troca de senha (POST /acesso/trocar-senha). Ao concluir, o backend revoga os
 * refresh tokens; o AuthProvider desloga e a tela volta pro login.
 */
export function TrocarSenhaDialog({ aberto, onAbrir }: Props) {
  const { trocarSenha } = useAuth()
  const [erro, setErro] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<Campos>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async ({ senhaAtual, senhaNova }) => {
    setErro(null)
    try {
      await trocarSenha(senhaAtual, senhaNova)
      reset()
      onAbrir(false)
      // trocarSenha desloga; a app renderiza o login automaticamente.
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível trocar a senha.')
    }
  })

  return (
    <Dialog.Root open={aberto} onOpenChange={onAbrir}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/40 data-[state=open]:animate-in data-[state=open]:fade-in-0" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-xl border border-border bg-elevated p-6 shadow-drawer focus:outline-none">
          <div className="mb-4 flex items-start justify-between">
            <div>
              <Dialog.Title className="text-h3 text-fg">Trocar senha</Dialog.Title>
              <Dialog.Description className="text-small text-fg-muted">
                Você precisará entrar de novo com a nova senha.
              </Dialog.Description>
            </div>
            <Dialog.Close asChild>
              <Button variant="ghost" size="icon" aria-label="Fechar">
                <X />
              </Button>
            </Dialog.Close>
          </div>

          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <div className="space-y-1.5">
              <label htmlFor="senhaAtual" className="text-small font-medium text-fg">
                Senha atual
              </label>
              <Input id="senhaAtual" type="password" autoComplete="current-password" {...register('senhaAtual')} />
              {errors.senhaAtual && (
                <p className="text-caption text-danger">{errors.senhaAtual.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <label htmlFor="senhaNova" className="text-small font-medium text-fg">
                Nova senha
              </label>
              <Input id="senhaNova" type="password" autoComplete="new-password" {...register('senhaNova')} />
              {errors.senhaNova && (
                <p className="text-caption text-danger">{errors.senhaNova.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <label htmlFor="confirmar" className="text-small font-medium text-fg">
                Confirmar nova senha
              </label>
              <Input id="confirmar" type="password" autoComplete="new-password" {...register('confirmar')} />
              {errors.confirmar && (
                <p className="text-caption text-danger">{errors.confirmar.message}</p>
              )}
            </div>

            {erro && (
              <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>
            )}

            <div className="flex justify-end gap-2 pt-1">
              <Dialog.Close asChild>
                <Button variant="secondary" type="button">
                  Cancelar
                </Button>
              </Dialog.Close>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="animate-spin" />}
                Trocar senha
              </Button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
