import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { ativarCliente, inativarCliente, listarClientes } from '@/modulos/cadastros/api'
import { ClientesTabela } from '@/modulos/cadastros/clientes-tabela'
import { ClienteDrawer } from '@/modulos/cadastros/cliente-drawer'
import type { ClienteResumo, FiltroClientes } from '@/modulos/cadastros/tipos'

const MESES = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
]

const classeSelect =
  'h-9 rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

export function ClientesPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const qc = useQueryClient()

  const podeCriar = podeVer(sessao, 'cad', 'cad.cliente.criar')
  const podeEditar = podeVer(sessao, 'cad', 'cad.cliente.editar')

  // Draft (o que está nos inputs) vs aplicado (o que alimenta a query).
  const [draft, setDraft] = useState<FiltroClientes>({})
  const [filtro, setFiltro] = useState<FiltroClientes>({})
  const [drawerAberto, setDrawerAberto] = useState(false)
  const [editandoId, setEditandoId] = useState<string | null>(null)

  const lista = useQuery({
    queryKey: ['clientes', filtro],
    queryFn: ({ signal }) => listarClientes(requisitar, filtro, signal),
  })

  const situacao = useMutation({
    mutationFn: (c: ClienteResumo) =>
      c.ativo ? inativarCliente(requisitar, c.id) : ativarCliente(requisitar, c.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['clientes'] }),
  })

  const abrirNovo = () => {
    setEditandoId(null)
    setDrawerAberto(true)
  }
  const abrirEdicao = (id: string) => {
    setEditandoId(id)
    setDrawerAberto(true)
  }
  const alternarSituacao = (c: ClienteResumo) => {
    if (c.ativo && !window.confirm(`Inativar o cliente "${c.nome}"?`)) return
    situacao.mutate(c)
  }

  // Aplica texto no submit; selects aplicam na hora.
  const aplicarTexto = () => setFiltro(draft)
  const mudarSelect = (patch: Partial<FiltroClientes>) => {
    const novo = { ...draft, ...patch }
    setDraft(novo)
    setFiltro(novo)
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-h1 text-fg">Clientes</h1>
          <p className="text-small text-fg-muted">Cadastro de clientes da empresa.</p>
        </div>
        {podeCriar && (
          <Button onClick={abrirNovo}>
            <Plus /> Novo cliente
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
            placeholder="Buscar por nome ou documento…"
            className="pl-9"
          />
        </div>
        <Input
          value={draft.cidade ?? ''}
          onChange={(e) => setDraft({ ...draft, cidade: e.target.value })}
          placeholder="Cidade"
          className="w-36"
        />
        <Input
          value={draft.bairro ?? ''}
          onChange={(e) => setDraft({ ...draft, bairro: e.target.value })}
          placeholder="Bairro"
          className="w-36"
        />
        <select
          className={cn(classeSelect, 'w-32')}
          value={draft.situacao ?? ''}
          onChange={(e) => mudarSelect({ situacao: (e.target.value || undefined) as FiltroClientes['situacao'] })}
        >
          <option value="">Situação</option>
          <option value="ativo">Ativos</option>
          <option value="inativo">Inativos</option>
        </select>
        <select
          className={cn(classeSelect, 'w-40')}
          value={draft.mes ?? ''}
          onChange={(e) => mudarSelect({ mes: e.target.value ? Number(e.target.value) : undefined })}
        >
          <option value="">Aniversário</option>
          {MESES.map((m, i) => (
            <option key={m} value={i + 1}>
              {m}
            </option>
          ))}
        </select>
        <Button type="submit" variant="secondary">
          Filtrar
        </Button>
      </form>

      <ClientesTabela
        dados={lista.data ?? []}
        carregando={lista.isPending}
        podeEditar={podeEditar}
        onEditar={abrirEdicao}
        onAlternarSituacao={alternarSituacao}
      />

      <ClienteDrawer aberto={drawerAberto} onAbrir={setDrawerAberto} clienteId={editandoId} />
    </div>
  )
}
