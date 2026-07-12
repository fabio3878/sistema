import { useEffect, useState } from 'react'
import { useForm, Controller, type Control } from 'react-hook-form'
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
import { listarUnidades } from '@/modulos/cadastros/unidades'
import { BotaoHistorico } from '@/modulos/auditoria/botao-historico'
import { atualizarProduto, criarProduto, obterProduto } from './api'
import type { ProdutoEntrada, OrigemMercadoria } from './tipos'

const soDigitos = (v: string) => (v ?? '').replace(/\D/g, '')

/** Origem da mercadoria (orig da NF-e) — rótulos amigáveis. */
const ORIGENS: { valor: OrigemMercadoria; rotulo: string }[] = [
  { valor: 'Nacional', rotulo: '0 — Nacional' },
  { valor: 'EstrangeiraImportacaoDireta', rotulo: '1 — Estrangeira (importação direta)' },
  { valor: 'EstrangeiraAdquiridaMercadoInterno', rotulo: '2 — Estrangeira (mercado interno)' },
  { valor: 'NacionalImportacaoSuperior40', rotulo: '3 — Nacional (importação > 40%)' },
  { valor: 'NacionalProcessosBasicos', rotulo: '4 — Nacional (processos básicos)' },
  { valor: 'NacionalImportacaoInferiorIgual40', rotulo: '5 — Nacional (importação ≤ 40%)' },
  { valor: 'EstrangeiraImportacaoDiretaSemSimilar', rotulo: '6 — Estrangeira (direta, sem similar)' },
  { valor: 'EstrangeiraAdquiridaMercadoInternoSemSimilar', rotulo: '7 — Estrangeira (interno, sem similar)' },
  { valor: 'NacionalImportacaoSuperior70', rotulo: '8 — Nacional (importação > 70%)' },
]

const schema = z.object({
  codigoInterno: z.string().optional(),
  descricao: z.string().min(1, 'Informe a descrição'),
  codigoBarras: z.string().optional(),
  unidade: z.string().min(1, 'Selecione a unidade'),
  ncm: z.string().refine((v) => soDigitos(v).length === 8, 'NCM deve ter 8 dígitos'),
  cest: z
    .string()
    .optional()
    .refine((v) => !v || soDigitos(v).length === 7, 'CEST deve ter 7 dígitos'),
  origem: z.enum([
    'Nacional',
    'EstrangeiraImportacaoDireta',
    'EstrangeiraAdquiridaMercadoInterno',
    'NacionalImportacaoSuperior40',
    'NacionalProcessosBasicos',
    'NacionalImportacaoInferiorIgual40',
    'EstrangeiraImportacaoDiretaSemSimilar',
    'EstrangeiraAdquiridaMercadoInternoSemSimilar',
    'NacionalImportacaoSuperior70',
  ]),
  precoVenda: z.string().refine((v) => v !== '' && Number(v) >= 0, 'Preço inválido'),
})

type Campos = z.infer<typeof schema>

const VAZIO: Campos = {
  codigoInterno: '',
  descricao: '',
  codigoBarras: '',
  unidade: '',
  ncm: '',
  cest: '',
  origem: 'Nacional',
  precoVenda: '',
}

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  produtoId: string | null
}

