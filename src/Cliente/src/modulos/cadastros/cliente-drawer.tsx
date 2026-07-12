import { useEffect, useState } from 'react'
import {
  useForm,
  useFieldArray,
  useWatch,
  Controller,
  type Control,
  type UseFormRegister,
  type UseFormSetValue,
  type FieldErrors,
} from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import { Combobox } from '@/components/ui/combobox'
import { Drawer, DrawerCancelar, useDrawerMaximizado } from '@/components/ui/drawer'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { ApiError } from '@/lib/api'
import { useAuth } from '@/lib/auth'
import { BotaoHistorico } from '@/modulos/auditoria/botao-historico'
import { atualizarCliente, criarCliente, listarEstados, listarMunicipios, obterCliente } from './api'
import { mascararCep, mascararDocumento, mascararTelefone, soDigitos, validarCnpj, validarCpf } from './formato'
import type { ClienteEntrada, TipoPessoa } from './tipos'

const enderecoSchema = z.object({
  id: z.string().optional(),
  tipo: z.enum(['Principal', 'Cobranca', 'Entrega']),
  cep: z.string().refine((v) => soDigitos(v).length === 8, 'CEP deve ter 8 dígitos'),
  logradouro: z.string().min(1, 'Obrigatório'),
  numero: z.string().min(1, 'Obrigatório'),
  complemento: z.string().optional(),
  bairro: z.string().min(1, 'Obrigatório'),
  municipio: z.string().min(1, 'Selecione a cidade'),
  uf: z.string().refine((v) => /^[A-Za-z]{2}$/.test(v.trim()), 'UF'),
  codigoIbgeMunicipio: z.string().refine((v) => soDigitos(v).length === 7, 'Selecione a cidade'),
})

const schema = z
  .object({
    tipoPessoa: z.enum(['Fisica', 'Juridica']),
    nome: z.string().min(1, 'Informe o nome'),
    documento: z.string().min(1, 'Informe o documento'),
    indicadorIe: z.enum(['Contribuinte', 'Isento', 'NaoContribuinte']),
    nomeFantasia: z.string().optional(),
    email: z.string().optional(),
    emailFinanceiro: z.string().optional(),
    telefone: z.string().optional(),
    celular: z.string().optional(),
    whatsapp: z.string().optional(),
    site: z.string().optional(),
    dataNascimento: z.string().optional(),
    rg: z.string().optional(),
    orgaoEmissorRg: z.string().optional(),
    inscricaoEstadual: z.string().optional(),
    inscricaoMunicipal: z.string().optional(),
    regimeTributario: z.enum(['SimplesNacional', 'SimplesExcessoSublimite', 'Normal', '']).optional(),
    limiteCredito: z.string().optional(),
    origem: z.string().optional(),
    preferencias: z.string().optional(),
    observacoes: z.string().optional(),
    aceitaEmail: z.boolean(),
    aceitaSms: z.boolean(),
    aceitaWhatsapp: z.boolean(),
    aceitaLigacoes: z.boolean(),
    aceitouTermosLgpd: z.boolean(),
    enderecos: z.array(enderecoSchema),
  })
  .superRefine((d, ctx) => {
    if (d.tipoPessoa === 'Fisica' && !validarCpf(d.documento))
      ctx.addIssue({ path: ['documento'], code: 'custom', message: 'CPF inválido' })
    if (d.tipoPessoa === 'Juridica' && !validarCnpj(d.documento))
      ctx.addIssue({ path: ['documento'], code: 'custom', message: 'CNPJ inválido' })
    if (d.indicadorIe === 'Contribuinte' && !(d.inscricaoEstadual ?? '').trim())
      ctx.addIssue({ path: ['inscricaoEstadual'], code: 'custom', message: 'Contribuinte exige Inscrição Estadual' })
  })

type Campos = z.infer<typeof schema>

const VAZIO: Campos = {
  tipoPessoa: 'Fisica',
  nome: '',
  documento: '',
  indicadorIe: 'NaoContribuinte',
  nomeFantasia: '',
  email: '',
  emailFinanceiro: '',
  telefone: '',
  celular: '',
  whatsapp: '',
  site: '',
  dataNascimento: '',
  rg: '',
  orgaoEmissorRg: '',
  inscricaoEstadual: '',
  inscricaoMunicipal: '',
  regimeTributario: '',
  limiteCredito: '',
  origem: '',
  preferencias: '',
  observacoes: '',
  aceitaEmail: false,
  aceitaSms: false,
  aceitaWhatsapp: false,
  aceitaLigacoes: false,
  aceitouTermosLgpd: false,
  enderecos: [],
}

