import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { Sidebar } from './sidebar'
import { Topbar } from './topbar'
import { CommandPalette } from './command-palette'
import { NAV } from '@/modulos/registro'
import type { ReactNode } from 'react'

const CHAVE_RECOLHIDA = 'ac:sidebar-recolhida'

/** Casca única do produto: sidebar + topbar + área de conteúdo. DESIGN_1 §3. */
export function AppShell({ children }: { children: ReactNode }) {
  const [recolhida, setRecolhida] = useState(
    () => localStorage.getItem(CHAVE_RECOLHIDA) === '1',
  )
  const [buscaAberta, setBuscaAberta] = useState(false)
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
        <Topbar titulo={titulo} />
        <main className="flex-1 overflow-y-auto p-6">{children}</main>
      </div>
      <CommandPalette aberto={buscaAberta} onAbrir={setBuscaAberta} />
    </div>
  )
}
