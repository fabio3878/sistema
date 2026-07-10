/**
 * Tipos do cadastro de Produto (mercadoria) — espelham os DTOs de `Cadastros.Contratos`. O produto
 * mora em Cadastros (master data), mas seu gating é do módulo Estoque (`est.produto.*`), por isso
 * vive nesta pasta `modulos/estoque`. O host serializa em camelCase e enums como string.
 */
export type OrigemMercadoria =
  | 'Nacional'
  | 'EstrangeiraImportacaoDireta'
  | 'EstrangeiraAdquiridaMercadoInterno'
  | 'NacionalImportacaoSuperior40'
  | 'NacionalProcessosBasicos'
  | 'NacionalImportacaoInferiorIgual40'
  | 'EstrangeiraImportacaoDiretaSemSimilar'
  | 'EstrangeiraAdquiridaMercadoInternoSemSimilar'
  | 'NacionalImportacaoSuperior70'

/** Linha da listagem de produtos. */
export interface ProdutoResumo {
  id: string
  codigoInterno: string | null
  descricao: string
  codigoBarras: string | null
  unidade: string
  precoVenda: number
  ativo: boolean
}

/** Produto completo (retorno de GET /cad/produtos/{id}). */
export interface Produto {
  id: string
  empresaId: string
  codigoInterno: string | null
  descricao: string
  codigoBarras: string | null
  unidade: string
  ncm: string
  cest: string | null
  origem: OrigemMercadoria
  precoVenda: number
  ativo: boolean
}

/** Payload de criação/edição do produto. */
export interface ProdutoEntrada {
  descricao: string
  ncm: string
  precoVenda: number
  unidade: string
  origem: OrigemMercadoria
  codigoInterno?: string | null
  codigoBarras?: string | null
  cest?: string | null
}

/** Filtros da listagem. */
export interface FiltroProdutos {
  busca?: string
  situacao?: 'ativo' | 'inativo'
}