interface Props {
  aberto: boolean
  onAbrir: (v: boolean) => void
  clienteId: string | null
}

export function ClienteDrawer({ aberto, onAbrir, clienteId }: Props) {
  const { requisitar } = useAuth()
  const qc = useQueryClient()
  const [erro, setErro] = useState<string | null>(null)
  const [aceiteAnterior, setAceiteAnterior] = useState<string | null>(null)
  const editando = clienteId !== null

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Campos>({ resolver: zodResolver(schema), defaultValues: VAZIO })

  const tipo = useWatch({ control, name: 'tipoPessoa' })
  const enderecos = useFieldArray({ control, name: 'enderecos' })

  const estados = useQuery({
    queryKey: ['estados'],
    queryFn: ({ signal }) => listarEstados(requisitar, signal),
    enabled: aberto,
    staleTime: Infinity,
  })

  const detalhe = useQuery({
    queryKey: ['cliente', clienteId],
    queryFn: ({ signal }) => obterCliente(requisitar, clienteId!, signal),
    enabled: aberto && editando,
  })

  useEffect(() => {
    if (!aberto) return
    setErro(null)
    setAceiteAnterior(null)
    if (!editando) {
      reset(VAZIO)
    } else if (detalhe.data) {
      const c = detalhe.data
      setAceiteAnterior(c.dataAceiteLgpd)
      reset({
        tipoPessoa: c.tipoPessoa,
        nome: c.nome,
        documento: mascararDocumento(c.documento, c.tipoPessoa),
        indicadorIe: c.indicadorIe,
        nomeFantasia: c.nomeFantasia ?? '',
        email: c.email ?? '',
        emailFinanceiro: c.emailFinanceiro ?? '',
        telefone: mascararTelefone(c.telefone ?? ''),
        celular: mascararTelefone(c.celular ?? ''),
        whatsapp: mascararTelefone(c.whatsapp ?? ''),
        site: c.site ?? '',
        dataNascimento: c.dataNascimento ?? '',
        rg: c.rg ?? '',
        orgaoEmissorRg: c.orgaoEmissorRg ?? '',
        inscricaoEstadual: c.inscricaoEstadual ?? '',
        inscricaoMunicipal: c.inscricaoMunicipal ?? '',
        regimeTributario: c.regimeTributario ?? '',
        limiteCredito: c.limiteCredito != null ? String(c.limiteCredito) : '',
        origem: c.origem ?? '',
        preferencias: c.preferencias ?? '',
        observacoes: c.observacoes ?? '',
        aceitaEmail: c.aceitaEmail,
        aceitaSms: c.aceitaSms,
        aceitaWhatsapp: c.aceitaWhatsapp,
        aceitaLigacoes: c.aceitaLigacoes,
        aceitouTermosLgpd: c.aceitouTermosLgpd,
        enderecos: c.enderecos.map((e) => ({
          id: e.id,
          tipo: e.tipo,
          cep: mascararCep(e.cep),
          logradouro: e.logradouro,
          numero: e.numero,
          complemento: e.complemento ?? '',
          bairro: e.bairro,
          municipio: e.municipio,
          uf: e.uf,
          codigoIbgeMunicipio: e.codigoIbgeMunicipio,
        })),
      })
    }
  }, [aberto, editando, detalhe.data, reset])

  const salvar = useMutation({
    mutationFn: async (dados: ClienteEntrada) => {
      if (editando) await atualizarCliente(requisitar, clienteId!, dados)
      else await criarCliente(requisitar, dados)
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['clientes'] })
      if (editando) await qc.invalidateQueries({ queryKey: ['cliente', clienteId] })
      onAbrir(false)
    },
    onError: (e) => {
      const msg = e instanceof ApiError ? e.message : 'Não foi possível salvar o cliente.'
      setErro(msg)
      if (/inscri/i.test(msg)) setError('inscricaoEstadual', { message: msg })
      else if (/documento|cpf|cnpj/i.test(msg)) setError('documento', { message: msg })
      else if (/nome/i.test(msg)) setError('nome', { message: msg })
      else if (/e-?mail/i.test(msg)) setError('email', { message: msg })
    },
  })

  const onSubmit = handleSubmit((v) => {
    setErro(null)
    const pj = v.tipoPessoa === 'Juridica'
    const dados: ClienteEntrada = {
      tipoPessoa: v.tipoPessoa,
      nome: v.nome,
      documento: soDigitos(v.documento),
      indicadorIe: v.indicadorIe,
      nomeFantasia: pj ? v.nomeFantasia || null : null,
      email: v.email || null,
      emailFinanceiro: v.emailFinanceiro || null,
      telefone: v.telefone ? soDigitos(v.telefone) : null,
      celular: v.celular ? soDigitos(v.celular) : null,
      whatsapp: v.whatsapp ? soDigitos(v.whatsapp) : null,
      site: v.site || null,
      dataNascimento: !pj && v.dataNascimento ? v.dataNascimento : null,
      rg: !pj ? v.rg || null : null,
      orgaoEmissorRg: !pj ? v.orgaoEmissorRg || null : null,
      inscricaoEstadual: pj ? v.inscricaoEstadual || null : null,
      inscricaoMunicipal: pj ? v.inscricaoMunicipal || null : null,
      regimeTributario: pj && v.regimeTributario ? v.regimeTributario : null,
      limiteCredito: v.limiteCredito ? Number(v.limiteCredito) : null,
      origem: v.origem || null,
      preferencias: v.preferencias || null,
      observacoes: v.observacoes || null,
      aceitaEmail: v.aceitaEmail,
      aceitaSms: v.aceitaSms,
      aceitaWhatsapp: v.aceitaWhatsapp,
      aceitaLigacoes: v.aceitaLigacoes,
      aceitouTermosLgpd: v.aceitouTermosLgpd,
      dataAceiteLgpd: v.aceitouTermosLgpd ? (aceiteAnterior ?? new Date().toISOString()) : null,
      enderecos: v.enderecos.map((e) => ({
        id: e.id ?? null,
        tipo: e.tipo,
        cep: soDigitos(e.cep),
        logradouro: e.logradouro,
        numero: e.numero,
        complemento: e.complemento || null,
        bairro: e.bairro,
        municipio: e.municipio,
        uf: e.uf,
        codigoIbgeMunicipio: e.codigoIbgeMunicipio,
      })),
    }
    salvar.mutate(dados)
  })

  const carregando = editando && detalhe.isLoading
  const pj = tipo === 'Juridica'
  const opcoesUf = (estados.data ?? []).map((e) => ({ value: e.uf, label: `${e.uf} — ${e.nome}` }))

  return (
    <Drawer
      aberto={aberto}
      onAbrir={onAbrir}
      titulo={editando ? 'Editar cliente' : 'Novo cliente'}
      descricao={editando ? 'Altere os dados e salve.' : 'Preencha os dados do cliente.'}
      rodape={
        <>
          {editando && <BotaoHistorico entidade="Cliente" registroId={clienteId!} />}
          <DrawerCancelar onAbrir={onAbrir} />
          <Button type="submit" form="form-cliente" disabled={isSubmitting || carregando}>
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
        <form id="form-cliente" onSubmit={onSubmit} className="space-y-6" noValidate>
          {erro && <div className="rounded-md bg-danger-bg px-3 py-2 text-small text-danger">{erro}</div>}

          {/* Tipo de pessoa — segmented */}
          <Controller
            control={control}
            name="tipoPessoa"
            render={({ field }) => (
              <div className="inline-flex rounded-md border border-border p-0.5">
                {(['Fisica', 'Juridica'] as TipoPessoa[]).map((t) => (
                  <button
                    key={t}
                    type="button"
                    onClick={() => field.onChange(t)}
                    className={
                      'rounded px-4 py-1.5 text-small font-medium transition-colors ' +
                      (field.value === t ? 'bg-primary text-primary-fg' : 'text-fg-muted hover:text-fg')
                    }
                  >
                    {t === 'Fisica' ? 'Pessoa física' : 'Pessoa jurídica'}
                  </button>
                ))}
              </div>
            )}
          />

          <Secao titulo="Identificação">
            <GridCampos>
              <Campo className="col-span-full" rotulo={pj ? 'Razão social' : 'Nome'} erro={errors.nome?.message}>
                <Input {...register('nome')} />
              </Campo>
              <CampoMascarado
                control={control}
                name="documento"
                rotulo={pj ? 'CNPJ' : 'CPF'}
                erro={errors.documento?.message}
                mascara={(v) => mascararDocumento(v, pj ? 'Juridica' : 'Fisica')}
              />
              {pj ? (
                <Campo rotulo="Nome fantasia" erro={errors.nomeFantasia?.message}>
                  <Input {...register('nomeFantasia')} />
                </Campo>
              ) : (
                <Campo rotulo="Data de nascimento" erro={errors.dataNascimento?.message}>
                  <Input type="date" {...register('dataNascimento')} />
                </Campo>
              )}
              {!pj && (
                <>
                  <Campo rotulo="RG" erro={errors.rg?.message}>
                    <Input {...register('rg')} />
                  </Campo>
                  <Campo rotulo="Órgão emissor" erro={errors.orgaoEmissorRg?.message}>
                    <Input {...register('orgaoEmissorRg')} />
                  </Campo>
                </>
              )}
            </GridCampos>
          </Secao>

          <Secao titulo="Contato">
            <GridCampos>
              <Campo rotulo="E-mail" erro={errors.email?.message}>
                <Input type="email" {...register('email')} />
              </Campo>
              <Campo rotulo="E-mail financeiro" erro={errors.emailFinanceiro?.message}>
                <Input type="email" {...register('emailFinanceiro')} />
              </Campo>
              <CampoMascarado control={control} name="telefone" rotulo="Telefone" mascara={mascararTelefone} />
              <CampoMascarado control={control} name="celular" rotulo="Celular" mascara={mascararTelefone} />
              <CampoMascarado control={control} name="whatsapp" rotulo="WhatsApp" mascara={mascararTelefone} />
              <Campo rotulo="Site" erro={errors.site?.message}>
                <Input placeholder="https://" {...register('site')} />
              </Campo>
            </GridCampos>
          </Secao>

          {pj && (
            <Secao titulo="Fiscal">
              <GridCampos>
                <Campo rotulo="Indicador de IE" erro={errors.indicadorIe?.message}>
                  <SelectRHF
                    control={control}
                    name="indicadorIe"
                    opcoes={[
                      { valor: 'NaoContribuinte', rotulo: 'Não contribuinte' },
                      { valor: 'Isento', rotulo: 'Isento' },
                      { valor: 'Contribuinte', rotulo: 'Contribuinte' },
                    ]}
                  />
                </Campo>
                <Campo rotulo="Regime tributário" erro={errors.regimeTributario?.message}>
                  <SelectRHF
                    control={control}
                    name="regimeTributario"
                    placeholder="—"
                    opcoes={[
                      { valor: '', rotulo: '—' },
                      { valor: 'SimplesNacional', rotulo: 'Simples Nacional' },
                      { valor: 'SimplesExcessoSublimite', rotulo: 'Simples (excesso sublimite)' },
                      { valor: 'Normal', rotulo: 'Normal' },
                    ]}
                  />
                </Campo>
                <Campo rotulo="Inscrição estadual" erro={errors.inscricaoEstadual?.message}>
                  <Input {...register('inscricaoEstadual')} />
                </Campo>
                <Campo rotulo="Inscrição municipal" erro={errors.inscricaoMunicipal?.message}>
                  <Input {...register('inscricaoMunicipal')} />
                </Campo>
              </GridCampos>
            </Secao>
          )}

          <Secao
            titulo="Endereços"
            acao={
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() =>
                  enderecos.append({
                    tipo: 'Principal',
                    cep: '',
                    logradouro: '',
                    numero: '',
                    complemento: '',
                    bairro: '',
                    municipio: '',
                    uf: '',
                    codigoIbgeMunicipio: '',
                  })
                }
              >
                <Plus /> Adicionar
              </Button>
            }
          >
            {enderecos.fields.length === 0 && (
              <p className="text-small text-fg-muted">Nenhum endereço. Use "Adicionar".</p>
            )}
            <div className="space-y-4">
              {enderecos.fields.map((campo, i) => (
                <EnderecoRow
                  key={campo.id}
                  i={i}
                  control={control}
                  register={register}
                  setValue={setValue}
                  errors={errors}
                  opcoesUf={opcoesUf}
                  onRemover={() => enderecos.remove(i)}
                />
              ))}
            </div>
          </Secao>

          <Secao titulo="Adicionais">
            <GridCampos>
              <Campo rotulo="Origem / como conheceu" erro={errors.origem?.message}>
                <Input {...register('origem')} />
              </Campo>
              <Campo rotulo="Limite de crédito" erro={errors.limiteCredito?.message}>
                <Input type="number" step="0.01" min="0" {...register('limiteCredito')} />
              </Campo>
              <Campo className="col-span-full" rotulo="Preferências" erro={errors.preferencias?.message}>
                <Textarea rows={2} {...register('preferencias')} />
              </Campo>
              <Campo className="col-span-full" rotulo="Observações" erro={errors.observacoes?.message}>
                <Textarea {...register('observacoes')} />
              </Campo>
            </GridCampos>
          </Secao>

          <Secao titulo="Marketing / LGPD">
            <GridCampos className="gap-3">
              <CheckboxRHF control={control} name="aceitaEmail" label="Aceita e-mail" />
              <CheckboxRHF control={control} name="aceitaSms" label="Aceita SMS" />
              <CheckboxRHF control={control} name="aceitaWhatsapp" label="Aceita WhatsApp" />
              <CheckboxRHF control={control} name="aceitaLigacoes" label="Aceita ligações" />
              <CheckboxRHF control={control} name="aceitouTermosLgpd" label="Aceitou os termos (LGPD)" />
            </GridCampos>
            {aceiteAnterior && (
              <p className="text-caption text-fg-muted">
                Aceite registrado em {new Date(aceiteAnterior).toLocaleDateString('pt-BR')}.
              </p>
            )}
          </Secao>
        </form>
      )}
    </Drawer>
  )
}

