import { useMemo, useState } from 'react'
import { ChevronsUpDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { SeletorRegistro, type ColunaSeletor } from '@/components/ui/seletor-registro'
import { useAuth } from '@/lib/auth'
import { listarClientes } from './api'
import { formatarDocumento, formatarTelefone } from './formato'
import type { ClienteResumo } from './tipos'

interface Props {
  value: string
  onChange: (id: string, nome: string) => void
  disabled?: boolean
  placeholder?: string
  /** Rótulo já conhecido (ex.: edição, quando o id vem preenchido). */
  rotuloInicial?: string
}

/** Colunas oferecidas no seletor de cliente (as marcadas `visivelPadrao` aparecem de início). */
const COLUNAS: ColunaSeletor<ClienteResumo>[] = [
  {
    chave: 'nome',
    titulo: 'Nome',
    visivelPadrao: true,
    ordenavel: true,
    largura: 240,
    valor: (c) => c.nome,
    render: (c) => <span className="font-medium text-fg">{c.nome}</span>,
  },
  {
    chave: 'documento',
    titulo: 'Documento',
    visivelPadrao: true,
    ordenavel: true,
    largura: 170,
    valor: (c) => c.documento,
    render: (c) => <span className="tnum text-fg-muted">{formatarDocumento(c.documento)}</span>,
  },
  {
    chave: 'cidade',
    titulo: 'Cidade/UF',
    visivelPadrao: true,
    ordenavel: true,
    largura: 150,
    valor: (c) => c.cidade ?? '',
    render: (c) =>
      c.cidade ? (
        <span className="text-fg">
          {c.cidade}
          {c.uf ? <span className="text-fg-muted">/{c.uf}</span> : null}
        </span>
      ) : (
        <span className="text-fg-muted">—</span>
      ),
  },
  {
    chave: 'tipo',
    titulo: 'Tipo',
    ordenavel: true,
    largura: 90,
    valor: (c) => c.tipoPessoa,
    render: (c) => <Badge tom="neutro">{c.tipoPessoa === 'Fisica' ? 'PF' : 'PJ'}</Badge>,
  },
  {
    chave: 'fantasia',
    titulo: 'Nome fantasia',
    largura: 200,
    valor: (c) => c.nomeFantasia ?? '',
    render: (c) => <span className="text-fg-muted">{c.nomeFantasia ?? '—'}</span>,
  },
  {
    chave: 'email',
    titulo: 'E-mail',
    largura: 220,
    valor: (c) => c.email ?? '',
    render: (c) => <span className="text-fg-muted">{c.email ?? '—'}</span>,
  },
  {
    chave: 'telefone',
    titulo: 'Telefone',
    largura: 160,
    valor: (c) => c.telefone ?? '',
    render: (c) => <span className="tnum text-fg-muted">{c.telefone ? formatarTelefone(c.telefone) : '—'}</span>,
  },
  {
    chave: 'endereco',
    titulo: 'Endereço',
    largura: 240,
    valor: (c) => c.logradouro ?? '',
    render: (c) =>
      c.logradouro ? (
        <span className="text-fg-muted">
          {c.logradouro}
          {c.numero ? `, ${c.numero}` : ''}
        </span>
      ) : (
        <span className="text-fg-muted">—</span>
      ),
  },
  {
    chave: 'bairro',
    titulo: 'Bairro',
    largura: 160,
    valor: (c) => c.bairro ?? '',
    render: (c) => <span className="text-fg-muted">{c.bairro ?? '—'}</span>,
  },
]

/** Campo de seleção de cliente via janela de busca em grade (colunas configuráveis). */
export function SeletorCliente({ value, onChange, disabled, placeholder = 'Selecione o cliente…', rotuloInicial }: Props) {
  const { requisitar } = useAuth()
  const [aberto, setAberto] = useState(false)
  const [rotulo, setRotulo] = useState(rotuloInicial ?? '')

  const buscar = useMemo(
    () => (termo: string, signal?: AbortSignal) =>
      listarClientes(requisitar, { busca: termo || undefined, situacao: 'ativo' }, signal),
    [requisitar],
  )

  const texto = rotulo || rotuloInicial || (value ? '' : '')

  return (
    <>
      <button
        type="button"
        disabled={disabled}
        onClick={() => setAberto(true)}
        className={cn(
          'flex h-9 w-full items-center justify-between gap-2 rounded-md border border-border bg-surface px-3 text-body',
          'transition-colors duration-fast focus:outline-none focus-visible:ring-2 focus-visible:ring-ring',
          'disabled:cursor-not-allowed disabled:opacity-50',
        )}
      >
        <span className={cn('truncate', !texto && 'text-fg-muted')}>{texto || placeholder}</span>
        <ChevronsUpDown className="size-4 shrink-0 text-fg-muted" />
      </button>

      <SeletorRegistro<ClienteResumo>
        aberto={aberto}
        onAbrir={setAberto}
        titulo="Selecionar cliente"
        placeholderBusca="Buscar por nome ou documento…"
        colunas={COLUNAS}
        buscar={buscar}
        getId={(c) => c.id}
        storageKey="seletor.cliente.colunas"
        onSelecionar={(c) => {
          setRotulo(c.nome)
          onChange(c.id, c.nome)
        }}
      />
    </>
  )
}
