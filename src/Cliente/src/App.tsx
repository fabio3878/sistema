import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { TooltipProvider } from '@/components/ui/tooltip'
import { ThemeProvider } from '@/lib/theme'
import { SessaoProvider } from '@/lib/sessao'
import { AppShell } from '@/components/shell/app-shell'
import { DashboardPage } from '@/paginas/dashboard'
import { PlaceholderPage } from '@/paginas/placeholder'

export default function App() {
  return (
    <ThemeProvider>
      <SessaoProvider>
        <TooltipProvider delayDuration={200}>
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
        </TooltipProvider>
      </SessaoProvider>
    </ThemeProvider>
  )
}