/** Uma linha de endereço: UF (Select) + Cidade (Combobox IBGE) + CEP com autofill via ViaCEP. */
function EnderecoRow({
  i,
  control,
  register,
  setValue,
  errors,
  opcoesUf,
  onRemover,
}: {
  i: number
  control: Control<Campos>
  register: UseFormRegister<Campos>
  setValue: UseFormSetValue<Campos>
  errors: FieldErrors<Campos>
  opcoesUf: { value: string; label: string }[]
  onRemover: () => void
}) {
  const { requisitar } = useAuth()
  const uf = useWatch({ control, name: `enderecos.${i}.uf` })
  const ibge = useWatch({ control, name: `enderecos.${i}.codigoIbgeMunicipio` })

  const municipios = useQuery({
    queryKey: ['municipios', uf],
    queryFn: ({ signal }) => listarMunicipios(requisitar, uf!, signal),
    enabled: !!uf && /^[A-Za-z]{2}$/.test(uf),
    staleTime: Infinity,
  })
  const opcoesCidade = (municipios.data ?? []).map((m) => ({ value: m.codigoIbge, label: m.nome }))

  async function autofillCep(cep: string) {
    const d = soDigitos(cep)
    if (d.length !== 8) return
    try {
      const r = await fetch(`https://viacep.com.br/ws/${d}/json/`)
      const j = await r.json()
      if (j.erro) return
      if (j.logradouro) setValue(`enderecos.${i}.logradouro`, j.logradouro)
      if (j.bairro) setValue(`enderecos.${i}.bairro`, j.bairro)
      if (j.uf) setValue(`enderecos.${i}.uf`, j.uf)
      if (j.localidade) setValue(`enderecos.${i}.municipio`, j.localidade)
      if (j.ibge) setValue(`enderecos.${i}.codigoIbgeMunicipio`, j.ibge)
    } catch {
      /* offline: preenchimento manual */
    }
  }

  return (
    <div className="rounded-lg border border-border p-4">
      <div className="mb-3 flex items-center justify-between">
        <SelectRHF
          control={control}
          name={`enderecos.${i}.tipo`}
          className="w-48"
          opcoes={[
            { valor: 'Principal', rotulo: 'Principal' },
            { valor: 'Cobranca', rotulo: 'Cobrança' },
            { valor: 'Entrega', rotulo: 'Entrega' },
          ]}
        />
        <Button type="button" variant="ghost" size="icon" aria-label="Remover endereço" onClick={onRemover}>
          <Trash2 />
        </Button>
      </div>
      <div className="grid grid-cols-6 gap-3">
        <Campo className="col-span-2" rotulo="CEP" erro={errArr(errors, i, 'cep')}>
          <Controller
            control={control}
            name={`enderecos.${i}.cep`}
            render={({ field }) => (
              <Input
                value={field.value ?? ''}
                onChange={(e) => field.onChange(mascararCep(e.target.value))}
                onBlur={() => autofillCep(field.value ?? '')}
                placeholder="00000-000"
              />
            )}
          />
        </Campo>
        <Campo className="col-span-4" rotulo="Logradouro" erro={errArr(errors, i, 'logradouro')}>
          <Input {...register(`enderecos.${i}.logradouro`)} />
        </Campo>
        <Campo className="col-span-2" rotulo="Número" erro={errArr(errors, i, 'numero')}>
          <Input {...register(`enderecos.${i}.numero`)} />
        </Campo>
        <Campo className="col-span-4" rotulo="Complemento" erro={errArr(errors, i, 'complemento')}>
          <Input {...register(`enderecos.${i}.complemento`)} />
        </Campo>
        <Campo className="col-span-2" rotulo="Bairro" erro={errArr(errors, i, 'bairro')}>
          <Input {...register(`enderecos.${i}.bairro`)} />
        </Campo>
        <Campo className="col-span-2" rotulo="UF" erro={errArr(errors, i, 'uf')}>
          <Combobox
            value={uf ?? ''}
            onChange={(v) => {
              setValue(`enderecos.${i}.uf`, v)
              setValue(`enderecos.${i}.municipio`, '')
              setValue(`enderecos.${i}.codigoIbgeMunicipio`, '')
            }}
            options={opcoesUf}
            placeholder="UF"
          />
        </Campo>
        <Campo className="col-span-2" rotulo="Cidade" erro={errArr(errors, i, 'municipio')}>
          <Combobox
            value={ibge ?? ''}
            onChange={(codigo, nome) => {
              setValue(`enderecos.${i}.codigoIbgeMunicipio`, codigo)
              setValue(`enderecos.${i}.municipio`, nome)
            }}
            options={opcoesCidade}
            disabled={!uf}
            placeholder={uf ? 'Selecione…' : 'Escolha a UF'}
            buscaPlaceholder="Buscar cidade…"
          />
        </Campo>
      </div>
    </div>
  )
}

