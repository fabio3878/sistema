/**
 * Cliente HTTP fino para o host .NET (AgenteLocal). Em dev, a base é vazia e o
 * Vite faz proxy de /acesso para o host (ver vite.config.ts). Em produção/Tauri,
 * defina VITE_API_URL. Erros de domínio do backend vêm como ProblemDetails.
 */
const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? ''

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

interface Opcoes {
  method?: string
  body?: unknown
  token?: string | null
  signal?: AbortSignal
}

export async function api<T>(caminho: string, opcoes: Opcoes = {}): Promise<T> {
  const { method = 'GET', body, token, signal } = opcoes
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers['Authorization'] = `Bearer ${token}`

  const resp = await fetch(`${BASE}${caminho}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  })

  if (resp.status === 204) return undefined as T

  const texto = await resp.text()
  const dados = texto ? JSON.parse(texto) : null

  if (!resp.ok) {
    // ProblemDetails (.detail) ou { erro } ou fallback pelo status.
    const msg =
      dados?.detail ?? dados?.erro ?? dados?.title ?? `Erro ${resp.status}`
    throw new ApiError(resp.status, msg)
  }

  return dados as T
}
