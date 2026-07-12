import type { BadgeProps } from '@/components/ui/badge'
import type { SituacaoConta, StatusParcela } from './tipos'

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

/** Formata um número como moeda BRL (R$ 1.234,56). */
export function formatarMoeda(valor: number): string {
  return moeda.format(valor ?? 0)
}

/** Formata uma DateOnly do backend ('yyyy-MM-dd') como dd/MM/yyyy — sem fuso (é data pura). */
export function formatarData(iso: string | null | undefined): string {
  if (!iso) return '—'
  const [y, m, d] = iso.split('-')
  return d && m && y ? `${d}/${m}/${y}` : iso
}

/** ISO de hoje ('yyyy-MM-dd') em horário local, para defaults de formulário. */
export function hojeIso(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const rotulosStatus: Record<StatusParcela, string> = {
  Aberta: 'Em aberto',
  RecebidaParcial: 'Parcial',
  Recebida: 'Recebida',
  Vencida: 'Vencida',
  Cancelada: 'Cancelada',
  Renegociada: 'Renegociada',
}

export function rotuloStatus(s: StatusParcela): string {
  return rotulosStatus[s] ?? s
}

export function tomStatus(s: StatusParcela): NonNullable<BadgeProps['tom']> {
  switch (s) {
    case 'Recebida':
      return 'success'
    case 'RecebidaParcial':
      return 'info'
    case 'Vencida':
      return 'danger'
    case 'Renegociada':
      return 'warning'
    default:
      return 'neutro'
  }
}

const rotulosSituacao: Record<SituacaoConta, string> = {
  EmAberto: 'Em aberto',
  ParcialmenteRecebida: 'Parcial',
  Quitada: 'Quitada',
  PossuiParcelasVencidas: 'Vencida',
  Cancelada: 'Cancelada',
}

export function rotuloSituacao(s: SituacaoConta): string {
  return rotulosSituacao[s] ?? s
}

export function tomSituacao(s: SituacaoConta): NonNullable<BadgeProps['tom']> {
  switch (s) {
    case 'Quitada':
      return 'success'
    case 'ParcialmenteRecebida':
      return 'info'
    case 'PossuiParcelasVencidas':
      return 'danger'
    default:
      return 'neutro'
  }
}
