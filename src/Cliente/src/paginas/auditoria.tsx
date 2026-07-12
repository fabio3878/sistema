import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Search, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { listarAuditoria } from '@/modulos/auditoria/api'
import { AuditoriaTabela } from '@/modulos/auditoria/auditoria-tabela'
import { AuditoriaDrawer } from '@/modulos/auditoria/auditoria-drawer'
import type { FiltroAuditoria, ModuloAuditoria, Operacao, RegistroAuditoria } from '@/modulos/auditoria/tipos'

const classeSelect =
  'h-9 rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'

const TODOS_MODULOS: { id: ModuloAuditoria; label: string; funcionalidade: string }[] = [
  { id: 'cad', label: 'Cadastros', funcionalidade: 'cad.auditoria.ver' },
  { id: 'fin', label: 'Financeiro', funcionalidade: 'fin.auditoria.ver' },
  { id: 'acs', label: 'Segurança', funcionalidade: 'acs.auditoria.ver' },
]

const TAMANHO = 20

export function AuditoriaPage() {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const navigate = useNavigate()
  const [params] = useSearchParams()

  // Módulos que o usuário pode ver (cada trilha é gated por sua funcionalidade).
  const modulos = TODOS_MODULOS.filter((m) => podeVer(sessao, m.id, m.funcionalidade))

  // Modo "histórico de um registro": entidade + registroId vêm da URL (botão nas telas de cadastro).
  const entidade = params.get('entidade') ?? undefined
  const registroId = params.get('registroId') ?? undefined
  const moduloUrl = params.get('modulo') as ModuloAuditoria | null

  const moduloInicial =
    (moduloUrl && modulos.some((m) => m.id === moduloUrl) ? moduloUrl : modulos[0]?.id) ?? 'cad'

  const [modulo, setModulo] = useState<ModuloAuditoria>(moduloInicial)
  const [operacao, setOperacao] = useState<Operacao | ''>('')
  const [usuarioDraft, setUsuarioDraft] = useState('')
  const [usuario, setUsuario] = useState('')
  const [pagina, setPagina] = useState(1)
  const [selecionado, setSelecionado] = useState<RegistroAuditoria | null>(null)
  const [drawerAberto, setDrawerAberto] = useState(false)

  const filtro: FiltroAuditoria = {
    entidade,
    registroId,
    usuario: usuario || undefined,
    operacao: operacao || undefined,
    pagina,
    tamanho: TAMANHO,
  }

  const consulta = useQuery({
    queryKey: ['auditoria', modulo, entidade, registroId, usuario, operacao, pagina],
    queryFn: ({ signal }) => listarAuditoria(requisitar, modulo, filtro, signal),
  })

  const trocarModulo = (m: ModuloAuditoria) => {
    setModulo(m)
    setPagina(1)
    // Sair do modo histórico ao trocar de módulo (o registroId é de outro módulo).
    if (entidade || registroId) navigate('/auditoria', { replace: true })
  }

  const abrirDetalhe = (r: RegistroAuditoria) => {
    setSelecionado(r)
    setDrawerAberto(true)
  }

  const limparHistorico = () => {
    navigate('/auditoria', { replace: true })
    setPagina(1)
  }

  if (modulos.length === 0) {
    return (
      <div className="mx-auto max-w-5xl">
        <h1 className="text-h1 text-fg">Auditoria</h1>
        <p className="mt-2 text-small text-fg-muted">Você não tem permissão para ver a auditoria.</p>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      <div>
        <h1 className="text-h1 text-fg">Auditoria</h1>
        <p className="text-small text-fg-muted">Trilha de alterações — quem alterou o quê, e quando.</p>
      </div>

      {/* Seletor de módulo (só os que o usuário pode ver). */}
      {modulos.length > 1 && (
        <div className="inline-flex rounded-md border border-border p-0.5">
          {modulos.map((m) => (
            <button
              key={m.id}
              onClick={() => trocarModulo(m.id)}
              className={cn(
                'rounded px-3 py-1 text-small font-medium transition-colors',
                modulo === m.id ? 'bg-primary/10 text-primary' : 'text-fg-muted hover:text-fg',
              )}
            >
              {m.label}
            </button>
          ))}
        </div>
      )}

      {/* Chip do modo histórico (registro específico). */}
      {registroId && (
        <div className="flex items-center gap-2 rounded-md bg-info-bg px-3 py-2 text-small text-info">
          <span>
            Histórico de <strong>{entidade ?? 'registro'}</strong>{' '}
            <span className="tnum opacity-70">{registroId}</span>
          </span>
          <button onClick={limparHistorico} className="ml-auto inline-flex items-center gap-1 hover:underline">
            <X className="size-3.5" /> limpar
          </button>
        </div>
      )}

      {/* Filtros. */}
      <form
        className="flex flex-wrap items-center gap-2"
        onSubmit={(e) => {
          e.preventDefault()
          setUsuario(usuarioDraft)
          setPagina(1)
        }}
      >
        <div className="relative min-w-56 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-fg-muted" />
          <Input
            value={usuarioDraft}
            onChange={(e) => setUsuarioDraft(e.target.value)}
            placeholder="Buscar por usuário (login)…"
            className="pl-9"
          />
        </div>
        <select
          className={cn(classeSelect, 'w-40')}
          value={operacao}
          onChange={(e) => {
            setOperacao(e.target.value as Operacao | '')
            setPagina(1)
          }}
        >
          <option value="">Toda operação</option>
          <option value="Criacao">Criação</option>
          <option value="Alteracao">Alteração</option>
          <option value="Exclusao">Exclusão</option>
        </select>
        <Button type="submit" variant="secondary">
          Filtrar
        </Button>
      </form>

      <AuditoriaTabela
        pagina={consulta.data}
        carregando={consulta.isPending}
        onSelecionar={abrirDetalhe}
        onPagina={setPagina}
      />

      <AuditoriaDrawer registro={selecionado} aberto={drawerAberto} onAbrir={setDrawerAberto} />
    </div>
  )
}
