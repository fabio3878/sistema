import { Badge } from '@/components/ui/badge'
import { Drawer } from '@/components/ui/drawer'
import { exibirValor, formatarDataHora, lerDiferencas, rotuloOperacao, tomOperacao } from './formato'
import type { RegistroAuditoria } from './tipos'

interface Props {
  registro: RegistroAuditoria | null
  aberto: boolean
  onAbrir: (v: boolean) => void
}

/** Detalhe só-leitura de uma linha da trilha: cabeçalho + diff campo/de/para. Sem rodapé de ações. */
export function AuditoriaDrawer({ registro, aberto, onAbrir }: Props) {
  const diffs = registro ? lerDiferencas(registro.alteracoes) : []

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo="Detalhe da auditoria"
      descricao={registro ? `${registro.entidade} · ${formatarDataHora(registro.ocorridoEm)}` : ''}
    >
      {registro && (
        <div className="space-y-6">
          <dl className="grid grid-cols-2 gap-4">
            <Info rotulo="Operação">
              <Badge tom={tomOperacao(registro.operacao)}>{rotuloOperacao(registro.operacao)}</Badge>
            </Info>
            <Info rotulo="Usuário">{registro.usuarioLogin ?? '— (sistema)'}</Info>
            <Info rotulo="Entidade">{registro.entidade}</Info>
            <Info rotulo="Data/hora">{formatarDataHora(registro.ocorridoEm)}</Info>
            <Info rotulo="Registro" className="col-span-full">
              <span className="tnum break-all text-fg-muted">{registro.registroId}</span>
            </Info>
          </dl>

          <section className="space-y-3">
            <h4 className="text-small font-semibold uppercase tracking-wide text-fg-muted">
              Alterações
            </h4>
            {diffs.length === 0 ? (
              <p className="text-small text-fg-muted">Sem campos alterados.</p>
            ) : (
              <div className="overflow-hidden rounded-lg border border-border">
                <table className="w-full text-body">
                  <thead className="bg-surface">
                    <tr className="border-b border-border text-caption font-semibold uppercase tracking-wide text-fg-muted">
                      <th className="px-3 py-2 text-left">Campo</th>
                      <th className="px-3 py-2 text-left">De</th>
                      <th className="px-3 py-2 text-left">Para</th>
                    </tr>
                  </thead>
                  <tbody>
                    {diffs.map((d) => (
                      <tr key={d.campo} className="border-b border-border last:border-0 align-top">
                        <td className="px-3 py-1.5 font-medium text-fg">{d.campo}</td>
                        <td className="px-3 py-1.5 text-fg-muted line-through decoration-danger/40">
                          {exibirValor(d.de)}
                        </td>
                        <td className="px-3 py-1.5 text-fg">{exibirValor(d.para)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      )}
    </Drawer>
  )
}

function Info({
  rotulo,
  className,
  children,
}: {
  rotulo: string
  className?: string
  children: React.ReactNode
}) {
  return (
    <div className={`space-y-1 ${className ?? ''}`}>
      <dt className="text-caption font-medium uppercase tracking-wide text-fg-muted">{rotulo}</dt>
      <dd className="text-body text-fg">{children}</dd>
    </div>
  )
}
