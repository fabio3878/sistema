import { useEffect, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Combobox } from '@/components/ui/combobox'
import { Drawer, DrawerCancelar } from '@/components/ui/drawer'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { listarFormas, registrarRecebimento, sugerirRecebimento } from './api'
import { formatarData, formatarMoeda, hojeIso } from './formato'
import type { ContaReceber, Parcela, RecebimentoEntrada } from './tipos'

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  conta: ContaReceber | null
  parcela: Parcela | null
}

interface Campos {
  data: string
  valorRecebido: string
  desconto: string
  juros: string
  multa: string
  acrescimos: string
  formaPagamentoId: string
  observacoes: string
}

export function RecebimentoDrawer({ aberto, onAbrir, conta, parcela }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)

  const { register, control, handleSubmit, reset, watch, formState: { isSubmitting } } = useForm<Campos>({
    defaultValues: { data: hojeIso(), valorRecebido: '', desconto: '0', juros: '0', multa: '0', acrescimos: '0', formaPagamentoId: '', observacoes: '' },
  })

  const formas = useQuery({
    queryKey: ['formas-combo'],
    queryFn: ({ signal }) => listarFormas(requisitar, 'ativo', signal),
    enabled: aberto,
    staleTime: 60_000,
  })

  const sugestao = useQuery({
    queryKey: ['sugestao', parcela?.id],
    queryFn: ({ signal }) => sugerirRecebimento(requisitar, parcela!.id, signal),
    enabled: aberto && !!parcela,
  })

  useEffect(() => {
    if (!aberto || !parcela) return
    setErro(null)
    const s = sugestao.data
    reset({
      data: s?.data ?? hojeIso(),
      valorRecebido: String(s?.saldoPrincipal ?? parcela.saldoPrincipal),
      desconto: '0',
      juros: String(s?.juros ?? 0),
      multa: String(s?.multa ?? 0),
      acrescimos: '0',
      formaPagamentoId: '',
      observacoes: '',
    })
  }, [aberto, parcela, sugestao.data, reset])

  const salvar = useMutation({
    mutationFn: async (v: Campos) => {
      const dados: RecebimentoEntrada = {
        data: v.data,
        valorRecebido: Number(v.valorRecebido),
        formaPagamentoId: v.formaPagamentoId,
        desconto: Number(v.desconto) || 0,
        juros: Number(v.juros) || 0,
        multa: Number(v.multa) || 0,
        acrescimos: Number(v.acrescimos) || 0,
        observacoes: v.observacoes.trim() || null,
      }
      await registrarRecebimento(requisitar, parcela!.id, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['contas'] })
      onAbrir(false)
    },
    onError: (e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível registrar o recebimento.'),
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    if (!v.formaPagamentoId) return setErro('Selecione a forma de pagamento.')
    if (!(Number(v.valorRecebido) > 0)) return setErro('Informe o valor recebido.')
    salvar.mutate(v)
  })

  const opcoesForma = (formas.data ?? []).map((f) => ({ value: f.id, label: f.nome }))
  const valor = Number(watch('valorRecebido')) || 0
  const excede = parcela ? valor > parcela.saldoPrincipal + 0.01 : false

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo="Registrar recebimento"
      descricao={parcela ? `Parcela ${parcela.numero}/${parcela.totalParcelas} — ${conta?.clienteNome ?? ''}` : undefined}
      rodape={
        <>
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-receb" disabled={isSubmitting || excede}>
            {(isSubmitting || salvar.isPending) && <Loader2 className="animate-spin" />}
            Confirmar
          </Button>
        </>
      }
    >
      <form id="form-receb" onSubmit={onSubmit} className="space-y-6" noValidate>
        {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

        {parcela && (
          <div className="rounded-md border border-border bg-surface px-3 py-2 text-small text-fg-muted">
            Vencimento <span className="tnum text-fg">{formatarData(parcela.vencimento)}</span> · Saldo{' '}
            <span className="tnum text-fg">{formatarMoeda(parcela.saldoPrincipal)}</span>
            {parcela.diasAtraso > 0 && <> · <span className="text-danger">{parcela.diasAtraso} dias em atraso</span></>}
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <Campo rotulo="Data">
            <Input type="date" {...register('data')} />
          </Campo>
          <Campo rotulo="Forma de pagamento">
            <Controller
              control={control}
              name="formaPagamentoId"
              render={({ field }) => (
                <Combobox
                  value={field.value}
                  onChange={(v) => field.onChange(v)}
                  options={opcoesForma}
                  placeholder="Selecione…"
                  buscaPlaceholder="Buscar forma…"
                  vazioTexto="Nenhuma forma ativa."
                />
              )}
            />
          </Campo>
          <Campo rotulo="Valor recebido" erro={excede ? 'Maior que o saldo da parcela.' : undefined}>
            <Input type="number" step="0.01" min="0" {...register('valorRecebido')} />
          </Campo>
          <Campo rotulo="Desconto">
            <Input type="number" step="0.01" min="0" {...register('desconto')} />
          </Campo>
          <Campo rotulo="Juros">
            <Input type="number" step="0.01" min="0" {...register('juros')} />
          </Campo>
          <Campo rotulo="Multa">
            <Input type="number" step="0.01" min="0" {...register('multa')} />
          </Campo>
          <Campo rotulo="Acréscimos">
            <Input type="number" step="0.01" min="0" {...register('acrescimos')} />
          </Campo>
          <Campo className="col-span-full" rotulo="Observações">
            <Input {...register('observacoes')} />
          </Campo>
        </div>
      </form>
    </Drawer>
  )
}

function Campo({ rotulo, erro, className, children }: { rotulo: string; erro?: string; className?: string; children: React.ReactNode }) {
  return (
    <div className={`space-y-1.5 ${className ?? ''}`}>
      <label className="text-small font-medium text-fg">{rotulo}</label>
      {children}
      {erro && <p className="text-caption text-danger">{erro}</p>}
    </div>
  )
}
