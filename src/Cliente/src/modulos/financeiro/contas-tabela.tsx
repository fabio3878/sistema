import { useState } from 'react'
import { ChevronDown, ChevronRight, Pencil, Ban, HandCoins, Handshake, Undo2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { formatarData, formatarMoeda, rotuloSituacao, rotuloStatus, tomSituacao, tomStatus } from './formato'
import type { ContaReceber, Parcela, Recebimento } from './tipos'

interface Props {
  pagina: { itens: ContaReceber[]; total: number; pagina: number; tamanho: number } | undefined
  carregando: boolean
  podeEditar: boolean
  podeCancelar: boolean
  podeRenegociar: boolean
  podeReceber: boolean
  podeEstornar: boolean
  onEditar: (conta: ContaReceber) => void
  onCancelar: (conta: ContaReceber) => void
  onRenegociar: (conta: ContaReceber) => void
  onReceber: (conta: ContaReceber, parcela: Parcela) => void
  onEstornar: (conta: ContaReceber, parcela: Parcela, recebimento: Recebimento) => void
  onPagina: (p: number) => void
}

/** Tabela em árvore Conta → Parcela → Recebimento, com expansão e paginação server-side. */
export function ContasTabela({
  pagina,
  carregando,
  podeEditar,
  podeCancelar,
  podeRenegociar,
  podeReceber,
  podeEstornar,
  onEditar,
  onCancelar,
  onRenegociar,
  onReceber,
  onEstornar,
  onPagina,
}: Props) {
  const [contasAbertas, setContasAbertas] = useState<Set<string>>(new Set())
  const [parcelasAbertas, setParcelasAbertas] = useState<Set<string>>(new Set())

  const alternar = (set: Set<string>, setter: (s: Set<string>) => void, id: string) => {
    const novo = new Set(set)
    novo.has(id) ? novo.delete(id) : novo.add(id)
    setter(novo)
  }

  if (carregando) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="h-11 animate-pulse rounded-md bg-surface" />
        ))}
      </div>
    )
  }

  const itens = pagina?.itens ?? []
  if (itens.length === 0) {
    return (
      <div className="grid place-items-center rounded-lg border border-dashed border-border py-16 text-fg-muted">
        Nenhuma conta a receber encontrada.
      </div>
    )
  }

  const total = pagina?.total ?? 0
  const tamanho = pagina?.tamanho ?? 20
  const atual = pagina?.pagina ?? 1
  const totalPaginas = Math.max(1, Math.ceil(total / tamanho))

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-body">
          <thead className="bg-surface">
            <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
              <th className="w-8 px-2 py-2" />
              <th className="px-3 py-2 text-left">Cliente</th>
              <th className="px-3 py-2 text-left">Documento</th>
              <th className="px-3 py-2 text-left">Emissão</th>
              <th className="px-3 py-2 text-right">Valor total</th>
              <th className="px-3 py-2 text-center">Parcelas</th>
              <th className="px-3 py-2 text-right">Recebido</th>
              <th className="px-3 py-2 text-right">Saldo</th>
              <th className="px-3 py-2 text-left">Situação</th>
              <th className="px-2 py-2" />
            </tr>
          </thead>
          <tbody>
            {itens.map((conta) => {
              const aberta = contasAbertas.has(conta.id)
              const recebidas = conta.parcelas.filter((p) => p.status === 'Recebida').length
              const cancelavel = conta.situacao !== 'Cancelada' && conta.situacao !== 'Quitada'
              const renegociavel = conta.parcelas.some(
                (p) => p.status !== 'Cancelada' && p.status !== 'Renegociada' && p.saldoPrincipal > 0,
              )
              return (
                <FragmentConta key={conta.id}>
                  <tr
                    className="border-b border-border hover:bg-black/[0.02] dark:hover:bg-white/[0.03]"
                    style={{ height: 44 }}
                  >
                    <td className="px-2 text-center">
                      <button
                        aria-label={aberta ? 'Recolher' : 'Expandir'}
                        onClick={() => alternar(contasAbertas, setContasAbertas, conta.id)}
                        className="text-fg-muted hover:text-fg"
                      >
                        {aberta ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
                      </button>
                    </td>
                    <td className="px-3 py-1.5 align-middle text-fg">
                      {conta.clienteNome ?? <span className="tnum text-fg-muted">{conta.clienteId}</span>}
                      {conta.descricao && <div className="text-caption text-fg-muted">{conta.descricao}</div>}
                    </td>
                    <td className="px-3 py-1.5 align-middle text-fg-muted">{conta.numeroDocumento ?? conta.documentoOrigem ?? '—'}</td>
                    <td className="px-3 py-1.5 align-middle tnum text-fg-muted">{formatarData(conta.dataEmissao)}</td>
                    <td className="px-3 py-1.5 align-middle text-right tnum text-fg">{formatarMoeda(conta.valorTotal)}</td>
                    <td className="px-3 py-1.5 align-middle text-center tnum text-fg-muted">
                      {recebidas}/{conta.quantidadeParcelas}
                    </td>
                    <td className="px-3 py-1.5 align-middle text-right tnum text-fg">{formatarMoeda(conta.totalRecebido)}</td>
                    <td className="px-3 py-1.5 align-middle text-right tnum font-medium text-fg">{formatarMoeda(conta.saldoTotal)}</td>
                    <td className="px-3 py-1.5 align-middle">
                      <Badge tom={tomSituacao(conta.situacao)}>{rotuloSituacao(conta.situacao)}</Badge>
                    </td>
                    <td className="px-2 py-1.5 align-middle">
                      <div className="flex items-center justify-end gap-1">
                        {podeEditar && (
                          <Button variant="ghost" size="icon" aria-label="Editar" onClick={() => onEditar(conta)}>
                            <Pencil />
                          </Button>
                        )}
                        {podeRenegociar && renegociavel && (
                          <Button variant="ghost" size="icon" aria-label="Renegociar" title="Renegociar parcelas" onClick={() => onRenegociar(conta)}>
                            <Handshake />
                          </Button>
                        )}
                        {podeCancelar && cancelavel && (
                          <Button variant="ghost" size="icon" aria-label="Cancelar conta" onClick={() => onCancelar(conta)}>
                            <Ban />
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>

                  {aberta && (
                    <tr>
                      <td colSpan={10} className="bg-surface/50 px-3 py-3 pl-10">
                        <ParcelasTabela
                          conta={conta}
                          parcelasAbertas={parcelasAbertas}
                          alternarParcela={(id) => alternar(parcelasAbertas, setParcelasAbertas, id)}
                          podeReceber={podeReceber}
                          podeEstornar={podeEstornar}
                          onReceber={onReceber}
                          onEstornar={onEstornar}
                        />
                      </td>
                    </tr>
                  )}
                </FragmentConta>
              )
            })}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-end gap-2 text-small text-fg-muted">
        <span className="tnum">
          {total} conta{total === 1 ? '' : 's'} · página {atual} de {totalPaginas}
        </span>
        <Button variant="secondary" size="sm" disabled={atual <= 1} onClick={() => onPagina(atual - 1)}>
          Anterior
        </Button>
        <Button variant="secondary" size="sm" disabled={atual >= totalPaginas} onClick={() => onPagina(atual + 1)}>
          Próxima
        </Button>
      </div>
    </div>
  )
}

/** Wrapper para agrupar as duas <tr> de cada conta sem quebrar o <tbody>. */
function FragmentConta({ children }: { children: React.ReactNode }) {
  return <>{children}</>
}

function ParcelasTabela({
  conta,
  parcelasAbertas,
  alternarParcela,
  podeReceber,
  podeEstornar,
  onReceber,
  onEstornar,
}: {
  conta: ContaReceber
  parcelasAbertas: Set<string>
  alternarParcela: (id: string) => void
  podeReceber: boolean
  podeEstornar: boolean
  onReceber: (conta: ContaReceber, parcela: Parcela) => void
  onEstornar: (conta: ContaReceber, parcela: Parcela, recebimento: Recebimento) => void
}) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-elevated">
      <table className="w-full text-body">
        <thead className="bg-surface">
          <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
            <th className="w-8 px-2 py-1.5" />
            <th className="px-3 py-1.5 text-left">Parcela</th>
            <th className="px-3 py-1.5 text-left">Vencimento</th>
            <th className="px-3 py-1.5 text-right">Valor</th>
            <th className="px-3 py-1.5 text-right">Recebido</th>
            <th className="px-3 py-1.5 text-right">Saldo</th>
            <th className="px-3 py-1.5 text-center">Atraso</th>
            <th className="px-3 py-1.5 text-left">Status</th>
            <th className="px-2 py-1.5" />
          </tr>
        </thead>
        <tbody>
          {conta.parcelas.map((p) => {
            const aberta = parcelasAbertas.has(p.id)
            const podeBaixar = podeReceber && p.saldoPrincipal > 0 && p.status !== 'Cancelada'
            return (
              <FragmentConta key={p.id}>
                <tr className="border-b border-border last:border-0" style={{ height: 38 }}>
                  <td className="px-2 text-center">
                    {p.recebimentos.length > 0 && (
                      <button
                        aria-label={aberta ? 'Recolher' : 'Expandir'}
                        onClick={() => alternarParcela(p.id)}
                        className="text-fg-muted hover:text-fg"
                      >
                        {aberta ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
                      </button>
                    )}
                  </td>
                  <td className="px-3 py-1 align-middle tnum text-fg">
                    {p.numero}/{p.totalParcelas}
                  </td>
                  <td className="px-3 py-1 align-middle tnum text-fg-muted">{formatarData(p.vencimento)}</td>
                  <td className="px-3 py-1 align-middle text-right tnum text-fg">{formatarMoeda(p.valorOriginal)}</td>
                  <td className="px-3 py-1 align-middle text-right tnum text-fg-muted">{formatarMoeda(p.totalPago)}</td>
                  <td className="px-3 py-1 align-middle text-right tnum font-medium text-fg" title={p.juros + p.multa > 0 ? `inclui juros/multa ${formatarMoeda(p.juros + p.multa)}` : undefined}>
                    {formatarMoeda(p.saldoAtualizado)}
                  </td>
                  <td className="px-3 py-1 align-middle text-center tnum text-fg-muted">
                    {p.diasAtraso > 0 ? `${p.diasAtraso}d` : '—'}
                  </td>
                  <td className="px-3 py-1 align-middle">
                    <Badge tom={tomStatus(p.status)}>{rotuloStatus(p.status)}</Badge>
                  </td>
                  <td className="px-2 py-1 align-middle text-right">
                    {podeBaixar && (
                      <Button variant="secondary" size="sm" onClick={() => onReceber(conta, p)}>
                        <HandCoins /> Receber
                      </Button>
                    )}
                  </td>
                </tr>

                {aberta && p.recebimentos.length > 0 && (
                  <tr>
                    <td colSpan={9} className="bg-surface/60 px-3 py-2 pl-10">
                      <RecebimentosTabela
                        parcela={p}
                        podeEstornar={podeEstornar}
                        onEstornar={(rec) => onEstornar(conta, p, rec)}
                      />
                    </td>
                  </tr>
                )}
              </FragmentConta>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function RecebimentosTabela({
  parcela,
  podeEstornar,
  onEstornar,
}: {
  parcela: Parcela
  podeEstornar: boolean
  onEstornar: (rec: Recebimento) => void
}) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-bg">
      <table className="w-full text-small">
        <thead>
          <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
            <th className="px-3 py-1.5 text-left">Data</th>
            <th className="px-3 py-1.5 text-right">Valor</th>
            <th className="px-3 py-1.5 text-right">Juros</th>
            <th className="px-3 py-1.5 text-right">Multa</th>
            <th className="px-3 py-1.5 text-right">Desconto</th>
            <th className="px-3 py-1.5 text-left">Forma</th>
            <th className="px-2 py-1.5" />
          </tr>
        </thead>
        <tbody>
          {parcela.recebimentos.map((r) => (
            <tr key={r.id} className={cn('border-b border-border last:border-0', r.estornado && 'opacity-50')}>
              <td className="px-3 py-1 align-middle tnum text-fg-muted">{formatarData(r.data)}</td>
              <td className={cn('px-3 py-1 align-middle text-right tnum text-fg', r.estornado && 'line-through')}>
                {formatarMoeda(r.valorRecebido)}
              </td>
              <td className="px-3 py-1 align-middle text-right tnum text-fg-muted">{formatarMoeda(r.juros)}</td>
              <td className="px-3 py-1 align-middle text-right tnum text-fg-muted">{formatarMoeda(r.multa)}</td>
              <td className="px-3 py-1 align-middle text-right tnum text-fg-muted">{formatarMoeda(r.desconto)}</td>
              <td className="px-3 py-1 align-middle text-fg-muted">{r.formaPagamentoNome ?? '—'}</td>
              <td className="px-2 py-1 align-middle text-right">
                {r.estornado ? (
                  <Badge tom="neutro">Estornado</Badge>
                ) : (
                  podeEstornar && (
                    <Button variant="ghost" size="sm" onClick={() => onEstornar(r)}>
                      <Undo2 /> Estornar
                    </Button>
                  )
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
