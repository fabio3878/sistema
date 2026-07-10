import type { ServicoResumo, Servico, ServicoEntrada, FiltroServicos } from './tipos'

/** Fetch autenticado exposto por `useAuth().requisitar` (bearer + refresh automático). */
export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

export function listarServicos(req: Requisitar, filtro: FiltroServicos, signal?: AbortSignal) {
  const p = new URLSearchParams()
  if (filtro.busca) p.set('busca', filtro.busca)
  if (filtro.situacao) p.set('situacao', filtro.situacao)
  const qs = p.toString()
  return req<ServicoResumo[]>(`/cad/servicos${qs ? `?${qs}` : ''}`, { signal })
}

export function obterServico(req: Requisitar, id: string, signal?: AbortSignal) {
  return req<Servico>(`/cad/servicos/${id}`, { signal })
}

export function criarServico(req: Requisitar, dados: ServicoEntrada) {
  return req<{ id: string }>('/cad/servicos', { method: 'POST', body: dados })
}

export function atualizarServico(req: Requisitar, id: string, dados: ServicoEntrada) {
  return req<void>(`/cad/servicos/${id}`, { method: 'PUT', body: dados })
}

export function ativarServico(req: Requisitar, id: string) {
  return req<void>(`/cad/servicos/${id}/ativar`, { method: 'POST' })
}

export function inativarServico(req: Requisitar, id: string) {
  return req<void>(`/cad/servicos/${id}/inativar`, { method: 'POST' })
}
