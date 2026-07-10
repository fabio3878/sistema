import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { ativarProduto, inativarProduto, listarProdutos } from '@/modulos/estoque/api'
import { ProdutosTabela } from '@/modulos/estoque/produtos-tabela'
import { ProdutoDrawer } from '@/modulos/estoque/produto-drawer'
import type { ProdutoResumo, FiltroProdutos } from '@/modulos/estoque/tipos'

const classeSelect =
  'h-9 rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

export function ProdutosPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const qc = useQueryClient()

  const podeCriar = podeVer(sessao, 'est', 'est.produto.criar')
  const podeEditar = podeVer(sessao, 'est', 'est.produto.editar')

  // Draft (o que está nos inputs) vs aplicado (o que alimenta a query).
  const [draft, setDraft] = useState<FiltroProdutos>({})
  const [filtro, setFiltro] = useState<FiltroProdutos>({})
  const [drawerAberto, setDrawerAberto] = useState(false)
  const [editandoId, setEditandoId] = useState<string | null>(null)

  const lista = useQuery({
    queryKey: ['produtos', filtro],
    queryFn: ({ signal }) => listarProdutos(requisitar, filtro, signal),
  })

  const situacao = useMutation({
    mutationFn: (p: ProdutoResumo) =>
      p.ativo ? inativarProduto(requisitar, p.id) : ativarProduto(requisitar, p.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['produtos'] }),
  })

  const abrirNovo = () => {
    setEditandoId(null)
    setDrawerAberto(true)
  }
  const abrirEdicao = (id: string) => {
    setEditandoId(id)
    setDrawerAberto(true)
  }
  const alternarSituacao = (p: ProdutoResumo) => {
    if (p.ativo && !window.confirm(`Inativar o produto "${p.descricao}"?`)) return
    situacao.mutate(p)
  }

  // Aplica texto no submit; selects aplicam na hora.
  const aplicarTexto = () => setFiltro(draft)
  const mudarSelect = (patch: Partial<FiltroProdutos>) => {
    const novo = { ...draft, ...patch }
    setDraft(novo)
    setFiltro(novo)
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-h1 text-fg">Produtos</h1>
          <p className="text-small text-fg-muted">Cadastro de produtos (mercadorias) da empresa.</p>
        </div>
        {podeCriar && (
          <Button onClick={abrirNovo}>
            <Plus /> Novo produto
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
            placeholder="Buscar por código, descrição ou código de barras…"
            className="pl-9"
          />
        </div>
        <select
          className={cn(classeSelect, 'w-32')}
          value={draft.situacao ?? ''}
          onChange={(e) => mudarSelect({ situacao: (e.target.value || undefined) as FiltroProdutos['situacao'] })}
        >
          <option value="">Situação</option>
          <option value="ativo">Ativos</option>
          <option value="inativo">Inativos</option>
        </select>
        <Button type="submit" variant="secondary">
          Filtrar
        </Button>
      </form>

      <ProdutosTabela
        dados={lista.data ?? []}
        carregando={lista.isPending}
        podeEditar={podeEditar}
        onEditar={abrirEdicao}
        onAlternarSituacao={alternarSituacao}
      />

      <ProdutoDrawer aberto={drawerAberto} onAbrir={setDrawerAberto} produtoId={editandoId} />
    </div>
  )
}
