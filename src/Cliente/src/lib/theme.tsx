import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

type Tema = 'claro' | 'escuro'

interface TemaContexto {
  tema: Tema
  alternar: () => void
}

const Ctx = createContext<TemaContexto | null>(null)
const CHAVE = 'ac:tema'

/** Provedor de tema claro/escuro, persistido por usuário (DESIGN_1 §2.1). */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [tema, setTema] = useState<Tema>(() => {
    const salvo = localStorage.getItem(CHAVE) as Tema | null
    if (salvo) return salvo
    const prefereEscuro = window.matchMedia('(prefers-color-scheme: dark)').matches
    return prefereEscuro ? 'escuro' : 'claro'
  })

  useEffect(() => {
    document.documentElement.classList.toggle('dark', tema === 'escuro')
    localStorage.setItem(CHAVE, tema)
  }, [tema])

  const alternar = () => setTema((t) => (t === 'escuro' ? 'claro' : 'escuro'))

  return <Ctx.Provider value={{ tema, alternar }}>{children}</Ctx.Provider>
}

export function useTheme() {
  const ctx = useContext(Ctx)
  if (!ctx) throw new Error('useTheme deve ser usado dentro de <ThemeProvider>')
  return ctx
}
