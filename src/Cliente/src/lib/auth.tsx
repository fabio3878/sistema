import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
import { api, ApiError } from './api'
import { expirado, montarSessao, type Sessao } from './sessao'
import { NAV } from '@/modulos/registro'

const CHAVE = 'ac:auth'
const MODULOS_CONHECIDOS = [...new Set(NAV.map((n) => n.modulo).filter((m) => m !== 'core'))]

interface Tokens {
  accessToken: string
  refreshToken: string
}

interface RespostaEu {
  usuarioId: string
  login: string
  concedeTodas: boolean
  funcionalidades: string[]
}

interface TokenResposta {
  accessToken: string
  refreshToken: string
  expiraEm: string
  deveTrocarSenha: boolean
}

type Status = 'carregando' | 'anon' | 'autenticado'

interface AuthContexto {
  status: Status
  sessao: Sessao | null
  deveTrocarSenha: boolean
  entrar: (login: string, senha: string) => Promise<void>
  sair: () => Promise<void>
  trocarSenha: (senhaAtual: string, senhaNova: string) => Promise<void>
}

const Ctx = createContext<AuthContexto | null>(null)

function lerTokens(): Tokens | null {
  const bruto = localStorage.getItem(CHAVE)
  if (!bruto) return null
  try {
    return JSON.parse(bruto) as Tokens
  } catch {
    return null
  }
}

function gravarTokens(t: Tokens | null) {
  if (t) localStorage.setItem(CHAVE, JSON.stringify(t))
  else localStorage.removeItem(CHAVE)
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<Status>('carregando')
  const [sessao, setSessao] = useState<Sessao | null>(null)
  const [deveTrocarSenha, setDeveTrocarSenha] = useState(false)
  const tokens = useRef<Tokens | null>(null)

  /** Busca /eu (com refresh automático em 401) e monta a sessão. */
  const carregarSessao = useCallback(async () => {
    const eu = await comRefresh<RespostaEu>('/acesso/eu')
    setSessao(montarSessao(tokens.current!.accessToken, eu, MODULOS_CONHECIDOS))
    setStatus('autenticado')
  }, [])

  /** Chama endpoint autenticado; em 401 tenta um refresh e repete uma vez. */
  const comRefresh = useCallback(async function <T>(
    caminho: string,
    opcoes: { method?: string; body?: unknown } = {},
  ): Promise<T> {
    try {
      return await api<T>(caminho, { ...opcoes, token: tokens.current?.accessToken })
    } catch (e) {
      if (e instanceof ApiError && e.status === 401 && tokens.current?.refreshToken) {
        const novo = await api<TokenResposta>('/acesso/refresh', {
          method: 'POST',
          body: { refreshToken: tokens.current.refreshToken },
        })
        tokens.current = { accessToken: novo.accessToken, refreshToken: novo.refreshToken }
        gravarTokens(tokens.current)
        return await api<T>(caminho, { ...opcoes, token: tokens.current.accessToken })
      }
      throw e
    }
  }, [])

  // Bootstrap: reidrata a partir dos tokens salvos.
  useEffect(() => {
    void (async () => {
      const salvos = lerTokens()
      if (!salvos) return setStatus('anon')
      tokens.current = salvos
      try {
        if (expirado(salvos.accessToken)) {
          const novo = await api<TokenResposta>('/acesso/refresh', {
            method: 'POST',
            body: { refreshToken: salvos.refreshToken },
          })
          tokens.current = { accessToken: novo.accessToken, refreshToken: novo.refreshToken }
          gravarTokens(tokens.current)
        }
        await carregarSessao()
      } catch {
        gravarTokens(null)
        tokens.current = null
        setSessao(null)
        setStatus('anon')
      }
    })()
  }, [carregarSessao])

  const entrar = useCallback(
    async (login: string, senha: string) => {
      const r = await api<TokenResposta>('/acesso/login', {
        method: 'POST',
        body: { login, senha },
      })
      tokens.current = { accessToken: r.accessToken, refreshToken: r.refreshToken }
      gravarTokens(tokens.current)
      setDeveTrocarSenha(r.deveTrocarSenha)
      await carregarSessao()
    },
    [carregarSessao],
  )

  const sair = useCallback(async () => {
    const rt = tokens.current?.refreshToken
    if (rt) {
      // best-effort: revoga o refresh no servidor.
      try {
        await comRefresh('/acesso/logout', { method: 'POST', body: { refreshToken: rt } })
      } catch {
        /* ignora: local logout acontece de qualquer forma */
      }
    }
    gravarTokens(null)
    tokens.current = null
    setSessao(null)
    setDeveTrocarSenha(false)
    setStatus('anon')
  }, [comRefresh])

  const trocarSenha = useCallback(
    async (senhaAtual: string, senhaNova: string) => {
      // 204 no sucesso. O backend revoga os refresh tokens → exige novo login.
      await comRefresh('/acesso/trocar-senha', {
        method: 'POST',
        body: { senhaAtual, senhaNova },
      })
      gravarTokens(null)
      tokens.current = null
      setSessao(null)
      setDeveTrocarSenha(false)
      setStatus('anon')
    },
    [comRefresh],
  )

  return (
    <Ctx.Provider value={{ status, sessao, deveTrocarSenha, entrar, sair, trocarSenha }}>
      {children}
    </Ctx.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de <AuthProvider>')
  return ctx
}

/** Sessão garantidamente presente (usar dentro da casca autenticada). */
export function useSessao(): Sessao {
  const { sessao } = useAuth()
  if (!sessao) throw new Error('useSessao chamado sem sessão ativa')
  return sessao
}