/** Grade de campos que reflowa: 2 colunas no drawer lateral, 3 quando maximizado. */
function GridCampos({ className, children }: { className?: string; children: React.ReactNode }) {
  const maximizado = useDrawerMaximizado()
  return <div className={cn('grid gap-4', maximizado ? 'grid-cols-3' : 'grid-cols-2', className)}>{children}</div>
}

function Secao({ titulo, acao, children }: { titulo: string; acao?: React.ReactNode; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-small font-semibold uppercase tracking-wide text-fg-muted">{titulo}</h4>
        {acao}
      </div>
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

/** Input com máscara aplicada na digitação (via Controller). */
function CampoMascarado({
  control,
  name,
  rotulo,
  erro,
  mascara,
}: {
  control: Control<Campos>
  name: 'documento' | 'telefone' | 'celular' | 'whatsapp'
  rotulo: string
  erro?: string
  mascara: (v: string) => string
}) {
  return (
    <Campo rotulo={rotulo} erro={erro}>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <Input
            value={(field.value as string) ?? ''}
            onChange={(e) => field.onChange(mascara(e.target.value))}
          />
        )}
      />
    </Campo>
  )
}

function CheckboxRHF({ control, name, label }: { control: Control<Campos>; name: keyof Campos; label: string }) {
  return (
    <Controller
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      control={control as any}
      name={name as never}
      render={({ field }) => (
        <Checkbox checked={!!field.value} onChange={field.onChange} label={label} />
      )}
    />
  )
}

