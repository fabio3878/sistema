import { History } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import type { ModuloAuditoria } from './tipos'

interface Props {
  /** Nome da entidade como o backend grava na trilha (ex.: "Cliente", "Produto", "Servico"). */
  entidade: string
  registroId: string
  modulo?: ModuloAuditoria
}

const funcPorModulo: Record<ModuloAuditoria, string> = {
  cad: 'cad.auditoria.ver',
  acs: 'acs.auditoria.ver',
  fin: 'fin.auditoria.ver',
}

/**
 * Botão "Histórico" para os cadastros: abre a tela de Auditoria já filtrada por este registro.
 * Só aparece se o usuário tem permissão de ver a trilha do módulo.
 */
export function BotaoHistorico({ entidade, registroId, modulo = 'cad' }: Props) {
  const sessao = useSessao()
  const navigate = useNavigate()

  if (!podeVer(sessao, modulo, funcPorModulo[modulo])) return null

  const ir = () => {
    const qs = new URLSearchParams({ modulo, entidade, registroId }).toString()
    navigate(`/auditoria?${qs}`)
  }

  return (
    <Button variant="ghost" type="button" onClick={ir} className="mr-auto">
      <History /> Histórico
    </Button>
  )
}
