import { useEffect, useMemo, useRef, useState } from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import * as Popover from '@radix-ui/react-popover'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnSizingState,
  type SortingState,
  type VisibilityState,
} from '@tanstack/react-table'
import { ArrowUpDown, Columns3, RotateCcw, Search, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useJanelaArrastavel } from '@/lib/use-janela-arrastavel'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'

/** Definição de uma coluna do seletor. `valor` habilita ordenação; `render` desenha a célula. */
export interface ColunaSeletor<T> {
  chave: string
  titulo: string
  render: (row: T) => React.ReactNode
  valor?: (row: T) => string | number
  visivelPadrao?: boolean
  ordenavel?: boolean
  alinhar?: 'left' | 'right'
  /** Largura inicial da coluna em px (o usuário pode redimensionar arrastando; a escolha é salva). */
  largura?: number
}

interface Props<T> {
  aberto: boolean
  onAbrir: (v: boolean) => void
  titulo: string
  colunas: ColunaSeletor<T>[]
  buscar: (termo: string, signal?: AbortSignal) => Promise<T[]>
  getId: (row: T) => string
  onSelecionar: (row: T) => void
  /** Chave do localStorage p/ persistir a visibilidade das colunas (uma por entidade). */
  storageKey: string
  placeholderBusca?: string
  /**
   * Chamado quando a janela fecha, no momento em que o Radix devolveria o foco (`onCloseAutoFocus`).
   * `selecionou` = fechou por ter escolhido um item. O caller controla o foco (o padrão do Radix é
   * suprimido), então dá para avançar para o próximo campo ao selecionar ou voltar ao gatilho ao cancelar.
   */
  aoFechar?: (selecionou: boolean) => void
}

/**
 * Array vazio com identidade ESTÁVEL para o `data` da tabela. Passar `consulta.data ?? []` inline cria
 * um `[]` novo a cada render enquanto a query está sem dados → o row-model do TanStack recomputa e
 * dispara `autoResetPageIndex` num loop render↔microtask que trava a aba. Uma referência fixa evita isso.
 */
const VAZIO: never[] = []

/** Visibilidade padrão a partir das colunas (colunas sem `visivelPadrao` começam ocultas). */
function padrao<T>(colunas: ColunaSeletor<T>[]): VisibilityState {
  return Object.fromEntries(colunas.map((c) => [c.chave, !!c.visivelPadrao]))
}

function lerVisibilidade<T>(storageKey: string, colunas: ColunaSeletor<T>[]): VisibilityState {
  try {
    const bruto = localStorage.getItem(storageKey)
    if (!bruto) return padrao(colunas)
    const salvo = JSON.parse(bruto) as VisibilityState
    // Mescla com o padrão para tolerar colunas novas/removidas entre versões.
    return { ...padrao(colunas), ...salvo }
  } catch {
    return padrao(colunas)
  }
}

/** Larguras salvas por coluna (px). Vazio = usa o tamanho inicial (`size`) de cada coluna. */
function lerLarguras(storageKey: string): ColumnSizingState {
  try {
    const bruto = localStorage.getItem(`${storageKey}.larguras`)
    return bruto ? (JSON.parse(bruto) as ColumnSizingState) : {}
  } catch {
    return {}
  }
}

/**
 * Seletor de registro em grade (janela de busca "lookup"). Genérico e reutilizável: recebe colunas,
 * a função de busca (server-side) e devolve o item escolhido. As colunas visíveis são configuráveis
 * (engrenagem) e persistem por `storageKey` no localStorage. Fecha no Esc/overlay/X.
 */
