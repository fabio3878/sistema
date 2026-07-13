import { useMemo, useRef, useState } from 'react'
import { ChevronsUpDown, UserPlus } from 'lucide-react'
import { cn } from '@/lib/utils'
import { FOCAVEIS } from '@/lib/enter-como-tab'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SeletorRegistro, type ColunaSeletor } from '@/components/ui/seletor-registro'
import { useAuth, useSessao } from '@/lib/auth'
import { podeVer } from '@/lib/sessao'
import { ClienteDrawer } from './cliente-drawer'
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
  /** Mostra o botão "+" para cadastrar um cliente na hora (default true, se tiver permissão). */
  permitirCriar?: boolean
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
export function SeletorCliente({ value, onChange, disabled, placeholder = 'Selecione o cliente…', rotuloInicial, permitirCriar = true }: Props) {
  const { requisitar } = useAuth()
  const sessao = useSessao()
  const [aberto, setAberto] = useState(false)
  const [criando, setCriando] = useState(false)
  const [rotulo, setRotulo] = useState(rotuloInicial ?? '')
  const gatilhoRef = useRef<HTMLButtonElement>(null)
  // Distingue, no fechamento do cadastro de cliente, "criou → avança o foco" de "cancelou → volta ao gatilho".
  const criouRef = useRef(false)

  // "+" só quando permitido, com permissão de criar e o campo habilitado (em edição o cliente é fixo).
  const podeCriar = permitirCriar && !disabled && podeVer(sessao, 'cad', 'cad.cliente.criar')

  /** Após escolher, avança o foco para o próximo campo do formulário (em vez de voltar ao gatilho). */
  const avancarFoco = () => {
    const g = gatilhoRef.current
    const form = g?.closest('form')
    if (!g || !form) return
    const itens = Array.from(form.querySelectorAll<HTMLElement>(FOCAVEIS)).filter((el) => el === g || el.offsetParent !== null)
    const prox = itens[itens.indexOf(g) + 1]
    if (!prox) return
    prox.focus()
    if (prox instanceof HTMLInputElement && /^(text|number|search|tel|url|email|password|)$/.test(prox.type)) prox.select()
  }

  const buscar = useMemo(
    () => (termo: string, signal?: AbortSignal) =>
      listarClientes(requisitar, { busca: termo || undefined, situacao: 'ativo' }, signal),
    [requisitar],
  )

  const texto = rotulo || rotuloInicial || (value ? '' : '')

  return (
    <>
      <div className="flex items-center gap-2">
        <button
          ref={gatilhoRef}
          type="button"
          disabled={disabled}
          onClick={() => setAberto(true)}
          className={cn(
            'flex h-9 w-full min-w-0 items-center justify-between gap-2 rounded-md border border-border bg-surface px-3 text-body',
            'transition-colors duration-fast focus:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            'disabled:cursor-not-allowed disabled:opacity-50',
          )}
        >
          <span className={cn('truncate', !texto && 'text-fg-muted')}>{texto || placeholder}</span>
          <ChevronsUpDown className="size-4 shrink-0 text-fg-muted" />
        </button>
        {podeCriar && (
          <Button
            type="button"
            variant="secondary"
            size="icon"
            // Fora da sequência de Tab/Enter (FOCAVEIS ignora tabindex=-1): não atrapalha a navegação por teclado.
            tabIndex={-1}
            aria-label="Cadastrar novo cliente"
            title="Cadastrar novo cliente"
            onClick={() => setCriando(true)}
          >
            <UserPlus />
          </Button>
        )}
      </div>

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
        // No fechamento (momento em que o Radix devolveria o foco): avança ao selecionar; volta ao gatilho ao cancelar.
        aoFechar={(selecionou) => (selecionou ? avancarFoco() : gatilhoRef.current?.focus())}
      />

      {podeCriar && (
        <ClienteDrawer
          aberto={criando}
          onAbrir={setCriando}
          clienteId={null}
          onCriado={(c) => {
            setRotulo(c.nome)
            onChange(c.id, c.nome)
            criouRef.current = true
          }}
          // Ao fechar: criou → avança para o próximo campo; cancelou → volta ao gatilho do cliente.
          onCloseAutoFocus={(e) => {
            e.preventDefault()
            if (criouRef.current) avancarFoco()
            else gatilhoRef.current?.focus()
            criouRef.current = false
          }}
        />
      )}
    </>
  )
}
