import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { formatarDataHora, rotuloOperacao, tomOperacao } from './formato'
import type { PaginaAuditoria, RegistroAuditoria } from './tipos'

interface Props {
  pagina: PaginaAuditoria | undefined
  carregando: boolean
  onSelecionar: (r: RegistroAuditoria) => void
  onPagina: (p: number) => void
}

/** Tabela da trilha com paginação SERVER-SIDE (os controles pedem a página ao backend). */
export function AuditoriaTabela({ pagina, carregando, onSelecionar, onPagina }: Props) {
  if (carregando) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-md bg-surface" />
        ))}
      </div>
    )
  }

  const itens = pagina?.itens ?? []
  if (itens.length === 0) {
    return (
      <div className="grid place-items-center rounded-lg border border-dashed border-border py-16 text-fg-muted">
        Nenhum registro de auditoria encontrado.
      </div>
    )
  }

  const total = pagina?.total ?? 0
  const tamanho = pagina?.tamanho ?? 20
  const atual = pagina?.pagina ?? 1
  const totalPaginas = Math.max(1, Math.ceil(total / tamanho))

  return (
    <div className="space-y-3">
      <div className="overflow-hidden rounded-lg border border-border">
        <table className="w-full text-body">
          <thead className="bg-surface">
            <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
              <th className="px-3 py-2 text-left">Data/hora</th>
              <th className="px-3 py-2 text-left">Usuário</th>
              <th className="px-3 py-2 text-left">Entidade</th>
              <th className="px-3 py-2 text-left">Operação</th>
            </tr>
          </thead>
          <tbody>
            {itens.map((r) => (
              <tr
                key={r.id}
                onClick={() => onSelecionar(r)}
                className="border-b border-border last:border-0 hover:bg-black/[0.02] dark:hover:bg-white/[0.03]"
                style={{ height: 40, cursor: 'pointer' }}
              >
                <td className="px-3 py-1.5 align-middle tnum text-fg">{formatarDataHora(r.ocorridoEm)}</td>
                <td className="px-3 py-1.5 align-middle text-fg">{r.usuarioLogin ?? '— (sistema)'}</td>
                <td className="px-3 py-1.5 align-middle text-fg-muted">{r.entidade}</td>
                <td className="px-3 py-1.5 align-middle">
                  <Badge tom={tomOperacao(r.operacao)}>{rotuloOperacao(r.operacao)}</Badge>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-end gap-2 text-small text-fg-muted">
        <span className="tnum">
          {total} registro{total === 1 ? '' : 's'} · página {atual} de {totalPaginas}
        </span>
        <Button variant="secondary" size="sm" disabled={atual <= 1} onClick={() => onPagina(atual - 1)}>
          Anterior
        </Button>
        <Button
          variant="secondary"
          size="sm"
          disabled={atual >= totalPaginas}
          onClick={() => onPagina(atual + 1)}
        >
          Próxima
        </Button>
      </div>
    </div>
  )
}
