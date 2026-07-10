/**
 * Tipos do cadastro de Serviço — espelham os DTOs de `Cadastros.Contratos`. Cadastro próprio,
 * separado de Produtos, gated pelo módulo Cadastros (`cad.servico.*`). camelCase.
 */

/** Linha da listagem de serviços. */
export interface ServicoResumo {
  id: string
  codigoInterno: string | null
  descricao: string
  unidade: string
  precoVenda: number
  ativo: boolean
}

/** Serviço completo (retorno de GET /cad/servicos/{id}). */
export interface Servico {
  id: string
  empresaId: string
  codigoInterno: string | null
  descricao: string
  unidade: string
  precoVenda: number
  ativo: boolean
}

/** Payload de criação/edição do serviço. */
export interface ServicoEntrada {
  descricao: string
  precoVenda: number
  unidade: string
  codigoInterno?: string | null
}

/** Filtros da listagem. */
export interface FiltroServicos {
  busca?: string
  situacao?: 'ativo' | 'inativo'
}
