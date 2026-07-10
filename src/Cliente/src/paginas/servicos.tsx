import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { ativarServico, inativarServico, listarServicos } from '@/modulos/servicos/api'
import { ServicosTabela } from '@/modulos/servicos/servicos-tabela'
import { ServicoDrawer } from '@/modulos/servicos/servico-drawer'
import type { ServicoResumo, FiltroServicos } from '@/modulos/servicos/tipos'

const classeSelect =
  'h-9 rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

export function ServicosPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const qc = useQueryClient()

  const podeCriar = podeVer(sessao, 'cad', 'cad.servico.criar')
  const podeEditar = podeVer(sessao, 'cad', 'cad.servico.editar')

  const [draft, setDraft] = useState<FiltroServicos>({})
  const [filtro, setFiltro] = useState<FiltroServicos>({})
  const [drawerAberto, setDrawerAberto] = useState(false)
  const [editandoId, setEditandoId] = useState<string | null>(null)

  const lista = useQuery({
    queryKey: ['servicos', filtro],
    queryFn: ({ signal }) => listarServicos(requisitar, filtro, signal),
  })

  const situacao = useMutation({
    mutationFn: (s: ServicoResumo) =>
      s.ativo ? inativarServico(requisitar, s.id) : ativarServico(requisitar, s.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['servicos'] }),
  })

  const abrirNovo = () => {
    setEditandoId(null)
    setDrawerAberto(true)
  }
  const abrirEdicao = (id: string) => {
    setEditandoId(id)
    setDrawerAberto(true)
  }
  const alternarSituacao = (s: ServicoResumo) => {
    if (s.ativo && !window.confirm(`Inativar o serviço "${s.descricao}"?`)) return
    situacao.mutate(s)
  }

  const aplicarTexto = () => setFiltro(draft)
  const mudarSelect = (patch: Partial<FiltroServicos>) => {
    const novo = { ...draft, ...patch }
    setDraft(novo)
    setFiltro(novo)
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-h1 text-fg">Serviços</h1>
          <p className="text-small text-fg-muted">Cadastro de serviços da empresa.</p>
        </div>
        {podeCriar && (
          <Button onClick={abrirNovo}>
            <Plus /> Novo serviço
          </Button>
        )}
      </div>

      <form
        className="flex flex-wrap items-center gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          aplicarTexto()
        }}
      >
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-fg-muted" />
          <Input
            value={draft.busca ?? ''}
            onChange={(e) => setDraft({ ...draft, busca: e.target.value })}
            placeholder="Buscar por código ou descrição…"
            className="pl-9"
          />
        </div>
        <select
          className={cn(classeSelect, 'w-32')}
          value={draft.situacao ?? ''}
          onChange={(e) => mudarSelect({ situacao: (e.target.value || undefined) as FiltroServicos['situacao'] })}
        >
          <option value="">Situação</option>
          <option value="ativo">Ativos</option>
          <option value="inativo">Inativos</option>
        </select>
        <Button type="submit" variant="secondary">
          Filtrar
        </Button>
      </form>

      <ServicosTabela
        dados={lista.data ?? []}
        carregando={lista.isPending}
        podeEditar={podeEditar}
        onEditar={abrirEdicao}
        onAlternarSituacao={alternarSituacao}
      />

      <ServicoDrawer aberto={drawerAberto} onAbrir={setDrawerAberto} servicoId={editandoId} />
    </div>
  )
}