export function ProdutoDrawer({ aberto, onAbrir, produtoId }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)
  const editando = produtoId !== null

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
    queryKey: ['produto', produtoId],
    queryFn: ({ signal }) => obterProduto(requisitar, produtoId!, signal),
    enabled: aberto && editando,
  })

  useEffect(() => {
    if (!aberto) return
    setErro(null)
    if (!editando) {
      reset(VAZIO)
    } else if (detalhe.data) {
      const p = detalhe.data
      reset({
        codigoInterno: p.codigoInterno ?? '',
        descricao: p.descricao,
        codigoBarras: p.codigoBarras ?? '',
        unidade: p.unidade,
        ncm: p.ncm,
        cest: p.cest ?? '',
        origem: p.origem,
        precoVenda: String(p.precoVenda),
      })
    }
  }, [aberto, editando, detalhe.data, reset])

  const salvar = useMutation({
    mutationFn: async (dados: ProdutoEntrada) => {
      if (editando) await atualizarProduto(requisitar, produtoId!, dados)
      else await criarProduto(requisitar, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['produtos'] })
      if (editando) await qc.invalidateQueries({ queryKey: ['produto', produtoId] })
      onAbrir(false)
    },
    onError: (e) => {
      const msg = e instanceof ApiError ? e.message : 'Não foi possível salvar o produto.'
      setErro(msg)
      if (/código interno|codigo interno/i.test(msg)) setError('codigoInterno', { message: msg })
      else if (/ncm/i.test(msg)) setError('ncm', { message: msg })
      else if (/cest/i.test(msg)) setError('cest', { message: msg })
      else if (/unidade/i.test(msg)) setError('unidade', { message: msg })
    },
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    const dados: ProdutoEntrada = {
      descricao: v.descricao.trim(),
      ncm: soDigitos(v.ncm),
      precoVenda: Number(v.precoVenda),
      unidade: v.unidade,
      origem: v.origem,
      codigoInterno: v.codigoInterno?.trim() || null,
      codigoBarras: v.codigoBarras?.trim() || null,
      cest: v.cest ? soDigitos(v.cest) : null,
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
      titulo={editando ? 'Editar produto' : 'Novo produto'}
      descricao={editando ? 'Altere os dados e salve.' : 'Preencha os dados do produto.'}
      rodape={
        <>
          {editando && <BotaoHistorico entidade="Produto" registroId={produtoId!} />}
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-produto" disabled={isSubmitting || carregando}>
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
        <form id="form-produto" onSubmit={onSubmit} className="space-y-6" noValidate>
          {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

          <Secao titulo="Identificação">
            <GridCampos>
              <Campo className="col-span-full" rotulo="Descrição" erro={errors.descricao?.message}>
                <Input {...register('descricao')} />
              </Campo>
              <Campo rotulo="Código interno (opcional)" erro={errors.codigoInterno?.message}>
                <Input {...register('codigoInterno')} placeholder="Ex.: REFRI-350" />
              </Campo>
              <Campo rotulo="Código de barras" erro={errors.codigoBarras?.message}>
                <Input {...register('codigoBarras')} />
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
              <Campo rotulo="Preço de venda" erro={errors.precoVenda?.message}>
                <Input type="number" step="0.01" min="0" {...register('precoVenda')} />
              </Campo>
            </GridCampos>
          </Secao>

          <Secao titulo="Fiscal">
            <GridCampos>
              <CampoMascarado control={control} name="ncm" rotulo="NCM" erro={errors.ncm?.message} tamanho={8} />
              <CampoMascarado control={control} name="cest" rotulo="CEST (opcional)" erro={errors.cest?.message} tamanho={7} />
              <Campo className="col-span-full" rotulo="Origem da mercadoria" erro={errors.origem?.message}>
                <Controller
                  control={control}
                  name="origem"
                  render={({ field }) => (
                    <select
                      className="h-9 w-full rounded-md border border-border bg-surface px-2 text-body text-fg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                      value={field.value}
                      onChange={(e) => field.onChange(e.target.value)}
                    >
                      {ORIGENS.map((o) => (
                        <option key={o.valor} value={o.valor}>
                          {o.rotulo}
                        </option>
                      ))}
                    </select>
                  )}
                />
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

/** Input que só aceita dígitos até um tamanho (NCM/CEST). */
function CampoMascarado({
  control,
  name,
  rotulo,
  erro,
  tamanho,
}: {
  control: Control<Campos>
  name: 'ncm' | 'cest'
  rotulo: string
  erro?: string
  tamanho: number
}) {
  return (
    <Campo rotulo={rotulo} erro={erro}>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <Input
            inputMode="numeric"
            value={(field.value as string) ?? ''}
            onChange={(e) => field.onChange(soDigitos(e.target.value).slice(0, tamanho))}
          />
        )}
      />
    </Campo>
  )
}
