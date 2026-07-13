import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Drawer, DrawerCancelar } from '@/components/ui/drawer'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { enterComoTab } from '@/lib/enter-como-tab'
import { atualizarParametros, obterParametros } from './api'

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  podeEditar: boolean
}

/** Parâmetros de juros de mora + multa (padrão da empresa; a parcela pode ter override). */
export function ParametrosDrawer({ aberto, onAbrir, podeEditar }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm<{ jurosMoraMensalPercent: string; multaPercent: string }>({
    defaultValues: { jurosMoraMensalPercent: '0', multaPercent: '0' },
  })

  const consulta = useQuery({
    queryKey: ['parametros'],
    queryFn: ({ signal }) => obterParametros(requisitar, signal),
    enabled: aberto,
  })

  useEffect(() => {
    if (!aberto || !consulta.data) return
    setErro(null)
    reset({
      jurosMoraMensalPercent: String(consulta.data.jurosMoraMensalPercent),
      multaPercent: String(consulta.data.multaPercent),
    })
  }, [aberto, consulta.data, reset])

  const salvar = useMutation({
    mutationFn: (v: { jurosMoraMensalPercent: string; multaPercent: string }) =>
      atualizarParametros(requisitar, {
        jurosMoraMensalPercent: Number(v.jurosMoraMensalPercent) || 0,
        multaPercent: Number(v.multaPercent) || 0,
      }),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['parametros'] })
      onAbrir(false)
    },
    onError: (e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar os parâmetros.'),
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    salvar.mutate(v)
  })

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo="Parâmetros financeiros"
      descricao="Juros de mora e multa padrão para recebimentos em atraso."
      rodape={
        podeEditar ? (
          <>
            <DrawerCancelar onAbrir={onAbrir} />
            <Button type="submit" form="form-parametros" disabled={isSubmitting}>
              {(isSubmitting || salvar.isPending) && <Loader2 className="animate-spin" />}
              Salvar
            </Button>
          </>
        ) : (
          <Button variant="secondary" type="button" onClick={() => onAbrir(false)}>
            Fechar
          </Button>
        )
      }
    >
      <form id="form-parametros" onSubmit={onSubmit} onKeyDown={enterComoTab} className="space-y-4" noValidate>
        {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <label className="text-small font-medium text-fg">Juros de mora (% ao mês)</label>
            <Input type="number" step="0.01" min="0" disabled={!podeEditar} {...register('jurosMoraMensalPercent')} />
          </div>
          <div className="space-y-1.5">
            <label className="text-small font-medium text-fg">Multa (%)</label>
            <Input type="number" step="0.01" min="0" disabled={!podeEditar} {...register('multaPercent')} />
          </div>
        </div>
        <p className="text-caption text-fg-muted">
          O juros é aplicado pro rata sobre os dias em atraso; a multa, uma vez sobre o saldo. Uma parcela pode
          sobrescrever o juros individualmente.
        </p>
      </form>
    </Drawer>
  )
}
