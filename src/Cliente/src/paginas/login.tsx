import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ThemeToggle } from '@/components/shell/theme-toggle'
import { useAuth } from '@/lib/auth'
import { ApiError } from '@/lib/api'

const schema = z.object({
  login: z.string().min(1, 'Informe o login'),
  senha: z.string().min(1, 'Informe a senha'),
})
type Campos = z.infer<typeof schema>

export function LoginPage() {
  const { entrar } = useAuth()
  const [erro, setErro] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Campos>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async ({ login, senha }) => {
    setErro(null)
    try {
      await entrar(login, senha)
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível entrar. Tente novamente.')
    }
  })

  return (
    <div className="relative flex h-full items-center justify-center bg-bg p-6">
      <div className="absolute right-4 top-4">
        <ThemeToggle />
      </div>

      <div className="w-full max-w-sm">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <div className="grid h-11 w-11 place-items-center rounded-lg bg-primary text-primary-fg text-h2 font-semibold">
            S
          </div>
          <div>
            <h1 className="text-h1 text-fg">Automação Comercial</h1>
            <p className="text-small text-fg-muted">Entre com suas credenciais</p>
          </div>
        </div>

        <form onSubmit={onSubmit} className="space-y-4" noValidate>
          <div className="space-y-1.5">
            <label htmlFor="login" className="text-small font-medium text-fg">
              Login
            </label>
            <Input id="login" autoFocus autoComplete="username" {...register('login')} />
            {errors.login && <p className="text-caption text-danger">{errors.login.message}</p>}
          </div>

          <div className="space-y-1.5">
            <label htmlFor="senha" className="text-small font-medium text-fg">
              Senha
            </label>
            <Input id="senha" type="password" autoComplete="current-password" {...register('senha')} />
            {errors.senha && <p className="text-caption text-danger">{errors.senha.message}</p>}
          </div>

          {erro && (
            <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>
          )}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting && <Loader2 className="animate-spin" />}
            Entrar
          </Button>
        </form>
      </div>
    </div>
  )
}
