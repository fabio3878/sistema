import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { KeyRound } from 'lucide-react'
import { Sidebar } from './sidebar'
import { Topbar } from './topbar'
import { CommandPalette } from './command-palette'
import { TrocarSenhaDialog } from './trocar-senha-dialog'
import { NAV } from '@/modulos/registro'
import { useAuth } from '@/lib/auth'
import type { ReactNode } from 'react'

const CHAVE_RECOLHIDA = 'ac:sidebar-recolhida'

/** Casca única do produto: sidebar + topbar + área de conteúdo. DESIGN_1 §3. */
export function AppShell({ children }: { children: ReactNode }) {
  const { deveTrocarSenha } = useAuth()
  const [recolhida, setRecolhida] = useState(
    () => localStorage.getItem(CHAVE_RECOLHIDA) === '1',
  )
  const [buscaAberta, setBuscaAberta] = useState(false)
  const [trocarAberto, setTrocarAberto] = useState(false)
  const { pathname } = useLocation()
  const titulo = NAV.find((i) => i.rota === pathname)?.titulo ?? 'Dashboard'

  const alternarSidebar = () =>
    setRecolhida((r) => {
      localStorage.setItem(CHAVE_RECOLHIDA, r ? '0' : '1')
      return !r
    })

  return (
    <div className="flex h-full w-full overflow-hidden">
      <Sidebar
        recolhida={recolhida}
        onAlternar={alternarSidebar}
        onAbrirBusca={() => setBuscaAberta(true)}
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar titulo={titulo} onTrocarSenha={() => setTrocarAberto(true)} />
        {deveTrocarSenha && (
          <div className="flex items-center gap-2 border-b border-warning/30 bg-warning-bg px-6 py-2 text-small text-warning">
            <KeyRound className="size-4 shrink-0" />
            <span>Recomendamos trocar sua senha inicial.</span>
            <button
              onClick={() => setTrocarAberto(true)}
              className="ml-1 font-medium underline underline-offset-2"
            >
              Trocar agora
            </button>
          </div>
        )}
        <main className="flex-1 overflow-y-auto p-6">{children}</main>
      </div>
      <CommandPalette aberto={buscaAberta} onAbrir={setBuscaAberta} />
      <TrocarSenhaDialog aberto={trocarAberto} onAbrir={setTrocarAberto} />
    </div>
  )
}
