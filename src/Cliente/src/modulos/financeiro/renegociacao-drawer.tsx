import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { Combobox } from '@/components/ui/combobox'
import { Drawer, DrawerCancelar } from '@/components/ui/drawer'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { enterComoTab } from '@/lib/enter-como-tab'
import { listarFormas, renegociar, sugerirRenegociacao } from './api'
import { formatarData, formatarMoeda, hojeIso } from './formato'
import type { ContaReceber, Parcela, ParcelaEntrada, RenegociacaoEntrada } from './tipos'

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  conta: ContaReceber | null
}

/** Parcelas que podem ser renegociadas: vivas e com saldo em aberto. */
function elegiveis(conta: ContaReceber | null): Parcela[] {
  if (!conta) return []
  return conta.parcelas.filter(
    (p) => p.status !== 'Cancelada' && p.status !== 'Renegociada' && p.saldoPrincipal > 0,
  )
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
    lista.push({ numero: i, valor, vencimento: venc.toISOString().slice(0, 10) })
  }
  return lista
}

export function RenegociacaoDrawer({ aberto, onAbrir, conta }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)

  const opcoesParcelas = useMemo(() => elegiveis(conta), [conta])

  const [selecionadas, setSelecionadas] = useState<Set<string>>(new Set())
  const [incluirEncargos, setIncluirEncargos] = useState(true)
  const [desconto, setDesconto] = useState('0')
  const [entrada, setEntrada] = useState('0')
  const [entradaFormaId, setEntradaFormaId] = useState('')
  const [qtd, setQtd] = useState('1')
  const [primeiroVenc, setPrimeiroVenc] = useState(hojeIso())
  const [intervalo, setIntervalo] = useState('30')
  const [observacoes, setObservacoes] = useState('')
  const [parcelas, setParcelas] = useState<ParcelaEntrada[]>([])
  const [parcelasEditadas, setParcelasEditadas] = useState(false)

  // Ao abrir: pré-seleciona todas as parcelas elegíveis e zera o formulário.
  useEffect(() => {
    if (!aberto) return
    setErro(null)
    setSelecionadas(new Set(opcoesParcelas.map((p) => p.id)))
    setIncluirEncargos(true)
    setDesconto('0')
    setEntrada('0')
    setEntradaFormaId('')
    setQtd('1')
    setPrimeiroVenc(hojeIso())
    setIntervalo('30')
    setObservacoes('')
    setParcelasEditadas(false)
  }, [aberto, opcoesParcelas])

  const idsSelecionados = useMemo(() => [...selecionadas].sort(), [selecionadas])

  const sugestao = useQuery({
    queryKey: ['sugestao-reneg', conta?.id, idsSelecionados, incluirEncargos],
    queryFn: ({ signal }) => sugerirRenegociacao(requisitar, conta!.id, idsSelecionados, incluirEncargos, signal),
    enabled: aberto && !!conta && idsSelecionados.length > 0,
  })

  const valorBase = sugestao.data?.saldoAtualizado ?? 0
  const valorRenegociado = Math.round((valorBase - (Number(desconto) || 0) - (Number(entrada) || 0)) * 100) / 100

  const formas = useQuery({
    queryKey: ['formas-combo'],
    queryFn: ({ signal }) => listarFormas(requisitar, 'ativo', signal),
    enabled: aberto,
    staleTime: 60_000,
  })
  const opcoesForma = (formas.data ?? []).map((f) => ({ value: f.id, label: f.nome }))

  // Auto-gera o preview do novo plano até o usuário editar manualmente.
  useEffect(() => {
    if (parcelasEditadas) return
    setParcelas(gerarPlano(valorRenegociado, Number(qtd), primeiroVenc, Number(intervalo) || 30))
  }, [parcelasEditadas, valorRenegociado, qtd, primeiroVenc, intervalo])

  const regenerar = () => {
    setParcelasEditadas(false)
    setParcelas(gerarPlano(valorRenegociado, Number(qtd), primeiroVenc, Number(intervalo) || 30))
  }

  const editarParcela = (i: number, patch: Partial<ParcelaEntrada>) => {
    setParcelasEditadas(true)
    setParcelas((atual) => atual.map((p, idx) => (idx === i ? { ...p, ...patch } : p)))
  }

  const alternarParcela = (id: string) => {
    setSelecionadas((atual) => {
      const novo = new Set(atual)
      novo.has(id) ? novo.delete(id) : novo.add(id)
      return novo
    })
  }

  const somaParcelas = useMemo(() => parcelas.reduce((s, p) => s + (Number(p.valor) || 0), 0), [parcelas])
  const somaConfere = Math.abs(somaParcelas - valorRenegociado) <= 0.01
  const entradaNum = Number(entrada) || 0

  const salvar = useMutation({
    mutationFn: async () => {
      const dados: RenegociacaoEntrada = {
        parcelaIds: idsSelecionados,
        primeiroVencimento: parcelas[0]?.vencimento ?? primeiroVenc,
        quantidadeParcelas: Number(qtd),
        incluirEncargos,
        desconto: Number(desconto) || 0,
        entrada: entradaNum,
        entradaFormaPagamentoId: entradaNum > 0 ? entradaFormaId : null,
        intervaloDias: Number(intervalo) || 30,
        parcelas: parcelas.map((p) => ({ numero: p.numero, valor: Number(p.valor), vencimento: p.vencimento })),
        observacoes: observacoes.trim() || null,
      }
      await renegociar(requisitar, conta!.id, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['contas'] })
      onAbrir(false)
    },
    onError: (e) => setErro(e instanceof ApiError ? e.message : 'Não foi possível renegociar.'),
  })

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setErro(null)
    if (idsSelecionados.length === 0) return setErro('Selecione ao menos uma parcela para renegociar.')
    if (!(valorRenegociado > 0)) return setErro('O valor a reparcelar (base − desconto − entrada) deve ser maior que zero.')
    if (entradaNum > 0 && !entradaFormaId) return setErro('Selecione a forma de pagamento da entrada.')
    if (!somaConfere) return setErro('A soma do novo plano não confere com o valor a reparcelar.')
    salvar.mutate()
  }

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo="Renegociar parcelas"
      descricao={conta ? `Conta de ${conta.clienteNome ?? conta.clienteId}` : undefined}
      rodape={
        <>
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-reneg" disabled={salvar.isPending || !somaConfere}>
            {salvar.isPending && <Loader2 className="animate-spin" />}
            Renegociar
          </Button>
        </>
      }
    >
      <form id="form-reneg" onSubmit={onSubmit} onKeyDown={enterComoTab} className="space-y-6" noValidate>
        {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

        <Secao titulo="Parcelas a renegociar">
          {opcoesParcelas.length === 0 ? (
            <p className="text-small text-fg-muted">Nenhuma parcela em aberto para renegociar.</p>
          ) : (
            <div className="space-y-1.5">
              {opcoesParcelas.map((p) => (
                <div key={p.id} className="flex items-center justify-between rounded-md border border-border px-3 py-1.5">
                  <Checkbox
                    checked={selecionadas.has(p.id)}
                    onChange={() => alternarParcela(p.id)}
                    label={`Parcela ${p.numero}/${p.totalParcelas} — vence ${formatarData(p.vencimento)}${p.diasAtraso > 0 ? ` (${p.diasAtraso}d atraso)` : ''}`}
                  />
                  <span className="tnum text-small text-fg">{formatarMoeda(p.saldoAtualizado)}</span>
                </div>
              ))}
            </div>
          )}
          <Checkbox
            checked={incluirEncargos}
            onChange={setIncluirEncargos}
            label="Incluir juros e multa de mora no valor renegociado"
            className="pt-1"
          />
        </Secao>

        <Secao titulo="Valores">
          <div className="grid grid-cols-2 gap-4">
            <Campo rotulo="Saldo consolidado">
              <div className="flex h-9 items-center rounded-md border border-border bg-bg px-3 tnum text-fg">
                {sugestao.isFetching ? <Loader2 className="size-4 animate-spin text-fg-muted" /> : formatarMoeda(valorBase)}
              </div>
            </Campo>
            <Campo rotulo="A reparcelar">
              <div className="flex h-9 items-center rounded-md border border-border bg-bg px-3 tnum font-medium text-fg">
                {formatarMoeda(valorRenegociado)}
              </div>
            </Campo>
            <Campo rotulo="Desconto">
              <Input type="number" step="0.01" min="0" value={desconto} onChange={(e) => setDesconto(e.target.value)} />
            </Campo>
            <Campo rotulo="Entrada">
              <Input type="number" step="0.01" min="0" value={entrada} onChange={(e) => setEntrada(e.target.value)} />
            </Campo>
            {entradaNum > 0 && (
              <Campo className="col-span-full" rotulo="Forma de pagamento da entrada">
                <Combobox
                  value={entradaFormaId}
                  onChange={(v) => setEntradaFormaId(v)}
                  options={opcoesForma}
                  placeholder="Selecione…"
                  buscaPlaceholder="Buscar forma…"
                  vazioTexto="Nenhuma forma ativa."
                />
              </Campo>
            )}
          </div>
        </Secao>

        <Secao titulo="Novo plano">
          <div className="grid grid-cols-3 gap-4">
            <Campo rotulo="Qtd. parcelas">
              <Input type="number" min="1" value={qtd} onChange={(e) => setQtd(e.target.value)} />
            </Campo>
            <Campo rotulo="1º vencimento">
              <Input type="date" value={primeiroVenc} onChange={(e) => setPrimeiroVenc(e.target.value)} />
            </Campo>
            <Campo rotulo="Intervalo (dias)">
              <Input type="number" min="1" value={intervalo} onChange={(e) => setIntervalo(e.target.value)} />
            </Campo>
          </div>

          <div className="flex items-center justify-between">
            <span className={`text-small ${somaConfere ? 'text-fg-muted' : 'text-danger'}`}>
              Soma: <span className="tnum">{formatarMoeda(somaParcelas)}</span> de{' '}
              <span className="tnum">{formatarMoeda(valorRenegociado)}</span>
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
                      <Input type="date" value={p.vencimento} onChange={(e) => editarParcela(i, { vencimento: e.target.value })} className="h-8" />
                    </td>
                    <td className="px-3 py-1">
                      <Input type="number" step="0.01" min="0" value={p.valor} onChange={(e) => editarParcela(i, { valor: Number(e.target.value) })} className="h-8 text-right" />
                    </td>
                  </tr>
                ))}
                {parcelas.length === 0 && (
                  <tr>
                    <td colSpan={3} className="px-3 py-4 text-center text-small text-fg-muted">
                      Selecione parcelas e informe a quantidade do novo plano.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <Campo className="col-span-full" rotulo="Observações">
            <Input value={observacoes} onChange={(e) => setObservacoes(e.target.value)} />
          </Campo>
        </Secao>
      </form>
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
