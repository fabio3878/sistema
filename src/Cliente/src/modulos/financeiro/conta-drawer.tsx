import { useEffect, useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Drawer, DrawerCancelar } from '@/components/ui/drawer'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { SeletorCliente } from '@/modulos/cadastros/seletor-cliente'
import { atualizarCabecalho, criarConta, obterConta } from './api'
import { formatarMoeda, hojeIso } from './formato'
import type { ContaEntrada, ParcelaEntrada } from './tipos'

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  contaId: string | null
}

interface Campos {
  clienteId: string
  valorTotal: string
  quantidadeParcelas: string
  dataEmissao: string
  primeiroVencimento: string
  intervaloDias: string
  descricao: string
  documentoOrigem: string
  numeroDocumento: string
  categoriaFinanceira: string
  observacoes: string
}

function vazio(): Campos {
  return {
    clienteId: '',
    valorTotal: '',
    quantidadeParcelas: '1',
    dataEmissao: hojeIso(),
    primeiroVencimento: hojeIso(),
    intervaloDias: '30',
    descricao: '',
    documentoOrigem: '',
    numeroDocumento: '',
    categoriaFinanceira: '',
    observacoes: '',
  }
}

/** Gera o plano de parcelas iguais (ajuste de centavos na última), espelhando o backend. */
function gerarPlano(total: number, qtd: number, primeiro: string, intervalo: number): ParcelaEntrada[] {
  if (!(total > 0) || !(qtd >= 1) || !primeiro) return []
  const base = Math.trunc((total / qtd) * 100) / 100
  const lista: ParcelaEntrada[] = []
  let acumulado = 0
  const [y, m, d] = primeiro.split('-').map(Number)
  for (let i = 1; i <= qtd; i++) {
    const valor = i < qtd ? base : Math.round((total - acumulado) * 100) / 100
    if (i < qtd) acumulado = Math.round((acumulado + base) * 100) / 100
    const venc = new Date(Date.UTC(y, m - 1, d + intervalo * (i - 1)))
    const iso = venc.toISOString().slice(0, 10)
    lista.push({ numero: i, valor, vencimento: iso })
  }
  return lista
}

