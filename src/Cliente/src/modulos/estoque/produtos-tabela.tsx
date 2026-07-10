import { useMemo, useState } from 'react'
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type SortingState,
} from '@tanstack/react-table'
import { ArrowUpDown, Pencil, Power, PowerOff } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import type { ProdutoResumo } from './tipos'

interface Props {
  dados: ProdutoResumo[]
  carregando: boolean
  podeEditar: boolean
  onEditar: (id: string) => void
  onAlternarSituacao: (p: ProdutoResumo) => void
}

const col = createColumnHelper<ProdutoResumo>()

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function ProdutosTabela({ dados, carregando, podeEditar, onEditar, onAlternarSituacao }: Props) {
  const [sorting, setSorting] = useState<SortingState>([{ id: 'descricao', desc: false }])

  const colunas = useMemo(
    () => [
      col.accessor('codigoInterno', {
        header: 'Código',
        cell: (c) => <span className="tnum font-medium text-fg">{c.getValue() ?? '—'}</span>,
      }),
      col.accessor('descricao', {
        header: 'Descrição',
        cell: (c) => <span className="text-fg">{c.getValue()}</span>,
      }),
      col.accessor('unidade', {
        header: 'Un.',
        cell: (c) => <span className="tnum text-fg-muted">{c.getValue()}</span>,
      }),
      col.accessor('precoVenda', {
        header: 'Preço',
        cell: (c) => <span className="tnum text-fg">{moeda.format(c.getValue())}</span>,
      }),
      col.accessor('ativo', {
        header: 'Situação',
        cell: (c) =>
          c.getValue() ? <Badge tom="success">Ativo</Badge> : <Badge tom="neutro">Inativo</Badge>,
      }),
      col.display({
        id: 'acoes',
        header: '',
        cell: ({ row }) =>
          podeEditar ? (
            <div className="flex justify-end gap-1">
              <Button
                variant="ghost"
                size="icon"
                aria-label="Editar"
                onClick={(e) => {
                  e.stopPropagation()
                  onEditar(row.original.id)
                }}
              >
                <Pencil />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                aria-label={row.original.ativo ? 'Inativar' : 'Ativar'}
                onClick={(e) => {
                  e.stopPropagation()
                  onAlternarSituacao(row.original)
                }}
              >
                {row.original.ativo ? <PowerOff /> : <Power />}
              </Button>
            </div>
          ) : null,
      }),
    ],
    [podeEditar, onEditar, onAlternarSituacao],
  )

  const tabela = useReactTable({
    data: dados,
    columns: colunas,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize: 20 } },
  })

  if (carregando) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-md bg-surface" />
        ))}
      </div>
    )
  }

  if (dados.length === 0) {
    return (
      <div className="grid place-items-center rounded-lg border border-dashed border-border py-16 text-fg-muted">
        Nenhum produto encontrado.
      </div>
    )
  }

  return (
    <div className="space-y-3">
      <div className="overflow-hidden rounded-lg border border-border">
        <table className="w-full text-body">
          <thead className="bg-surface">
            {tabela.getHeaderGroups().map((grupo) => (
              <tr key={grupo.id} className="border-b border-border">
                {grupo.headers.map((h) => {
                  const podeOrdenar = h.column.getCanSort()
                  return (
                    <th
                      key={h.id}
                      className="px-3 py-2 text-left text-caption font-semibold uppercase tracking-wide text-fg-muted"
                    >
                      {podeOrdenar ? (
                        <button
                          className="inline-flex items-center gap-1 hover:text-fg"
                          onClick={h.column.getToggleSortingHandler()}
                        >
                          {flexRender(h.column.columnDef.header, h.getContext())}
                          <ArrowUpDown className="size-3" />
                        </button>
                      ) : (
                        flexRender(h.column.columnDef.header, h.getContext())
                      )}
                    </th>
                  )
                })}
              </tr>
            ))}
          </thead>
          <tbody>
            {tabela.getRowModel().rows.map((row) => (
              <tr
                key={row.id}
                onClick={() => podeEditar && onEditar(row.original.id)}
                className="border-b border-border last:border-0 hover:bg-black/[0.02] dark:hover:bg-white/[0.03]"
                style={{ height: 40, cursor: podeEditar ? 'pointer' : 'default' }}
              >
                {row.getVisibleCells().map((cell) => (
                  <td key={cell.id} className="px-3 py-1.5 align-middle">
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {tabela.getPageCount() > 1 && (
        <div className="flex items-center justify-end gap-2 text-small text-fg-muted">
          <span className="tnum">
            Página {tabela.getState().pagination.pageIndex + 1} de {tabela.getPageCount()}
          </span>
          <Button
            variant="secondary"
            size="sm"
            disabled={!tabela.getCanPreviousPage()}
            onClick={() => tabela.previousPage()}
          >
            Anterior
          </Button>
          <Button
            variant="secondary"
            size="sm"
            disabled={!tabela.getCanNextPage()}
            onClick={() => tabela.nextPage()}
          >
            Próxima
          </Button>
        </div>
      )}
    </div>
  )
}
