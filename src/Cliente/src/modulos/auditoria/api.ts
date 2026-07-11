import type { FiltroAuditoria, ModuloAuditoria, PaginaAuditoria } from './tipos'

/** Fetch autenticado exposto por `useAuth().requisitar` (bearer + refresh automático). */
export type Requisitar = <T>(
  caminho: string,
  opcoes?: { method?: string; body?: unknown; signal?: AbortSignal },
) => Promise<T>

/** Grupo de endpoint por módulo: cad → /cad/auditoria, acs → /acesso/auditoria. */
const grupo: Record<ModuloAuditoria, string> = { cad: '/cad', acs: '/acesso' }

export function listarAuditoria(
  req: Requisitar,
  modulo: ModuloAuditoria,
  filtro: FiltroAuditoria,
  signal?: AbortSignal,
) {
  const p = new URLSearchParams()
  if (filtro.entidade) p.set('entidade', filtro.entidade)
  if (filtro.registroId) p.set('registroId', filtro.registroId)
  if (filtro.usuario) p.set('usuario', filtro.usuario)
  if (filtro.operacao) p.set('operacao', filtro.operacao)
  if (filtro.de) p.set('de', filtro.de)
  if (filtro.ate) p.set('ate', filtro.ate)
  if (filtro.pagina) p.set('pagina', String(filtro.pagina))
  if (filtro.tamanho) p.set('tamanho', String(filtro.tamanho))
  const qs = p.toString()
  return req<PaginaAuditoria>(`${grupo[modulo]}/auditoria${qs ? `?${qs}` : ''}`, { signal })
}
