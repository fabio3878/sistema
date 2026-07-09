import { TrendingUp, TrendingDown, ShoppingCart, Wallet, Package, Users } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import type { LucideIcon } from 'lucide-react'

interface Kpi {
  rotulo: string
  valor: string
  delta: string
  subindo: boolean
  icone: LucideIcon
}

const KPIS: Kpi[] = [
  { rotulo: 'Receita (hoje)', valor: 'R$ 12.480', delta: '+8,2%', subindo: true, icone: Wallet },
  { rotulo: 'Vendas', valor: '143', delta: '+12', subindo: true, icone: ShoppingCart },
  { rotulo: 'Ticket médio', valor: 'R$ 87,27', delta: '-2,1%', subindo: false, icone: TrendingUp },
  { rotulo: 'Produtos em falta', valor: '7', delta: '+3', subindo: false, icone: Package },
]

/** Dashboard modular (DESIGN_1 §5) — KPIs de exemplo. Widgets viram drag-and-drop depois. */
export function DashboardPage() {
  return (
    <div className="mx-auto max-w-6xl space-y-6">
      <div>
        <h1 className="text-h1 text-fg">Bom dia, Fábio 👋</h1>
        <p className="text-small text-fg-muted">Visão geral da Loja Centro — hoje.</p>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {KPIS.map((kpi) => {
          const Icone = kpi.icone
          return (
            <Card key={kpi.rotulo}>
              <CardContent className="p-5 pt-5">
                <div className="flex items-center justify-between">
                  <span className="text-small text-fg-muted">{kpi.rotulo}</span>
                  <Icone className="size-4 text-fg-muted" />
                </div>
                <div className="mt-2 flex items-end justify-between">
                  <span className="tnum text-display text-fg">{kpi.valor}</span>
                  <Badge tom={kpi.subindo ? 'success' : 'danger'}>
                    {kpi.subindo ? (
                      <TrendingUp className="size-3" />
                    ) : (
                      <TrendingDown className="size-3" />
                    )}
                    {kpi.delta}
                  </Badge>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {/* placeholder de widgets maiores */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardContent className="p-5">
            <div className="mb-4 flex items-center justify-between">
              <h3 className="text-h3 text-fg">Vendas na semana</h3>
              <Badge tom="info">últimos 7 dias</Badge>
            </div>
            <div className="grid h-48 place-items-center rounded-md border border-dashed border-border text-small text-fg-muted">
              Gráfico (Recharts) entra aqui
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-5">
            <h3 className="mb-4 text-h3 text-fg">Atividade recente</h3>
            <ul className="space-y-3">
              {[
                { icone: ShoppingCart, txt: 'Venda #1042 fechada', meta: 'há 3 min' },
                { icone: Users, txt: 'Cliente “Padaria Sol” criado', meta: 'há 21 min' },
                { icone: Package, txt: 'Estoque de “Café 500g” baixo', meta: 'há 1 h' },
              ].map((a, i) => {
                const Icone = a.icone
                return (
                  <li key={i} className="flex items-center gap-3">
                    <span className="grid size-8 shrink-0 place-items-center rounded-full bg-black/5 text-fg-muted dark:bg-white/5">
                      <Icone className="size-4" />
                    </span>
                    <span className="flex-1 text-small text-fg">{a.txt}</span>
                    <span className="text-caption text-fg-muted">{a.meta}</span>
                  </li>
                )
              })}
            </ul>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
