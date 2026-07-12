import {
  LayoutDashboard,
  Users,
  Boxes,
  Wrench,
  ShoppingCart,
  Wallet,
  CreditCard,
  ScrollText,
  type LucideIcon,
} from 'lucide-react'
import type { AlvoAcesso } from '@/lib/sessao'

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
  /** Alternativas: o item aparece se o alvo principal OU qualquer uma destas for permitida. */
  requerQualquer?: AlvoAcesso[]
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
    rota: '/servicos',
    titulo: 'Serviços',
    icone: Wrench,
    modulo: 'cad',
    funcionalidade: 'cad.servico.listar',
  },
  {
    rota: '/auditoria',
    titulo: 'Auditoria',
    icone: ScrollText,
    modulo: 'cad',
    funcionalidade: 'cad.auditoria.ver',
    // Cruza módulos: aparece para quem vê a trilha de Cadastros, Financeiro OU Segurança (Acesso).
    requerQualquer: [
      { modulo: 'fin', funcionalidade: 'fin.auditoria.ver' },
      { modulo: 'acs', funcionalidade: 'acs.auditoria.ver' },
    ],
  },
  {
    rota: '/vendas',
    titulo: 'Vendas',
    icone: ShoppingCart,
    modulo: 'ven',
    funcionalidade: 'ven.venda.listar',
  },
  {
    rota: '/contas-receber',
    titulo: 'Contas a receber',
    icone: Wallet,
    modulo: 'fin',
    funcionalidade: 'fin.contareceber.listar',
  },
  {
    rota: '/formas-pagamento',
    titulo: 'Formas de pagamento',
    icone: CreditCard,
    modulo: 'fin',
    funcionalidade: 'fin.formapagamento.listar',
  },
]
