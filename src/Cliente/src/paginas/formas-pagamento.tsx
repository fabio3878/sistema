import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Pencil } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { ativarForma, inativarForma, listarFormas } from '@/modulos/financeiro/api'
import { FormaDrawer } from '@/modulos/financeiro/forma-drawer'
import type { FormaPagamento } from '@/modulos/financeiro/tipos'

export function FormasPagamentoPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const qc = useQueryClient()

  const podeCriar = podeVer(sessao, 'fin', 'fin.formapagamento.criar')
  const podeEditar = podeVer(sessao, 'fin', 'fin.formapagamento.editar')

  const [drawer, setDrawer] = useState(false)
  const [editando, setEditando] = useState<FormaPagamento | null>(null)

  const lista = useQuery({
    queryKey: ['formas'],
    queryFn: ({ signal }) => listarFormas(requisitar, undefined, signal),
  })

  const situacao = useMutation({
    mutationFn: (f: FormaPagamento) => (f.ativo ? inativarForma(requisitar, f.id) : ativarForma(requisitar, f.id)),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['formas'] })
      qc.invalidateQueries({ queryKey: ['formas-combo'] })
    },
  })

  const abrirNovo = () => {
    setEditando(null)
    setDrawer(true)
  }
  const abrirEdicao = (f: FormaPagamento) => {
    setEditando(f)
    setDrawer(true)
  }

  const itens = lista.data ?? []

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-h1 text-fg">Formas de pagamento</h1>
          <p className="text-small text-fg-muted">Meios de recebimento usados nas baixas de parcelas.</p>
        </div>
        {podeCriar && (
          <Button onClick={abrirNovo}>
            <Plus /> Nova forma
          </Button>
        )}
      </div>

      {lista.isPending ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-10 animate-pulse rounded-md bg-surface" />
          ))}
        </div>
      ) : itens.length === 0 ? (
        <div className="grid place-items-center rounded-lg border border-dashed border-border py-16 text-fg-muted">
          Nenhuma forma de pagamento cadastrada.
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-body">
            <thead className="bg-surface">
              <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
                <th className="px-3 py-2 text-left">Nome</th>
                <th className="px-3 py-2 text-left">Situação</th>
                <th className="px-2 py-2" />
              </tr>
            </thead>
            <tbody>
              {itens.map((f) => (
                <tr key={f.id} className="border-b border-border last:border-0" style={{ height: 40 }}>
                  <td className="px-3 py-1.5 align-middle text-fg">{f.nome}</td>
                  <td className="px-3 py-1.5 align-middle">
                    <Badge tom={f.ativo ? 'success' : 'neutro'}>{f.ativo ? 'Ativa' : 'Inativa'}</Badge>
                  </td>
                  <td className="px-2 py-1.5 align-middle">
                    <div className="flex items-center justify-end gap-1">
                      {podeEditar && (
                        <>
                          <Button variant="ghost" size="icon" aria-label="Editar" onClick={() => abrirEdicao(f)}>
                            <Pencil />
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => situacao.mutate(f)}>
                            {f.ativo ? 'Inativar' : 'Ativar'}
                          </Button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <FormaDrawer aberto={drawer} onAbrir={setDrawer} forma={editando} />
    </div>
  )
}
