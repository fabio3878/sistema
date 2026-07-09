import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { Loader2 } from 'lucide-react'
import { TooltipProvider } from '@/components/ui/tooltip'
import { ThemeProvider } from '@/lib/theme'
import { AuthProvider, useAuth } from '@/lib/auth'
import { LoginPage } from '@/paginas/login'
import { AppShell } from '@/components/shell/app-shell'
import { DashboardPage } from '@/paginas/dashboard'
import { PlaceholderPage } from '@/paginas/placeholder'

/** Decide entre login e casca conforme o estado de autenticação. */
function Raiz() {
  const { status } = useAuth()

  if (status === 'carregando') {
    return (
      <div className="grid h-full place-items-center bg-bg text-fg-muted">
        <Loader2 className="size-6 animate-spin" />
      </div>
    )
  }

  if (status === 'anon') return <LoginPage />

  return (
    <BrowserRouter>
      <AppShell>
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/clientes" element={<PlaceholderPage titulo="Clientes" />} />
          <Route path="/produtos" element={<PlaceholderPage titulo="Produtos" />} />
          <Route path="/vendas" element={<PlaceholderPage titulo="Vendas" />} />
          <Route path="/financeiro" element={<PlaceholderPage titulo="Financeiro" />} />
        </Routes>
      </AppShell>
    </BrowserRouter>
  )
}

export default function App() {
  return (
    <ThemeProvider>
      <TooltipProvider delayDuration={200}>
        <AuthProvider>
          <Raiz />
        </AuthProvider>
      </TooltipProvider>
    </ThemeProvider>
  )
}
