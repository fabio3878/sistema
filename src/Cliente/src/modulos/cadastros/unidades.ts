/** Unidades de medida (referência global, endpoint /cad/unidades) — compartilhado por Produtos e Serviços. */

export interface Unidade {
  sigla: string
  descricao: string
  casasDecimais: number
  fracionavel: boolean
}

export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

export function listarUnidades(req: Requisitar, signal?: AbortSignal) {
  return req<Unidade[]>('/cad/unidades', { signal })
}
