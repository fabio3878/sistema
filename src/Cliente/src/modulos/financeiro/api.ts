import type {
  ContaCabecalhoEntrada,
  ContaEntrada,
  ContaReceber,
  FiltroContas,
  FormaPagamento,
  PaginaContas,
  Parametros,
  RecebimentoEntrada,
  SugestaoRecebimento,
} from './tipos'

/** Fetch autenticado exposto por `useAuth().requisitar` (bearer + refresh automático). */
export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

// ─────────────────────────────── Contas a Receber ───────────────────────────────

export function listarContas(req: Requisitar, filtro: FiltroContas, signal?: AbortSignal) {
  const p = new URLSearchParams()
  if (filtro.clienteId) p.set('clienteId', filtro.clienteId)
  if (filtro.busca) p.set('busca', filtro.busca)
  if (filtro.situacao) p.set('situacao', filtro.situacao)
  if (filtro.vencimentoDe) p.set('vencimentoDe', filtro.vencimentoDe)
  if (filtro.vencimentoAte) p.set('vencimentoAte', filtro.vencimentoAte)
  if (filtro.emissaoDe) p.set('emissaoDe', filtro.emissaoDe)
  if (filtro.emissaoAte) p.set('emissaoAte', filtro.emissaoAte)
  p.set('pagina', String(filtro.pagina))
  p.set('tamanho', String(filtro.tamanho))
  return req<PaginaContas>(`/fin/contas-receber?${p.toString()}`, { signal })
}

export function obterConta(req: Requisitar, id: string, signal?: AbortSignal) {
  return req<ContaReceber>(`/fin/contas-receber/${id}`, { signal })
}

export function criarConta(req: Requisitar, dados: ContaEntrada) {
  return req<{ id: string }>('/fin/contas-receber', { method: 'POST', body: dados })
}

export function atualizarCabecalho(req: Requisitar, id: string, dados: ContaCabecalhoEntrada) {
  return req<void>(`/fin/contas-receber/${id}`, { method: 'PUT', body: dados })
}

export function cancelarConta(req: Requisitar, id: string) {
  return req<void>(`/fin/contas-receber/${id}/cancelar`, { method: 'POST' })
}

// ─────────────────────────────── Recebimentos ───────────────────────────────

export function sugerirRecebimento(req: Requisitar, parcelaId: string, signal?: AbortSignal) {
  return req<SugestaoRecebimento>(`/fin/contas-receber/parcelas/${parcelaId}/sugestao`, { signal })
}

export function registrarRecebimento(req: Requisitar, parcelaId: string, dados: RecebimentoEntrada) {
  return req<{ id: string }>(`/fin/contas-receber/parcelas/${parcelaId}/recebimentos`, {
    method: 'POST',
    body: dados,
  })
}

export function estornarRecebimento(req: Requisitar, parcelaId: string, recebimentoId: string, motivo: string | null) {
  return req<void>(
    `/fin/contas-receber/parcelas/${parcelaId}/recebimentos/${recebimentoId}/estornar`,
    { method: 'POST', body: { motivo } },
  )
}

// ─────────────────────────────── Formas de pagamento ───────────────────────────────

export function listarFormas(req: Requisitar, situacao?: 'ativo' | 'inativo', signal?: AbortSignal) {
  const qs = situacao ? `?situacao=${situacao}` : ''
  return req<FormaPagamento[]>(`/fin/formas-pagamento${qs}`, { signal })
}

export function criarForma(req: Requisitar, nome: string) {
  return req<{ id: string }>('/fin/formas-pagamento', { method: 'POST', body: { nome } })
}

export function atualizarForma(req: Requisitar, id: string, nome: string) {
  return req<void>(`/fin/formas-pagamento/${id}`, { method: 'PUT', body: { nome } })
}

export function ativarForma(req: Requisitar, id: string) {
  return req<void>(`/fin/formas-pagamento/${id}/ativar`, { method: 'POST' })
}

export function inativarForma(req: Requisitar, id: string) {
  return req<void>(`/fin/formas-pagamento/${id}/inativar`, { method: 'POST' })
}

// ─────────────────────────────── Parâmetros ───────────────────────────────

export function obterParametros(req: Requisitar, signal?: AbortSignal) {
  return req<Parametros>('/fin/parametros', { signal })
}

export function atualizarParametros(req: Requisitar, dados: Parametros) {
  return req<void>('/fin/parametros', { method: 'PUT', body: dados })
}