export function SeletorRegistro<T>({
  aberto,
  onAbrir,
  titulo,
  colunas,
  buscar,
  getId,
  onSelecionar,
  storageKey,
  placeholderBusca = 'Buscar…',
  aoFechar,
}: Props<T>) {
  const [termo, setTermo] = useState('')
  const [termoBusca, setTermoBusca] = useState('')
  const [sorting, setSorting] = useState<SortingState>([])
  const [visibilidade, setVisibilidade] = useState<VisibilityState>(() => lerVisibilidade(storageKey, colunas))
  const [larguras, setLarguras] = useState<ColumnSizingState>(() => lerLarguras(storageKey))
  // Célula ativa (navegação por teclado estilo grid): índices na página atual.
  const [cursor, setCursor] = useState({ r: 0, c: 0 })
  const cursorRef = useRef(cursor)
  cursorRef.current = cursor
  const inputRef = useRef<HTMLInputElement>(null)
  const selecionouRef = useRef(false)
  const { alvoRef, estilo, propsBarra } = useJanelaArrastavel(aberto)

  // Debounce da busca (300ms).
  useEffect(() => {
    const t = setTimeout(() => setTermoBusca(termo), 300)
    return () => clearTimeout(t)
  }, [termo])

  // Ao abrir, limpa a busca anterior.
  useEffect(() => {
    if (aberto) {
      setTermo('')
      setTermoBusca('')
    }
  }, [aberto])

  // Persiste a escolha de colunas.
  useEffect(() => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(visibilidade))
    } catch {
      /* ignore quota */
    }
  }, [storageKey, visibilidade])

  // Persiste as larguras ajustadas (arrasto).
  useEffect(() => {
    try {
      localStorage.setItem(`${storageKey}.larguras`, JSON.stringify(larguras))
    } catch {
      /* ignore quota */
    }
  }, [storageKey, larguras])

  const consulta = useQuery({
    queryKey: ['seletor', storageKey, termoBusca],
    queryFn: ({ signal }) => buscar(termoBusca, signal),
    enabled: aberto,
    // Mantém a lista anterior enquanto refaz a busca: sem isso, `data` vira `undefined` a cada troca de
    // termo e o `?? VAZIO` reabriria a janela do loop do autoResetPageIndex (além de piscar skeleton).
    placeholderData: keepPreviousData,
  })

  // Reabrir sempre refaz a busca — limpa um erro/estale de quando o host estava fora do ar.
  useEffect(() => {
    if (aberto) consulta.refetch()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [aberto])

  // Volta o cursor para a 1ª célula quando os resultados mudam (nova busca) ou ao (re)abrir.
  useEffect(() => {
    setCursor({ r: 0, c: 0 })
  }, [termoBusca, aberto])

  // Mantém a célula ativa visível ao navegar (vertical e horizontal).
  useEffect(() => {
    if (!aberto) return
    const el = document.querySelector('[data-seletor-cursor="1"]')
    el?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
  }, [cursor, aberto])

  const col = useMemo(() => createColumnHelper<T>(), [])
  const colunasTabela = useMemo(
    () =>
      colunas.map((c) =>
        col.accessor((row: T) => (c.valor ? c.valor(row) : ''), {
          id: c.chave,
          header: c.titulo,
          enableSorting: !!c.ordenavel,
          size: c.largura ?? 160,
          minSize: 60,
          cell: ({ row }) => c.render(row.original),
          meta: { alinhar: c.alinhar ?? 'left' },
        }),
      ),
    [col, colunas],
  )

  const tabela = useReactTable({
    data: consulta.data ?? VAZIO,
    columns: colunasTabela,
    state: { sorting, columnVisibility: visibilidade, columnSizing: larguras },
    onSortingChange: setSorting,
    onColumnVisibilityChange: setVisibilidade,
    onColumnSizingChange: setLarguras,
    enableColumnResizing: true,
    columnResizeMode: 'onChange',
    getRowId: (row) => getId(row),
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    initialState: { pagination: { pageSize: 8 } },
  })

  const escolher = (row: T) => {
    selecionouRef.current = true
    onSelecionar(row)
    onAbrir(false)
  }

  // Navegação por CÉLULA (setas) + Enter seleciona a linha do cursor. Setas dão preventDefault para não
  // mexer no cursor de texto da busca; letras/Backspace continuam indo para o campo de busca.
  useEffect(() => {
    if (!aberto) return
    const onKey = (e: KeyboardEvent) => {
      const linhas = tabela.getRowModel().rows
      const nLinhas = linhas.length
      const nColunas = tabela.getVisibleLeafColumns().length
      // Type-ahead: com o foco no grid (fora da busca), digitar alimenta o campo de busca.
      const noBusca = document.activeElement === inputRef.current
      if (!noBusca) {
        if (e.key === 'Backspace') {
          e.preventDefault()
          inputRef.current?.focus()
          setTermo((t) => t.slice(0, -1))
          return
        }
        if (e.key.length === 1 && !e.ctrlKey && !e.metaKey && !e.altKey) {
          e.preventDefault()
          inputRef.current?.focus()
          setTermo((t) => t + e.key)
          return
        }
      }
      if (e.key === 'Enter') {
        const linha = linhas[cursorRef.current.r]
        if (linha) {
          e.preventDefault()
          escolher(linha.original)
        }
        return
      }
      if (nLinhas === 0) return
      if (e.key === 'ArrowDown') {
        e.preventDefault()
        // No fim da página, avança para a próxima (cursor no topo); senão desce uma linha.
        if (cursorRef.current.r >= nLinhas - 1 && tabela.getCanNextPage()) {
          tabela.nextPage()
          setCursor((p) => ({ ...p, r: 0 }))
        } else {
          setCursor((p) => ({ ...p, r: Math.min(p.r + 1, nLinhas - 1) }))
        }
      } else if (e.key === 'ArrowUp') {
        e.preventDefault()
        // No topo da página, volta para a anterior (cursor na última linha); senão sobe uma linha.
        if (cursorRef.current.r <= 0 && tabela.getCanPreviousPage()) {
          tabela.previousPage()
          setCursor((p) => ({ ...p, r: tabela.getState().pagination.pageSize - 1 }))
        } else {
          setCursor((p) => ({ ...p, r: Math.max(p.r - 1, 0) }))
        }
      } else if (e.key === 'ArrowRight') {
        e.preventDefault()
        setCursor((p) => ({ ...p, c: Math.min(p.c + 1, nColunas - 1) }))
      } else if (e.key === 'ArrowLeft') {
        e.preventDefault()
        setCursor((p) => ({ ...p, c: Math.max(p.c - 1, 0) }))
      }
    }
    document.addEventListener('keydown', onKey, true)
    return () => document.removeEventListener('keydown', onKey, true)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [aberto, tabela])

  // Janela modal PRÓPRIA (Radix Dialog, portal para o body): fica no topo da pilha de focus-scopes do
  // Radix, então PAUSA o trap do drawer — sem disputa de foco. Um único overlay limpo cobre tudo, e
  // Popover/Select DENTRO do seletor funcionam (viram o topo ao abrir). Esc/overlay fecham só o seletor.
  return (
    <Dialog.Root open={aberto} onOpenChange={onAbrir}>
      <Dialog.Portal>
        <Dialog.Overlay className="ac-overlay-in fixed inset-0 z-[70] bg-black/40" />
        <Dialog.Content
          ref={alvoRef}
          style={estilo}
          aria-describedby={undefined}
          // Ao abrir, foca o campo de busca (digitar já filtra) em vez do 1º elemento tabulável.
          onOpenAutoFocus={(e) => {
            e.preventDefault()
            inputRef.current?.focus()
          }}
          // Ao fechar, o caller decide o foco (avança se selecionou; volta ao gatilho se cancelou).
          onCloseAutoFocus={(e) => {
            if (!aoFechar) return
            e.preventDefault()
            aoFechar(selecionouRef.current)
            selecionouRef.current = false
          }}
          className="ac-modal-in fixed left-1/2 top-1/2 z-[80] flex max-h-[85vh] w-[92vw] max-w-4xl flex-col rounded-xl border border-border bg-elevated shadow-drawer focus:outline-none"
        >
          <div {...propsBarra} className="flex cursor-move select-none items-center justify-between gap-4 border-b border-border px-5 py-3.5">
            <Dialog.Title className="text-h3 text-fg">{titulo}</Dialog.Title>
            <Dialog.Close asChild>
              <Button variant="ghost" size="icon" aria-label="Fechar">
                <X />
              </Button>
            </Dialog.Close>
          </div>

          {/* Busca + engrenagem de colunas. */}
          <div className="flex items-center gap-2 px-5 py-3">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-fg-muted" />
              <Input ref={inputRef} value={termo} onChange={(e) => setTermo(e.target.value)} placeholder={placeholderBusca} className="pl-9" />
            </div>
            <SeletorColunas
              tabela={tabela}
              onRestaurar={() => {
                setVisibilidade(padrao(colunas))
                setLarguras({})
              }}
            />
          </div>

          {/* Grade — altura fixa proporcional à tela; rola vertical E horizontal DENTRO do grid. */}
          <div className="px-5 pb-2">
            <div className="h-[55vh] overflow-auto rounded-lg border border-border">
              <table className="table-fixed text-body" style={{ width: tabela.getTotalSize() }}>
                <thead className="sticky top-0 z-10 bg-surface">
                  {tabela.getHeaderGroups().map((grupo) => (
                    <tr key={grupo.id} className="border-b border-border">
                      {grupo.headers.map((h) => {
                        const meta = h.column.columnDef.meta as { alinhar?: string } | undefined
                        return (
                          <th
                            key={h.id}
                            style={{ width: h.getSize() }}
                            className={cn(
                              'relative border-r border-border px-3 py-2 text-caption font-semibold uppercase tracking-wide text-fg-muted last:border-r-0',
                              meta?.alinhar === 'right' ? 'text-right' : 'text-left',
                            )}
                          >
                            {h.column.getCanSort() ? (
                              <button className="inline-flex max-w-full items-center gap-1 truncate hover:text-fg" onClick={h.column.getToggleSortingHandler()}>
                                <span className="truncate">{flexRender(h.column.columnDef.header, h.getContext())}</span>
                                <ArrowUpDown className="size-3 shrink-0" />
                              </button>
                            ) : (
                              flexRender(h.column.columnDef.header, h.getContext())
                            )}
                            {h.column.getCanResize() && (
                              <div
                                onMouseDown={h.getResizeHandler()}
                                onTouchStart={h.getResizeHandler()}
                                onClick={(e) => e.stopPropagation()}
                                className={cn(
                                  'absolute right-0 top-0 h-full w-1.5 cursor-col-resize touch-none select-none hover:bg-primary/40',
                                  h.column.getIsResizing() && 'bg-primary',
                                )}
                                aria-hidden
                              />
                            )}
                          </th>
                        )
                      })}
                    </tr>
                  ))}
                </thead>
                <tbody>
                  {consulta.isPending ? (
                    <tr>
                      <td colSpan={tabela.getVisibleLeafColumns().length} className="px-3 py-8">
                        <div className="space-y-2">
                          {Array.from({ length: 5 }).map((_, i) => (
                            <div key={i} className="h-8 animate-pulse rounded bg-surface" />
                          ))}
                        </div>
                      </td>
                    </tr>
                  ) : consulta.isError ? (
                    <tr>
                      <td colSpan={tabela.getVisibleLeafColumns().length} className="px-3 py-12 text-center">
                        <p className="text-fg-muted">Não foi possível carregar os registros.</p>
                        <Button variant="secondary" size="sm" className="mt-3" onClick={() => consulta.refetch()}>
                          <RotateCcw /> Tentar novamente
                        </Button>
                      </td>
                    </tr>
                  ) : tabela.getRowModel().rows.length === 0 ? (
                    <tr>
                      <td colSpan={tabela.getVisibleLeafColumns().length} className="px-3 py-12 text-center text-fg-muted">
                        Nenhum registro encontrado.
                      </td>
                    </tr>
                  ) : (
                    tabela.getRowModel().rows.map((row, ri) => (
                      <tr
                        key={row.id}
                        onClick={() => escolher(row.original)}
                        className={cn(
                          'cursor-pointer border-b border-border last:border-0 even:bg-black/[0.02] dark:even:bg-white/[0.03]',
                          ri === cursor.r ? 'bg-primary/5 dark:bg-primary/10' : 'hover:bg-primary/5',
                        )}
                        style={{ height: 38 }}
                      >
                        {row.getVisibleCells().map((cell, ci) => {
                          const meta = cell.column.columnDef.meta as { alinhar?: string } | undefined
                          const ativa = ri === cursor.r && ci === cursor.c
                          return (
                            <td
                              key={cell.id}
                              data-seletor-cursor={ativa ? '1' : undefined}
                              style={{ width: cell.column.getSize() }}
                              className={cn(
                                'truncate border-r border-border px-3 py-1.5 align-middle last:border-r-0',
                                meta?.alinhar === 'right' && 'text-right',
                                ativa && 'rounded-sm ring-2 ring-inset ring-primary',
                              )}
                            >
                              {flexRender(cell.column.columnDef.cell, cell.getContext())}
                            </td>
                          )
                        })}
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Rodapé: contagem + paginação. min-h reserva a altura da paginação (evita "pulo" ao filtrar). */}
          <div className="flex min-h-14 items-center justify-between gap-2 border-t border-border px-5 py-3 text-small text-fg-muted">
            <span className="tnum">{consulta.data?.length ?? 0} registro(s)</span>
            {tabela.getPageCount() > 1 && (
              <div className="flex items-center gap-2">
                <span className="tnum">
                  Página {tabela.getState().pagination.pageIndex + 1} de {tabela.getPageCount()}
                </span>
                <Button variant="secondary" size="sm" disabled={!tabela.getCanPreviousPage()} onClick={() => tabela.previousPage()}>
                  Anterior
                </Button>
                <Button variant="secondary" size="sm" disabled={!tabela.getCanNextPage()} onClick={() => tabela.nextPage()}>
                  Próxima
                </Button>
              </div>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

/** Engrenagem "Colunas": liga/desliga colunas visíveis (persistido pelo pai). */
function SeletorColunas<T>({ tabela, onRestaurar }: { tabela: ReturnType<typeof useReactTable<T>>; onRestaurar: () => void }) {
  const colunas = tabela.getAllLeafColumns().filter((c) => c.getCanHide())
  return (
    <Popover.Root>
      <Popover.Trigger asChild>
        <Button variant="secondary" size="sm">
          <Columns3 /> Colunas
        </Button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={4}
          className="z-[90] w-56 rounded-md border border-border bg-elevated p-2 shadow-popover"
        >
          <div className="mb-1 px-1 text-caption font-semibold uppercase tracking-wide text-fg-muted">Colunas visíveis</div>
          <div className="space-y-1">
            {colunas.map((c) => (
              <Checkbox
                key={c.id}
                checked={c.getIsVisible()}
                onChange={() => c.toggleVisibility()}
                label={String(c.columnDef.header)}
                className="rounded px-1 py-1 hover:bg-black/[0.03] dark:hover:bg-white/[0.04]"
              />
            ))}
          </div>
          <button onClick={onRestaurar} className="mt-2 flex w-full items-center gap-1.5 rounded px-1 py-1 text-small text-fg-muted hover:text-fg">
            <RotateCcw className="size-3.5" /> Restaurar padrão
          </button>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  )
}