export function ContaDrawer({ aberto, onAbrir, contaId }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const editando = contaId !== null
  const [erro, setErro] = useState<string | null>(null)
  const [parcelas, setParcelas] = useState<ParcelaEntrada[]>([])
  const [parcelasEditadas, setParcelasEditadas] = useState(false)

  const { register, control, handleSubmit, reset, watch, formState: { isSubmitting } } = useForm<Campos>({
    defaultValues: vazio(),
  })

  const detalhe = useQuery({
    queryKey: ['conta', contaId],
    queryFn: ({ signal }) => obterConta(requisitar, contaId!, signal),
    enabled: aberto && editando,
  })

  useEffect(() => {
    if (!aberto) return
    setErro(null)
    setParcelasEditadas(false)
    if (!editando) {
      reset(vazio())
      setParcelas([])
    } else if (detalhe.data) {
      const c = detalhe.data
      reset({
        clienteId: c.clienteId,
        valorTotal: String(c.valorTotal),
        quantidadeParcelas: String(c.quantidadeParcelas),
        dataEmissao: c.dataEmissao,
        primeiroVencimento: c.parcelas[0]?.vencimento ?? hojeIso(),
        intervaloDias: '30',
        descricao: c.descricao ?? '',
        documentoOrigem: c.documentoOrigem ?? '',
        numeroDocumento: c.numeroDocumento ?? '',
        categoriaFinanceira: c.categoriaFinanceira ?? '',
        observacoes: c.observacoes ?? '',
      })
    }
  }, [aberto, editando, detalhe.data, reset])

  // Auto-gera o preview de parcelas na criação, até o usuário editar manualmente.
  const valorTotal = watch('valorTotal')
  const quantidade = watch('quantidadeParcelas')
  const primeiroVenc = watch('primeiroVencimento')
  const intervalo = watch('intervaloDias')

  useEffect(() => {
    if (editando || parcelasEditadas) return
    setParcelas(gerarPlano(Number(valorTotal), Number(quantidade), primeiroVenc, Number(intervalo) || 30))
  }, [editando, parcelasEditadas, valorTotal, quantidade, primeiroVenc, intervalo])

  const regenerar = () => {
    setParcelasEditadas(false)
    setParcelas(gerarPlano(Number(valorTotal), Number(quantidade), primeiroVenc, Number(intervalo) || 30))
  }

  const editarParcela = (i: number, patch: Partial<ParcelaEntrada>) => {
    setParcelasEditadas(true)
    setParcelas((atual) => atual.map((p, idx) => (idx === i ? { ...p, ...patch } : p)))
  }

  const somaParcelas = useMemo(() => parcelas.reduce((s, p) => s + (Number(p.valor) || 0), 0), [parcelas])
  const totalNumero = Number(valorTotal) || 0
  const somaConfere = Math.abs(somaParcelas - totalNumero) <= 0.01

  const salvar = useMutation({
    mutationFn: async (v: Campos) => {
      if (editando) {
        await atualizarCabecalho(requisitar, contaId!, {
          descricao: v.descricao.trim() || null,
          documentoOrigem: v.documentoOrigem.trim() || null,
          numeroDocumento: v.numeroDocumento.trim() || null,
          categoriaFinanceira: v.categoriaFinanceira.trim() || null,
          observacoes: v.observacoes.trim() || null,
        })
        return
      }
      const dados: ContaEntrada = {
        clienteId: v.clienteId,
        valorTotal: Number(v.valorTotal),
        quantidadeParcelas: Number(v.quantidadeParcelas),
        dataEmissao: v.dataEmissao,
        primeiroVencimento: parcelas[0]?.vencimento ?? v.primeiroVencimento,
        intervaloDias: Number(v.intervaloDias) || 30,
        descricao: v.descricao.trim() || null,
        documentoOrigem: v.documentoOrigem.trim() || null,
        numeroDocumento: v.numeroDocumento.trim() || null,
        categoriaFinanceira: v.categoriaFinanceira.trim() || null,
        observacoes: v.observacoes.trim() || null,
        parcelas: parcelas.map((p) => ({ numero: p.numero, valor: Number(p.valor), vencimento: p.vencimento })),
      }
      await criarConta(requisitar, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['contas'] })
      if (editando) await qc.invalidateQueries({ queryKey: ['conta', contaId] })
      onAbrir(false)
    },
    onError: (e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível salvar a conta.'),
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    if (!editando) {
      if (!v.clienteId) return setErro('Selecione o cliente.')
      if (!(Number(v.valorTotal) > 0)) return setErro('Informe o valor total.')
      if (!somaConfere) return setErro('A soma das parcelas não confere com o valor total.')
    }
    salvar.mutate(v)
  })

  const carregando = editando && detalhe.isLoading

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo={editando ? 'Editar conta a receber' : 'Nova conta a receber'}
      descricao={editando ? 'Edite os dados do cabeçalho.' : 'Informe o cliente, o valor e as parcelas.'}
      rodape={
        <>
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-conta" disabled={isSubmitting || carregando}>
            {(isSubmitting || salvar.isPending) && <Loader2 className="animate-spin" />}
            Salvar
          </Button>
        </>
      }
    >
      {carregando ? (
        <div className="grid place-items-center py-16 text-fg-muted">
          <Loader2 className="size-6 animate-spin" />
        </div>
      ) : (
        <form id="form-conta" onSubmit={onSubmit} className="space-y-6" noValidate>
          {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

          <Secao titulo="Dados">
            <div className="grid grid-cols-2 gap-4">
              <Campo className="col-span-full" rotulo="Cliente">
                <Controller
                  control={control}
                  name="clienteId"
                  render={({ field }) => (
                    <SeletorCliente
                      value={field.value}
                      onChange={(id) => field.onChange(id)}
                      disabled={editando}
                      rotuloInicial={editando ? detalhe.data?.clienteNome ?? undefined : undefined}
                    />
                  )}
                />
              </Campo>
              <Campo rotulo="Valor total">
                <Input type="number" step="0.01" min="0" disabled={editando} {...register('valorTotal')} />
              </Campo>
              <Campo rotulo="Qtd. parcelas">
                <Input type="number" min="1" disabled={editando} {...register('quantidadeParcelas')} />
              </Campo>
              <Campo rotulo="Emissão">
                <Input type="date" disabled={editando} {...register('dataEmissao')} />
              </Campo>
              {!editando && (
                <>
                  <Campo rotulo="1º vencimento">
                    <Input type="date" {...register('primeiroVencimento')} />
                  </Campo>
                  <Campo rotulo="Intervalo (dias)">
                    <Input type="number" min="1" {...register('intervaloDias')} />
                  </Campo>
                </>
              )}
              <Campo rotulo="Nº documento">
                <Input {...register('numeroDocumento')} placeholder="Ex.: NF 1234" />
              </Campo>
              <Campo rotulo="Categoria financeira">
                <Input {...register('categoriaFinanceira')} placeholder="Ex.: Vendas" />
              </Campo>
              <Campo className="col-span-full" rotulo="Descrição">
                <Input {...register('descricao')} />
              </Campo>
              <Campo className="col-span-full" rotulo="Observações">
                <Input {...register('observacoes')} />
              </Campo>
            </div>
          </Secao>

          {!editando && (
            <Secao titulo="Parcelas">
              <div className="flex items-center justify-between">
                <span className={`text-small ${somaConfere ? 'text-fg-muted' : 'text-danger'}`}>
                  Soma: <span className="tnum">{formatarMoeda(somaParcelas)}</span> de{' '}
                  <span className="tnum">{formatarMoeda(totalNumero)}</span>
                </span>
                <Button type="button" variant="ghost" size="sm" onClick={regenerar}>
                  <RefreshCw /> Regerar
                </Button>
              </div>
              <div className="overflow-hidden rounded-md border border-border">
                <table className="w-full text-body">
                  <thead className="bg-surface">
                    <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
                      <th className="px-3 py-1.5 text-left">Nº</th>
                      <th className="px-3 py-1.5 text-left">Vencimento</th>
                      <th className="px-3 py-1.5 text-right">Valor</th>
                    </tr>
                  </thead>
                  <tbody>
                    {parcelas.map((p, i) => (
                      <tr key={i} className="border-b border-border last:border-0">
                        <td className="px-3 py-1 tnum text-fg-muted">{p.numero}</td>
                        <td className="px-3 py-1">
                          <Input
                            type="date"
                            value={p.vencimento}
                            onChange={(e) => editarParcela(i, { vencimento: e.target.value })}
                            className="h-8"
                          />
                        </td>
                        <td className="px-3 py-1">
                          <Input
                            type="number"
                            step="0.01"
                            min="0"
                            value={p.valor}
                            onChange={(e) => editarParcela(i, { valor: Number(e.target.value) })}
                            className="h-8 text-right"
                          />
                        </td>
                      </tr>
                    ))}
                    {parcelas.length === 0 && (
                      <tr>
                        <td colSpan={3} className="px-3 py-4 text-center text-small text-fg-muted">
                          Informe o valor total e a quantidade de parcelas.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </Secao>
          )}
        </form>
      )}
    </Drawer>
  )
}

function Secao({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <h4 className="text-small font-semibold uppercase tracking-wide text-fg-muted">{titulo}</h4>
      {children}
    </section>
  )
}

function Campo({ rotulo, className, children }: { rotulo: string; className?: string; children: React.ReactNode }) {
  return (
    <div className={`space-y-1.5 ${className ?? ''}`}>
      <label className="text-small font-medium text-fg">{rotulo}</label>
      {children}
    </div>
  )
}
