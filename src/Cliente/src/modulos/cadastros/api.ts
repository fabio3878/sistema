import type { ClienteResumo, Cliente, ClienteEntrada, Estado, Municipio, FiltroClientes } from './tipos'

/** Fetch autenticado exposto por `useAuth().requisitar` (bearer + refresh automático). */
export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

export function listarClientes(req: Requisitar, filtro: FiltroClientes, signal?: AbortSignal) {
  const p = new URLSearchParams()
  if (filtro.busca) p.set('busca', filtro.busca)
  if (filtro.cidade) p.set('cidade', filtro.cidade)
  if (filtro.bairro) p.set('bairro', filtro.bairro)
  if (filtro.situacao) p.set('situacao', filtro.situacao)
  if (filtro.mes) p.set('mes', String(filtro.mes))
  const qs = p.toString()
  return req<ClienteResumo[]>(`/cad/clientes${qs ? `?${qs}` : ''}`, { signal })
}

export function obterCliente(req: Requisitar, id: string, signal?: AbortSignal) {
  return req<Cliente>(`/cad/clientes/${id}`, { signal })
}

export function criarCliente(req: Requisitar, dados: ClienteEntrada) {
  return req<{ id: string }>('/cad/clientes', { method: 'POST', body: dados })
}

export function atualizarCliente(req: Requisitar, id: string, dados: ClienteEntrada) {
  return req<void>(`/cad/clientes/${id}`, { method: 'PUT', body: dados })
}

export function ativarCliente(req: Requisitar, id: string) {
  return req<void>(`/cad/clientes/${id}/ativar`, { method: 'POST' })
}

export function inativarCliente(req: Requisitar, id: string) {
  return req<void>(`/cad/clientes/${id}/inativar`, { method: 'POST' })
}

export function listarEstados(req: Requisitar, signal?: AbortSignal) {
  return req<Estado[]>('/cad/localidades/estados', { signal })
}

export function listarMunicipios(req: Requisitar, uf: string, signal?: AbortSignal) {
  return req<Municipio[]>(`/cad/localidades/municipios?uf=${encodeURIComponent(uf)}`, { signal })
}
