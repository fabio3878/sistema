import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Drawer, DrawerCancelar } from '@/components/ui/drawer'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { enterComoTab } from '@/lib/enter-como-tab'
import { atualizarForma, criarForma } from './api'
import type { FormaPagamento } from './tipos'

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  forma: FormaPagamento | null
}

export function FormaDrawer({ aberto, onAbrir, forma }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const editando = forma !== null
  const [erro, setErro] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ nome: string }>({
    defaultValues: { nome: '' },
  })

  useEffect(() => {
    if (!aberto) return
    setErro(null)
    reset({ nome: forma?.nome ?? '' })
  }, [aberto, forma, reset])

  const salvar = useMutation({
    mutationFn: async (v: { nome: string }) => {
      if (editando) await atualizarForma(requisitar, forma!.id, v.nome.trim())
      else await criarForma(requisitar, v.nome.trim())
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['formas'] })
      await qc.invalidateQueries({ queryKey: ['formas-combo'] })
      onAbrir(false)
    },
    onError: (e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar a forma de pagamento.'),
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    if (!v.nome.trim()) return setErro('Informe o nome.')
    salvar.mutate(v)
  })

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo={editando ? 'Editar forma de pagamento' : 'Nova forma de pagamento'}
      rodape={
        <>
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-forma" disabled={isSubmitting}>
            {(isSubmitting || salvar.isPending) && <Loader2 className="animate-spin" />}
            Salvar
          </Button>
        </>
      }
    >
      <form id="form-forma" onSubmit={onSubmit} onKeyDown={enterComoTab} className="space-y-4" noValidate>
        {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}
        <div className="space-y-1.5">
          <label className="text-small font-medium text-fg">Nome</label>
          <Input {...register('nome')} placeholder="Ex.: Pix, Dinheiro, Cartão…" />
        </div>
      </form>
    </Drawer>
  )
}
