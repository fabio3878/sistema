import {
  LayoutDashboard,
  Users,
  Boxes,
  ShoppingCart,
  Wallet,
  type LucideIcon,
} from 'lucide-react'

/**
 * Catálogo de navegação da casca única. Cada item declara o MÓDULO licenciável
 * e a FUNCIONALIDADE (func:<codigo>) exigida — a sidebar filtra por licença +
 * permissão (ver lib/sessao.ts). Espelha o catálogo do backend; o front não
 * inventa item nem permissão.
 */
export interface ItemNav {
  rota: string
  titulo: string
  icone: LucideIcon
  /** Módulo (licenca.ModuloAtivo). Dashboard usa 'core' (sempre ativo). */
  modulo: string
  /** Código da funcionalidade (convenção <modulo>.<recurso>.<acao>). */
  funcionalidade: string
}

export const NAV: ItemNav[] = [
  {
    rota: '/',
    titulo: 'Dashboard',
    icone: LayoutDashboard,
    modulo: 'core',
    funcionalidade: 'core.dashboard.ver',
  },
  {
    rota: '/clientes',
    titulo: 'Clientes',
    icone: Users,
    modulo: 'cad',
    funcionalidade: 'cad.cliente.listar',
  },
  {
    rota: '/produtos',
    titulo: 'Produtos',
    icone: Boxes,
    modulo: 'est',
    funcionalidade: 'est.produto.listar',
  },
  {
    rota: '/vendas',
    titulo: 'Vendas',
    icone: ShoppingCart,
    modulo: 'ven',
    funcionalidade: 'ven.venda.listar',
  },
  {
    rota: '/financeiro',
    titulo: 'Financeiro',
    icone: Wallet,
    modulo: 'fin',
    funcionalidade: 'fin.lancamento.listar',
  },
]
