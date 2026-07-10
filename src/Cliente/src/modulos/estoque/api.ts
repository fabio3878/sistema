import type { ProdutoResumo, Produto, ProdutoEntrada, FiltroProdutos } from './tipos'

/** Fetch autenticado exposto por `useAuth().requisitar` (bearer + refresh automático). */
export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

export function listarProdutos(req: Requisitar, filtro: FiltroProdutos, signal?: AbortSignal) {
  const p = new URLSearchParams()
  if (filtro.busca) p.set('busca', filtro.busca)
  if (filtro.situacao) p.set('situacao', filtro.situacao)
  const qs = p.toString()
  return req<ProdutoResumo[]>(`/cad/produtos${qs ? `?${qs}` : ''}`, { signal })
}

export function obterProduto(req: Requisitar, id: string, signal?: AbortSignal) {
  return req<Produto>(`/cad/produtos/${id}`, { signal })
}

export function criarProduto(req: Requisitar, dados: ProdutoEntrada) {
  return req<{ id: string }>('/cad/produtos', { method: 'POST', body: dados })
}

export function atualizarProduto(req: Requisitar, id: string, dados: ProdutoEntrada) {
  return req<void>(`/cad/produtos/${id}`, { method: 'PUT', body: dados })
}

export function ativarProduto(req: Requisitar, id: string) {
  return req<void>(`/cad/produtos/${id}/ativar`, { method: 'POST' })
}

export function inativarProduto(req: Requisitar, id: string) {
  return req<void>(`/cad/produtos/${id}/inativar`, { method: 'POST' })
}