const SENTINELA_VAZIO = ' vazio'
const paraRadix = (v: string) => (v === '' ? SENTINELA_VAZIO : v)
const doRadix = (v: string) => (v === SENTINELA_VAZIO ? '' : v)

function SelectRHF({
  control,
  name,
  opcoes,
  placeholder,
  className,
}: {
  control: Control<Campos>
  name: keyof Campos | `enderecos.${number}.tipo`
  opcoes: { valor: string; rotulo: string }[]
  placeholder?: string
  className?: string
}) {
  return (
    <Controller
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      control={control as any}
      name={name as never}
      render={({ field }) => (
        <Select
          value={paraRadix((field.value as string) ?? '')}
          onValueChange={(v) => field.onChange(doRadix(v))}
        >
          <SelectTrigger className={className}>
            <SelectValue placeholder={placeholder} />
          </SelectTrigger>
          <SelectContent>
            {opcoes.map((o) => (
              <SelectItem key={o.valor} value={paraRadix(o.valor)}>
                {o.rotulo}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    />
  )
}

type CampoEndereco = 'cep' | 'logradouro' | 'numero' | 'complemento' | 'bairro' | 'municipio' | 'uf' | 'codigoIbgeMunicipio'

function errArr(errors: FieldErrors<Campos>, i: number, campo: CampoEndereco): string | undefined {
  return errors.enderecos?.[i]?.[campo]?.message
}
