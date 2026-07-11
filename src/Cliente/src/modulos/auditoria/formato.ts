import type { BadgeProps } from '@/components/ui/badge'
import type { Diferenca, Operacao } from './tipos'

const dataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' })

/** Formata o instante ISO (DateTimeOffset) do backend para exibição local. */
export function formatarDataHora(iso: string): string {
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? iso : dataHora.format(d)
}

const rotulos: Record<Operacao, string> = {
  Criacao: 'Criação',
  Alteracao: 'Alteração',
  Exclusao: 'Exclusão',
}

export function rotuloOperacao(op: Operacao): string {
  return rotulos[op] ?? op
}

/** Cor do Badge por operação: Criação=success, Alteração=info, Exclusão=danger. */
export function tomOperacao(op: Operacao): NonNullable<BadgeProps['tom']> {
  switch (op) {
    case 'Criacao':
      return 'success'
    case 'Exclusao':
      return 'danger'
    default:
      return 'info'
  }
}

/** Representa um valor do diff para leitura (null/undefined → travessão; objeto → JSON). */
export function exibirValor(v: unknown): string {
  if (v === null || v === undefined || v === '') return '—'
  if (typeof v === 'boolean') return v ? 'Sim' : 'Não'
  if (typeof v === 'object') return JSON.stringify(v)
  return String(v)
}

/** Desempacota o JSON de `alteracoes` numa lista de campo/de/para (tolerante a JSON inválido). */
export function lerDiferencas(alteracoesJson: string): Diferenca[] {
  try {
    const obj = JSON.parse(alteracoesJson) as Record<string, { de: unknown; para: unknown }>
    return Object.entries(obj).map(([campo, v]) => ({ campo, de: v?.de, para: v?.para }))
  } catch {
    return []
  }
}
