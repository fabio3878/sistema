/**
 * Tipos do módulo Cadastros — espelham os DTOs de `Cadastros.Contratos`. O host serializa em
 * camelCase (web defaults do ASP.NET) e enums como string (JsonStringEnumConverter).
 */
export type TipoPessoa = 'Fisica' | 'Juridica'
export type TipoEndereco = 'Principal' | 'Cobranca' | 'Entrega'
export type IndicadorIe = 'Contribuinte' | 'Isento' | 'NaoContribuinte'
export type RegimeTributario = 'SimplesNacional' | 'SimplesExcessoSublimite' | 'Normal'

/** Linha da listagem (leve — contagem de endereços + cidade principal). */
export interface ClienteResumo {
  id: string
  nome: string
  documento: string
  tipoPessoa: TipoPessoa
  nomeFantasia: string | null
  email: string | null
  telefone: string | null
  cidade: string | null
  uf: string | null
  ativo: boolean
  qtdEnderecos: number
}

export interface Endereco {
  id: string
  tipo: TipoEndereco
  cep: string
  logradouro: string
  numero: string
  complemento: string | null
  bairro: string
  municipio: string
  uf: string
  codigoIbgeMunicipio: string
  pais: string
}

/** Cliente completo (retorno de GET /cad/clientes/{id}). */
export interface Cliente {
  id: string
  empresaId: string
  nome: string
  documento: string
  tipoPessoa: TipoPessoa
  nomeFantasia: string | null
  email: string | null
  emailFinanceiro: string | null
  telefone: string | null
  celular: string | null
  whatsapp: string | null
  site: string | null
  dataNascimento: string | null
  rg: string | null
  orgaoEmissorRg: string | null
  inscricaoEstadual: string | null
  inscricaoMunicipal: string | null
  indicadorIe: IndicadorIe
  regimeTributario: RegimeTributario | null
  limiteCredito: number | null
  origem: string | null
  preferencias: string | null
  observacoes: string | null
  aceitaEmail: boolean
  aceitaSms: boolean
  aceitaWhatsapp: boolean
  aceitaLigacoes: boolean
  aceitouTermosLgpd: boolean
  dataAceiteLgpd: string | null
  ativo: boolean
  enderecos: Endereco[]
}

/** Payload de endereço no POST/PUT. `id` ausente = endereço novo. */
export interface EnderecoEntrada {
  id?: string | null
  tipo: TipoEndereco
  cep: string
  logradouro: string
  numero: string
  complemento?: string | null
  bairro: string
  municipio: string
  uf: string
  codigoIbgeMunicipio: string
  pais?: string
}

/** Payload de criação/edição do cliente. */
export interface ClienteEntrada {
  nome: string
  documento: string
  tipoPessoa: TipoPessoa
  indicadorIe: IndicadorIe
  nomeFantasia?: string | null
  email?: string | null
  emailFinanceiro?: string | null
  telefone?: string | null
  celular?: string | null
  whatsapp?: string | null
  site?: string | null
  dataNascimento?: string | null
  rg?: string | null
  orgaoEmissorRg?: string | null
  inscricaoEstadual?: string | null
  inscricaoMunicipal?: string | null
  regimeTributario?: RegimeTributario | null
  limiteCredito?: number | null
  origem?: string | null
  preferencias?: string | null
  observacoes?: string | null
  aceitaEmail?: boolean
  aceitaSms?: boolean
  aceitaWhatsapp?: boolean
  aceitaLigacoes?: boolean
  aceitouTermosLgpd?: boolean
  dataAceiteLgpd?: string | null
  enderecos: EnderecoEntrada[]
}

// Localidades (IBGE)
export interface Estado {
  uf: string
  nome: string
}
export interface Municipio {
  codigoIbge: string
  nome: string
  uf: string
}

/** Filtros da listagem. */
export interface FiltroClientes {
  busca?: string
  cidade?: string
  bairro?: string
  situacao?: 'ativo' | 'inativo'
  mes?: number
}
