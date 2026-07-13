import { useEffect, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Combobox } from '@/components/ui/combobox'
import { Drawer, DrawerCancelar, useDrawerMaximizado } from '@/components/ui/drawer'
import { cn } from '@/lib/utils'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { enterComoTab } from '@/lib/enter-como-tab'
import { listarUnidades } from '@/modulos/cadastros/unidades'
import { BotaoHistorico } from '@/modulos/auditoria/botao-historico'
import { atualizarServico, criarServico, obterServico } from './api'
import type { ServicoEntrada } from './tipos'

const schema = z.object({
  codigoInterno: z.string().optional(),
  descricao: z.string().min(1, 'Informe a descrição'),
  unidade: z.string().min(1, 'Selecione a unidade'),
  precoVenda: z.string().refine((v) => v !== '' && Number(v) >= 0, 'Preço inválido'),
})

type Campos = z.infer<typeof schema>

const VAZIO: Campos = { codigoInterno: '', descricao: '', unidade: '', precoVenda: '' }

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  servicoId: string | null
}

export function ServicoDrawer({ aberto, onAbrir, servicoId }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)
  const editando = servicoId !== null

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Campos>({ resolver: zodResolver(schema), defaultValues: VAZIO })

  const unidades = useQuery({
    queryKey: ['unidades'],
    queryFn: ({ signal }) => listarUnidades(requisitar, signal),
    enabled: aberto,
    staleTime: Infinity,
  })

  const detalhe = useQuery({
    queryKey: ['servico', servicoId],
    queryFn: ({ signal }) => obterServico(requisitar, servicoId!, signal),
    enabled: aberto && editando,
  })

  useEffect(() => {
    if (!aberto) return
    setErro(null)
    if (!editando) {
      reset(VAZIO)
    } else if (detalhe.data) {
      const s = detalhe.data
      reset({
        codigoInterno: s.codigoInterno ?? '',
        descricao: s.descricao,
        unidade: s.unidade,
        precoVenda: String(s.precoVenda),
      })
    }
  }, [aberto, editando, detalhe.data, reset])

  const salvar = useMutation({
    mutationFn: async (dados: ServicoEntrada) => {
      if (editando) await atualizarServico(requisitar, servicoId!, dados)
      else await criarServico(requisitar, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['servicos'] })
      if (editando) await qc.invalidateQueries({ queryKey: ['servico', servicoId] })
      onAbrir(false)
    },
    onError: (e) => {
      const msg = e instanceof ApiError ? e.message : 'Não foi possível salvar o serviço.'
      setErro(msg)
      if (/código interno|codigo interno/i.test(msg)) setError('codigoInterno', { message: msg })
      else if (/unidade/i.test(msg)) setError('unidade', { message: msg })
    },
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    const dados: ServicoEntrada = {
      descricao: v.descricao.trim(),
      precoVenda: Number(v.precoVenda),
      unidade: v.unidade,
      codigoInterno: v.codigoInterno?.trim() || null,
    }
    salvar.mutate(dados)
  })

  const carregando = editando && detalhe.isLoading
  const opcoesUnidade = (unidades.data ?? []).map((u) => ({
    value: u.sigla,
    label: `${u.sigla} — ${u.descricao}`,
  }))

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo={editando ? 'Editar serviço' : 'Novo serviço'}
      descricao={editando ? 'Altere os dados e salve.' : 'Preencha os dados do serviço.'}
      rodape={
        <>
          {editando && <BotaoHistorico entidade="Servico" registroId={servicoId!} />}
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-servico" disabled={isSubmitting || carregando}>
            {(isSubmitting || salvar.isPending) && <Loader2 className="animate-spin" />}
            Salvar
          </Button>
        </>
      }
    >
      {carregando ? (
        <div className="grid place-items-center py-16 text-fg-muted">
          <Loader2 className="size-6 animate-spin" />
        </div>
      ) : (
        <form id="form-servico" onSubmit={onSubmit} onKeyDown={enterComoTab} className="space-y-6" noValidate>
          {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

          <Secao titulo="Identificação">
            <GridCampos>
              <Campo className="col-span-full" rotulo="Descrição" erro={errors.descricao?.message}>
                <Input {...register('descricao')} />
              </Campo>
              <Campo rotulo="Código interno (opcional)" erro={errors.codigoInterno?.message}>
                <Input {...register('codigoInterno')} placeholder="Ex.: SERV-01" />
              </Campo>
              <Campo rotulo="Unidade" erro={errors.unidade?.message}>
                <Controller
                  control={control}
                  name="unidade"
                  render={({ field }) => (
                    <Combobox
                      value={field.value ?? ''}
                      onChange={(v) => field.onChange(v)}
                      options={opcoesUnidade}
                      placeholder="Selecione…"
                      buscaPlaceholder="Buscar unidade…"
                    />
                  )}
                />
              </Campo>
              <Campo rotulo="Preço" erro={errors.precoVenda?.message}>
                <Input type="number" step="0.01" min="0" {...register('precoVenda')} />
              </Campo>
            </GridCampos>
          </Secao>
        </form>
      )}
    </Drawer>
  )
}

/** Grade de campos que reflowa: 2 colunas no drawer lateral, 3 quando maximizado. */
function GridCampos({ className, children }: { className?: string; children: React.ReactNode }) {
  const maximizado = useDrawerMaximizado()
  return <div className={cn('grid gap-4', maximizado ? 'grid-cols-3' : 'grid-cols-2', className)}>{children}</div>
}

function Secao({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <h4 className="text-small font-semibold uppercase tracking-wide text-fg-muted">{titulo}</h4>
      {children}
    </section>
  )
}

function Campo({
  rotulo,
  erro,
  className,
  children,
}: {
  rotulo: string
  erro?: string
  className?: string
  children: React.ReactNode
}) {
  return (
    <div className={`space-y-1.5 ${className ?? ''}`}>
      <label className="text-small font-medium text-fg">{rotulo}</label>
      {children}
      {erro && <p className="text-caption text-danger">{erro}</p>}
    </div>
  )
}
