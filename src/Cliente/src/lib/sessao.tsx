import { createContext, useContext, type ReactNode } from 'react'

/**
 * Sessão do usuário — espelha o backend (CLAUDE.md / ARQUITETURA_1.md §6.8):
 * a casca é gated por LICENÇA (módulos contratados) + PERMISSÃO (claims
 * func:<codigo> do JWT). Aqui é um MOCK; quando o login JWT for plugado, estes
 * dados vêm das claims do token. O front NUNCA inventa permissão.
 */
export interface Sessao {
  usuario: { nome: string; login: string; empresa: string }
  /** Módulos licenciados para a empresa (licenca.ModuloAtivo). */
  modulosAtivos: Set<string>
  /** true = ConcedeTodas (perfil admin); senão usa o conjunto de funcionalidades. */
  concedeTodas: boolean
  funcionalidades: Set<string>
}

const SESSAO_MOCK: Sessao = {
  usuario: { nome: 'Fábio Moreno', login: 'admin', empresa: 'Loja Centro' },
  // Simula uma loja: cadastros, estoque, vendas, financeiro. (sem "clínica" etc.)
  modulosAtivos: new Set(['cad', 'est', 'ven', 'fin']),
  concedeTodas: true,
  funcionalidades: new Set<string>(),
}

const Ctx = createContext<Sessao>(SESSAO_MOCK)

export function SessaoProvider({ children }: { children: ReactNode }) {
  return <Ctx.Provider value={SESSAO_MOCK}>{children}</Ctx.Provider>
}

export function useSessao() {
  return useContext(Ctx)
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
