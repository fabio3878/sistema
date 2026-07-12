import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, SlidersHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { cancelarConta, estornarRecebimento, listarContas } from '@/modulos/financeiro/api'
import { ContasTabela } from '@/modulos/financeiro/contas-tabela'
import { ContaDrawer } from '@/modulos/financeiro/conta-drawer'
import { RecebimentoDrawer } from '@/modulos/financeiro/recebimento-drawer'
import { ParametrosDrawer } from '@/modulos/financeiro/parametros-drawer'
import type { ContaReceber, FiltroContas, Parcela, Recebimento, SituacaoConta } from '@/modulos/financeiro/tipos'

const classeSelect =
  'h-9 rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

const TAMANHO = 20

export function ContasReceberPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const qc = useQueryClient()

  const podeCriar = podeVer(sessao, 'fin', 'fin.contareceber.criar')
  const podeEditar = podeVer(sessao, 'fin', 'fin.contareceber.editar')
  const podeCancelar = podeVer(sessao, 'fin', 'fin.contareceber.cancelar')
  const podeReceber = podeVer(sessao, 'fin', 'fin.recebimento.registrar')
  const podeEstornar = podeVer(sessao, 'fin', 'fin.recebimento.estornar')
  const podeParametros = podeVer(sessao, 'fin', 'fin.parametros.ver')

  const [buscaDraft, setBuscaDraft] = useState('')
  const [busca, setBusca] = useState('')
  const [situacao, setSituacao] = useState<SituacaoConta | ''>('')
  const [pagina, setPagina] = useState(1)

  const [contaDrawer, setContaDrawer] = useState(false)
  const [editandoId, setEditandoId] = useState<string | null>(null)
  const [recebDrawer, setRecebDrawer] = useState(false)
  const [alvo, setAlvo] = useState<{ conta: ContaReceber; parcela: Parcela } | null>(null)
  const [parametrosDrawer, setParametrosDrawer] = useState(false)

  const filtro: FiltroContas = {
    busca: busca || undefined,
    situacao: situacao || undefined,
    pagina,
    tamanho: TAMANHO,
  }

  const lista = useQuery({
    queryKey: ['contas', busca, situacao, pagina],
    queryFn: ({ signal }) => listarContas(requisitar, filtro, signal),
  })

  const cancelar = useMutation({
    mutationFn: (conta: ContaReceber) => cancelarConta(requisitar, conta.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['contas'] }),
  })

  const estornar = useMutation({
    mutationFn: (v: { parcelaId: string; recebimentoId: string; motivo: string | null }) =>
      estornarRecebimento(requisitar, v.parcelaId, v.recebimentoId, v.motivo),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['contas'] }),
  })

  const abrirNovaConta = () => {
    setEditandoId(null)
    setContaDrawer(true)
  }
  const abrirEdicao = (conta: ContaReceber) => {
    setEditandoId(conta.id)
    setContaDrawer(true)
  }
  const abrirRecebimento = (conta: ContaReceber, parcela: Parcela) => {
    setAlvo({ conta, parcela })
    setRecebDrawer(true)
  }
  const confirmarCancelar = (conta: ContaReceber) => {
    if (window.confirm(`Cancelar as parcelas em aberto desta conta (${conta.clienteNome ?? conta.clienteId})?`))
      cancelar.mutate(conta)
  }
  const confirmarEstorno = (_conta: ContaReceber, parcela: Parcela, rec: Recebimento) => {
    if (!window.confirm('Estornar este recebimento? O saldo da parcela será restaurado.')) return
    const motivo = window.prompt('Motivo do estorno (opcional):') ?? null
    estornar.mutate({ parcelaId: parcela.id, recebimentoId: rec.id, motivo })
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-h1 text-fg">Contas a receber</h1>
          <p className="text-small text-fg-muted">Contas, parcelas e recebimentos dos clientes.</p>
        </div>
        <div className="flex items-center gap-2">
          {podeParametros && (
            <Button variant="secondary" onClick={() => setParametrosDrawer(true)}>
              <SlidersHorizontal /> Parâmetros
            </Button>
          )}
          {podeCriar && (
            <Button onClick={abrirNovaConta}>
              <Plus /> Nova conta
            </Button>
          )}
        </div>
      </div>

      <form
        className="flex flex-wrap items-center gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          setBusca(buscaDraft)
          setPagina(1)
        }}
      >
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-fg-muted" />
          <Input
            value={buscaDraft}
            onChange={(e) => setBuscaDraft(e.target.value)}
            placeholder="Buscar por documento ou descrição…"
            className="pl-9"
          />
        </div>
        <select
          className={cn(classeSelect, 'w-48')}
          value={situacao}
          onChange={(e) => {
            setSituacao(e.target.value as SituacaoConta | '')
            setPagina(1)
          }}
        >
          <option value="">Toda situação</option>
          <option value="EmAberto">Em aberto</option>
          <option value="ParcialmenteRecebida">Parcialmente recebida</option>
          <option value="PossuiParcelasVencidas">Com parcelas vencidas</option>
          <option value="Quitada">Quitada</option>
          <option value="Cancelada">Cancelada</option>
        </select>
        <Button type="submit" variant="secondary">
          Filtrar
        </Button>
      </form>

      <ContasTabela
        pagina={lista.data}
        carregando={lista.isPending}
        podeEditar={podeEditar}
        podeCancelar={podeCancelar}
        podeReceber={podeReceber}
        podeEstornar={podeEstornar}
        onEditar={abrirEdicao}
        onCancelar={confirmarCancelar}
        onReceber={abrirRecebimento}
        onEstornar={confirmarEstorno}
        onPagina={setPagina}
      />

      <ContaDrawer aberto={contaDrawer} onAbrir={setContaDrawer} contaId={editandoId} />
      <RecebimentoDrawer aberto={recebDrawer} onAbrir={setRecebDrawer} conta={alvo?.conta ?? null} parcela={alvo?.parcela ?? null} />
      <ParametrosDrawer aberto={parametrosDrawer} onAbrir={setParametrosDrawer} podeEditar={podeVer(sessao, 'fin', 'fin.parametros.editar')} />
    </div>
  )
}
