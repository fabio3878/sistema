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
 * 'core' e 'acs' (Acesso) são sempre ativos (não licenciáveis — o backend mantém o
 * Acesso fora do licenca.ModuloAtivo, autenticação/segurança não é contratável).
 */
export function podeVer(sessao: Sessao, modulo: string, funcionalidade: string) {
  const licenciado = modulo === 'core' || modulo === 'acs' || sessao.modulosAtivos.has(modulo)
  const permitido = sessao.concedeTodas || sessao.funcionalidades.has(funcionalidade)
  return licenciado && permitido
}

/** Alternativa de gating (módulo+funcionalidade), para itens que atendem a mais de um módulo. */
export interface AlvoAcesso {
  modulo: string
  funcionalidade: string
}

/**
 * Visibilidade de um item de navegação: visível se o alvo principal OU QUALQUER alternativa
 * (`requerQualquer`) for permitida. Usado por telas que cruzam módulos (ex.: Auditoria).
 */
export function podeVerItem(
  sessao: Sessao,
  item: AlvoAcesso & { requerQualquer?: AlvoAcesso[] },
) {
  if (podeVer(sessao, item.modulo, item.funcionalidade)) return true
  return (item.requerQualquer ?? []).some((a) => podeVer(sessao, a.modulo, a.funcionalidade))
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
