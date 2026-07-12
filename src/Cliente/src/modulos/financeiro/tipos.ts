// Tipos do front espelhando os DTOs de Financeiro.Contratos.
// Enums chegam como string (JsonStringEnumConverter); DateOnly como 'yyyy-MM-dd'; decimais como number.

export type TipoOrigemConta = 'Manual' | 'Venda'

export type StatusParcela =
  | 'Aberta'
  | 'RecebidaParcial'
  | 'Recebida'
  | 'Vencida'
  | 'Cancelada'
  | 'Renegociada'

export type SituacaoConta =
  | 'EmAberto'
  | 'ParcialmenteRecebida'
  | 'Quitada'
  | 'PossuiParcelasVencidas'
  | 'Cancelada'

export interface Recebimento {
  id: string
  data: string
  valorRecebido: number
  desconto: number
  juros: number
  multa: number
  acrescimos: number
  formaPagamentoId: string
  formaPagamentoNome: string | null
  observacoes: string | null
  usuarioId: string | null
  estornado: boolean
  estornadoEm: string | null
  estornoMotivo: string | null
}

export interface Parcela {
  id: string
  numero: number
  totalParcelas: number
  valorOriginal: number
  vencimento: string
  dataPrevistaRecebimento: string | null
  percentualJurosOverride: number | null
  totalPago: number
  saldoPrincipal: number
  juros: number
  multa: number
  saldoAtualizado: number
  diasAtraso: number
  status: StatusParcela
  observacoes: string | null
  recebimentos: Recebimento[]
}

export interface ContaReceber {
  id: string
  clienteId: string
  clienteNome: string | null
  descricao: string | null
  tipoOrigem: TipoOrigemConta
  documentoOrigem: string | null
  numeroDocumento: string | null
  valorTotal: number
  quantidadeParcelas: number
  dataEmissao: string
  categoriaFinanceira: string | null
  observacoes: string | null
  totalRecebido: number
  saldoTotal: number
  situacao: SituacaoConta
  parcelas: Parcela[]
}

export interface PaginaContas {
  itens: ContaReceber[]
  total: number
  pagina: number
  tamanho: number
}

export interface SugestaoRecebimento {
  data: string
  saldoPrincipal: number
  diasAtraso: number
  juros: number
  multa: number
  saldoAtualizado: number
}

export interface FormaPagamento {
  id: string
  nome: string
  ativo: boolean
}

export interface Parametros {
  jurosMoraMensalPercent: number
  multaPercent: number
}

export interface FiltroContas {
  clienteId?: string
  busca?: string
  situacao?: SituacaoConta
  vencimentoDe?: string
  vencimentoAte?: string
  emissaoDe?: string
  emissaoAte?: string
  pagina: number
  tamanho: number
}

// Entradas (escrita)

export interface ParcelaEntrada {
  numero: number
  valor: number
  vencimento: string
  dataPrevistaRecebimento?: string | null
  percentualJurosOverride?: number | null
}

export interface ContaEntrada {
  clienteId: string
  valorTotal: number
  quantidadeParcelas: number
  dataEmissao: string
  primeiroVencimento: string
  descricao?: string | null
  tipoOrigem?: TipoOrigemConta
  documentoOrigem?: string | null
  numeroDocumento?: string | null
  categoriaFinanceira?: string | null
  observacoes?: string | null
  intervaloDias?: number
  parcelas?: ParcelaEntrada[] | null
}

export interface ContaCabecalhoEntrada {
  descricao: string | null
  documentoOrigem: string | null
  numeroDocumento: string | null
  categoriaFinanceira: string | null
  observacoes: string | null
}

export interface RecebimentoEntrada {
  data: string
  valorRecebido: number
  formaPagamentoId: string
  desconto?: number
  juros?: number
  multa?: number
  acrescimos?: number
  observacoes?: string | null
}
