/**
 * Tipos da trilha de auditoria (leitura). O host serializa camelCase e o enum de operação como
 * string ("Criacao" | "Alteracao" | "Exclusao"). `alteracoes` é um JSON string com o diff.
 */
export type Operacao = 'Criacao' | 'Alteracao' | 'Exclusao'

/** Módulos que expõem trilha (mapeia para o grupo de endpoint: cad → /cad, acs → /acesso). */
export type ModuloAuditoria = 'cad' | 'acs'

export interface RegistroAuditoria {
  id: string
  ocorridoEm: string
  usuarioId: string | null
  usuarioLogin: string | null
  entidade: string
  registroId: string
  operacao: Operacao
  /** JSON: { "campo": { "de": <antigo>, "para": <novo> } }. */
  alteracoes: string
}

export interface PaginaAuditoria {
  itens: RegistroAuditoria[]
  total: number
  pagina: number
  tamanho: number
}

export interface FiltroAuditoria {
  entidade?: string
  registroId?: string
  usuario?: string
  operacao?: Operacao
  de?: string
  ate?: string
  pagina?: number
  tamanho?: number
}

/** Um par campo/de/para já desempacotado do JSON de `alteracoes`. */
export interface Diferenca {
  campo: string
  de: unknown
  para: unknown
}
