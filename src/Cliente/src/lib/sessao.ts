/**
 * Sessão do usuário — espelha o backend (CLAUDE.md / ARQUITETURA_1.md §6.8):
 * a casca é gated por LICENÇA (módulos contratados) + PERMISSÃO (claims
 * func:<codigo> do JWT). Aqui só o TIPO e as regras puras; o estado vem do
 * AuthProvider (lib/auth.tsx). O front NUNCA inventa permissão.
 */
export interface Sessao {
  usuario: { usuarioId: string; nome: string; login: string; empresa: string }
  /** Módulos licenciados para a empresa (licenca.ModuloAtivo). */
  modulosAtivos: Set<string>
  /** true = ConcedeTodas (perfil admin); senão usa o conjunto de funcionalidades. */
  concedeTodas: boolean
  funcionalidades: Set<string>
}

/**
 * Regra de gating: módulo licenciado E (ConcedeTodas OU tem a funcionalidade).
 * 'core' é sempre ativo (não é licenciável — como o módulo Acesso no backend,
 * que fica fora do licenca.ModuloAtivo).
 */
export function podeVer(sessao: Sessao, modulo: string, funcionalidade: string) {
  const licenciado = modulo === 'core' || sessao.modulosAtivos.has(modulo)
  const permitido = sessao.concedeTodas || sessao.funcionalidades.has(funcionalidade)
  return licenciado && permitido
}

/** Claims que o backend coloca no JWT (ver ServicoTokenJwt). */
interface ClaimsJwt {
  sub?: string
  empresa?: string
  login?: string
  perm_all?: string | boolean
  func?: string | string[]
  exp?: number
}

/** Decodifica o payload de um JWT (base64url) sem validar assinatura (isso é do backend). */
export function lerClaims(accessToken: string): ClaimsJwt {
  const payload = accessToken.split('.')[1]
  const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
  const json = decodeURIComponent(
    atob(base64)
      .split('')
      .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  )
  return JSON.parse(json) as ClaimsJwt
}

/** true se o token já expirou (com folga de 10s). */
export function expirado(accessToken: string): boolean {
  const { exp } = lerClaims(accessToken)
  if (!exp) return true
  return exp * 1000 <= Date.now() + 10_000
}

/**
 * Monta a Sessão a partir das claims do token + o "quem sou eu" do backend.
 * Enquanto não há endpoint de licença, os módulos ativos são derivados: admin
 * (ConcedeTodas) enxerga todos; senão, os prefixos de módulo das funcionalidades.
 * TODO(backend): expor a licença (módulos contratados) para gating fiel.
 */
export function montarSessao(
  accessToken: string,
  eu: { usuarioId: string; login: string; concedeTodas: boolean; funcionalidades: string[] },
  modulosConhecidos: string[],
): Sessao {
  const claims = lerClaims(accessToken)
  const funcionalidades = new Set(eu.funcionalidades)
  const modulosAtivos = eu.concedeTodas
    ? new Set(modulosConhecidos)
    : new Set([...funcionalidades].map((f) => f.split('.')[0]))

  return {
    usuario: {
      usuarioId: eu.usuarioId,
      login: eu.login,
      nome: eu.login,
      empresa: claims.empresa ?? '—',
    },
    concedeTodas: eu.concedeTodas,
    funcionalidades,
    modulosAtivos,
  }
}
