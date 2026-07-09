# Sistema de Automação Comercial — Arquitetura

Documento de referência para estruturar o projeto no Claude Code. Define decisões,
estrutura de solução, convenções transversais, contratos de módulo, catálogo de
eventos, motor de sincronização, camada fiscal e ordem de implementação.

> Princípio guia: **um produto, muitas verticais.** O core funcional é construído
> uma vez; cada vertical é uma camada fina (frente de operação) ligada por
> feature flag. Mesmo binário, comportamento por configuração.

---

## 1. Decisões arquiteturais (resumo)

| # | Decisão | Escolha | Porquê |
|---|---------|---------|--------|
| 1 | Estilo | **Modular monolith** | Módulos isolados (bounded contexts), 1 deploy. Microserviços seriam complexidade sem ganho nesta fase. |
| 2 | Fonte da verdade | **Cliente-servidor (servidor da loja)** | Servidor na loja é a fonte única; vários caixas na LAN compartilham estoque/preços em tempo real (consistência de graça, sem sync intra-loja). |
| 3 | Banco do servidor | **PostgreSQL + EF Core** | Escrita concorrente real (vários PDVs), robusto, mesmo provider da consolidação. É o banco principal. |
| 4 | Contingência local (futuro) | **SQLite + EF Core (opcional por PDV)** | Ainda NÃO construída. Permite o caixa vender offline se o servidor/LAN cair. Provider trocável por config — mesmo código de módulo. |
| 5 | Sync multi-loja | **Opcional por config** | `SyncMode = LocalOnly` ou `LocalComSync`, para consolidar lojas numa central. Mesmo código. |
| 6 | Mensageria interna | **Wolverine (MIT)** | Bus in-process + **outbox nativo**. MediatR/MassTransit viraram comerciais em 2025. |
| 7 | Camada fiscal | **API terceirizada** (PlugNotas/Focus) | Reforma Tributária 2026 + NFS-e nacional. Não construir comunicação com SEFAZ. |
| 8 | Frente desktop | **Tauri 2 + React/TS** | Reusa stack web, leve. Alternativa: PWA, se offline curto bastar. Padrão visual/UX em `DESIGN_1.md`. |
| 9 | Hardware | **Agente Local .NET (Worker Service)** | SDKs de periféricos BR são .NET. Expõe localhost/WebSocket; UI fica burra de hardware. |
| 10 | Back-office | **Next.js / Cloudflare** | Gestão e relatórios consolidados. |

**Buy vs build:** RH/Folha (eSocial) e Contabilidade fiscal completa são *produtos
dentro do produto*. Integrar (SPED para o contador, folha via parceiro) antes de
construir do zero. Internalizar só se virar diferencial.

---

## 2. Camadas (do mais estável para o mais específico)

```
Frentes de operação   PDV Varejo · Restaurante · Ordem de Serviço · Autopeças ·
(vertical, camada fina)  Distribuidora · Padaria · Orçamento
        ▲ compõe sobre
Módulos funcionais    Estoque · Compras · Financeiro(CR/CP/Caixa) · Fiscal ·
(comuns a todos)        Contabilidade · RH · CRM
        ▲ usa
Cadastros base        Clientes · Fornecedores · Produtos&Serviços · Tabelas Fiscais&Preço
(master data)
        ▲ apoia-se em
Plataforma            Identidade&Acesso · Multi-empresa · Engine de Sync ·
(shared kernel)         Licenciamento&Flags · Auditoria
```

Regra de dependência: setas apontam **para baixo**. Uma frente pode usar módulos
funcionais; um módulo funcional pode usar cadastros e plataforma. **Nunca o
contrário, nunca lateral via internals** (só via contratos públicos + eventos).

---

## 3. Estrutura da solução (.NET)

```
automacao-comercial/
├─ AutomacaoComercial.sln
├─ CLAUDE.md                      # contexto pro Claude Code (ver seção 14)
├─ src/
│  ├─ Compartilhado/
│  │  └─ BuildingBlocks/          # Result, EntidadeBase, Ulid, IModulo, abstrações de bus
│  │
│  ├─ Plataforma/                 # shared kernel
│  │  ├─ Plataforma.Dominio/
│  │  ├─ Plataforma.Aplicacao/
│  │  └─ Plataforma.Infraestrutura/   # Identidade, Tenant, Licenciamento, Auditoria, Outbox
│  │
│  ├─ Modulos/
│  │  ├─ Cadastros/
│  │  │  ├─ Cadastros.Contratos/      # PÚBLICO: interfaces, DTOs, eventos. Referenciável por todos.
│  │  │  ├─ Cadastros.Dominio/        # interno
│  │  │  ├─ Cadastros.Aplicacao/      # interno
│  │  │  └─ Cadastros.Infraestrutura/ # interno: EF, repos, migrations
│  │  ├─ Estoque/        (Contratos · Dominio · Aplicacao · Infraestrutura)
│  │  ├─ Vendas/         (core da venda — o agregado que dispara o fluxo)
│  │  ├─ Financeiro/
│  │  ├─ Fiscal/
│  │  ├─ Compras/
│  │  ├─ Contabilidade/
│  │  ├─ Crm/
│  │  └─ Rh/
│  │
│  ├─ Frentes/                    # verticais (camada fina sobre Vendas + módulos)
│  │  ├─ Frente.PdvVarejo/
│  │  ├─ Frente.Restaurante/
│  │  ├─ Frente.OrdemServico/
│  │  ├─ Frente.Autopecas/
│  │  ├─ Frente.Distribuidora/
│  │  ├─ Frente.Padaria/
│  │  └─ Frente.Orcamento/
│  │
│  └─ Hosts/
│     ├─ AgenteLocal/             # Worker Service .NET — orquestra módulos + frentes + hardware
│     │  ├─ Hardware/             # impressora, TEF, SAT, balança, leitor (atrás de interfaces + mocks)
│     │  └─ Endpoints/            # WebSocket/HTTP localhost consumido pela UI
│     └─ Api.Central/             # opcional: alvo de sync (Postgres) + back-office API
│
├─ desktop/                       # Tauri 2 + React/TS (frente PDV)
├─ web/                           # Next.js (back-office)
└─ tests/
   ├─ <Modulo>.Tests/             # unidade por módulo
   └─ Arquitetura.Tests/          # testes de fronteira (ver seção 3.1)
```

**Regra de referência de projeto (a fronteira que protege tudo):**
- `*.Contratos` → não referencia nada interno. Só BuildingBlocks.
- `*.Dominio` → BuildingBlocks + Contratos de outros módulos (nunca Dominio/Infra de outros).
- `*.Aplicacao` → seu próprio Dominio + Contratos de outros.
- `*.Infraestrutura` → tudo do seu módulo.
- Frentes → Contratos dos módulos + Vendas.Contratos. Nunca Infraestrutura alheia.

### 3.1 Testes de arquitetura (impõem a fronteira automaticamente)

Use `NetArchTest` ou `ArchUnitNET` para falhar o build se alguém violar a regra:

```csharp
[Fact]
public void Modulos_nao_referenciam_internals_de_outros_modulos()
{
    var resultado = Types.InAssembly(typeof(EstoqueModulo).Assembly)
        .That().ResideInNamespace("Estoque.Dominio")
        .ShouldNot().HaveDependencyOnAny("Financeiro.Dominio", "Financeiro.Infraestrutura",
                                         "Fiscal.Dominio", "Fiscal.Infraestrutura")
        .GetResult();
    Assert.True(resultado.IsSuccessful, string.Join(", ", resultado.FailingTypeNames));
}
```

Isso deixa o Claude Code trabalhar num módulo sem quebrar os outros por acidente.

---

## 4. Convenções transversais

### 4.1 Entidade base (campos de sync desde o dia 1)

Todo registro sincronizável herda isto. **Não dá pra adicionar depois sem migração dolorosa.**

```csharp
public abstract class EntidadeBase
{
    // PK = ULID (string 26 chars). NUNCA autoincrement — dois PDVs gerando id=1 colidem.
    // ULID é ordenável por tempo, bom para índice.
    public string Id { get; set; } = Ulid.NewUlid().ToString();

    // Multi-tenant: tudo é particionado por empresa/filial.
    public string EmpresaId { get; set; } = default!;

    // Sync / auditoria
    public DateTimeOffset CriadoEm   { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
    public long Versao { get; set; }          // incrementa a cada update; resolve conflito de sync
    public bool Excluido { get; set; }         // soft delete (tombstone). NUNCA DELETE físico no que sincroniza.
    public string? OrigemId { get; set; }      // id do device/loja que originou (rastreio de sync)
}
```

Regras derivadas:
- **PK = ULID**, sempre.
- **Soft delete** em tudo que sincroniza (DELETE físico faz o registro "ressuscitar" no próximo sync).
- `AtualizadoEm` + `Versao` resolvem conflito (estratégia padrão: last-write-wins por `AtualizadoEm`; merge manual onde fizer sentido).
- Filtro global de query no EF: `HasQueryFilter(e => !e.Excluido)`.

### 4.2 Result em vez de exceção para fluxo de domínio

```csharp
public readonly record struct Result(bool Sucesso, string? Erro)
{
    public static Result Ok() => new(true, null);
    public static Result Falha(string erro) => new(false, erro);
}
public readonly record struct Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T v) => new(true, v, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);
}
```

### 4.3 Contrato de módulo (registro plugável)

Cada módulo expõe um `IModulo` para auto-registro no host:

```csharp
public interface IModulo
{
    string Nome { get; }
    void RegistrarServicos(IServiceCollection services, IConfiguration config);
    void RegistrarMigrations(MigrationRegistry registry);
}
```

O host varre os módulos e só ativa os habilitados pela licença (ver seção 8).

---

## 5. Building blocks de mensageria (Wolverine)

Wolverine é o bus in-process **e** o outbox. O mesmo mecanismo que entrega eventos
entre módulos é o que persiste mutações para o sync.

```csharp
// Program.cs / host do AgenteLocal
builder.Host.UseWolverine(opts =>
{
    // Outbox durável sobre o banco local (sobrevive a crash/reinício)
    opts.PersistMessagesWithSqlite(connectionString);
    opts.Policies.AutoApplyTransactions();        // handler roda dentro da transação do EF
    opts.Policies.UseDurableLocalQueues();        // entrega garantida in-process
});
```

Handler = método. Sem interface, sem classe base:

```csharp
public static class QuandoVendaFechada
{
    // Estoque reage. Retornar mensagens publica via outbox, atômico com o SaveChanges.
    public static async Task<EstoqueBaixado> Handle(
        VendaFechada e, IEstoqueServico estoque)
    {
        await estoque.BaixarItens(e.EmpresaId, e.Itens);
        return new EstoqueBaixado(e.VendaId, e.Itens);
    }
}
```

Síncrono in-process (mesma transação) para estoque/financeiro. **Assíncrono via
outbox + worker** para a parte fiscal (chamada externa, retry, contingência).

---

## 6. Módulos — responsabilidade e contrato público

Cada módulo expõe **só** o que está em `*.Contratos`. Abaixo o essencial de cada um.

### 6.1 Plataforma (shared kernel)
- **Abstrações de identidade/tenant**: `IContextoEmpresa` (tenant atual) e — futuro —
  `IContextoUsuario` (usuário autenticado + permissões). A **persistência** de usuários/perfis
  mora no módulo `Acesso` (6.8), não aqui: Plataforma é biblioteca sem banco.
- **Multi-empresa & Filial**: tenant. Toda query filtra por `EmpresaId`.
- **Licenciamento & Feature flags**: quais módulos/frentes estão ligados por cliente (`ILicenca`).
- **Engine de Sync**: outbox/changelog → reconciliação com central.
- **Auditoria**: log de quem fez o quê.

### 6.2 Cadastros (master data)
- **Cadastros separados por tipo**: cada papel tem entidade/tabela própria — **Cliente**
  (`cad_clientes`) construído agora; Fornecedor, Funcionário e Transportadora virão depois,
  cada um independente. Não há "Pessoa unificada com papéis": no varejo (público geral) a
  sobreposição cliente↔fornecedor é rara, então o modelo separado é mais simples e a eventual
  duplicação do mesmo CNPJ é aceitável.
- **Cliente**: identidade (nome, CPF/CNPJ, tipo PF/PJ), contato (e-mail, telefone), dados
  fiscais do destinatário (RG, IE, IM, `indIEDest`, regime tributário), status `Ativo`
  (≠ soft delete), limite de crédito, e endereços 1:N (`cad_cliente_enderecos`,
  Principal/Cobrança/Entrega, com código IBGE do município para emissão fiscal).
- **Produto/Serviço**: SKU, código de barras, unidades, dados fiscais (NCM, CEST, CST, origem).
- **Tabelas**: preço (por cliente/tabela), tributárias.

```csharp
public interface ICadastrosConsulta
{
    Task<ClienteDto?> ObterCliente(string empresaId, string clienteId);
    Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId);
}
```

### 6.3 Estoque
- Saldo por produto/depósito, movimentações, inventário.
- **Lotes e validade** (crítico para padaria/mercado/distribuidora).
- Reserva e baixa.

```csharp
public interface IEstoqueServico
{
    Task<Result> BaixarItens(string empresaId, IReadOnlyList<ItemMovimento> itens);
    Task<Result> Entrada(string empresaId, IReadOnlyList<ItemMovimento> itens, string origem);
    Task<int> SaldoDisponivel(string empresaId, string produtoId, string depositoId);
}
```

### 6.4 Vendas (core da venda)
- Agregado `Venda`: itens, descontos, pagamentos, status.
- É quem **dispara o fluxo** publicando `VendaFechada`.
- Não conhece a vertical; a frente é que monta a `Venda` do seu jeito.

### 6.5 Financeiro (CR / CP / Caixa)
- **Contas a Receber**, **Contas a Pagar**, **Caixa/Bancos**, conciliação.
- **Centro de custo é DIMENSÃO**, não módulo: uma coluna/classificação no lançamento.

```csharp
public interface IFinanceiroServico
{
    Task<Result> GerarTitulosDeVenda(string empresaId, string vendaId,
                                     IReadOnlyList<PagamentoDto> pagamentos);
}
```

### 6.6 Fiscal
- Emissão NFC-e / NF-e / NFS-e via **API externa** (PlugNotas/Focus).
- **Assíncrono**: a venda fecha, a nota autoriza em background com retry/contingência.
- Guarda referência ao XML (o provedor arquiva os 11 anos; mantenha o link/chave).

```csharp
public interface IFiscalServico
{
    Task<Result> EnfileirarEmissao(string empresaId, DocumentoFiscalRequest req);
}
```

### 6.7 Compras, Contabilidade, CRM, RH
- **Compras**: pedido → recebimento → entrada no estoque + título a pagar.
- **Contabilidade**: plano de contas, lançamentos (a partir de eventos do financeiro), SPED.
- **CRM**: relacionamento, limite de crédito (consultado pelo Financeiro na venda a prazo).
- **RH**: começar por integração; folha completa depois.

### 6.8 Acesso (usuários e permissões — RBAC)
Controle de acesso ao sistema. Módulo **sempre ativo** (autenticação não é licenciável). Tabelas
com prefixo `acs_`, em **duas camadas**:

- **Dado de tenant** (herda `EntidadeBase`, filtra por `EmpresaId`, soft-delete/sync):
  - **Usuario** (`acs_usuarios`): login (único por empresa, normalizado), nome, e-mail, **hash de
    senha** (nunca em claro — porta `IHashSenha`/PBKDF2), `Ativo`, `DeveTrocarSenha`,
    `StampSeguranca` (base para revogar token no futuro).
  - **Perfil** (`acs_perfis`): papel nomeado; `Protegido` (o "Administrador" do seed) e
    `ConcedeTodas` (super-perfil que dispensa listar cada permissão).
  - **Vínculos N:N** (`acs_usuario_perfis`, `acs_perfil_funcionalidades`): usuário↔perfil e
    perfil↔funcionalidade. Permissão efetiva do usuário = união dos perfis.
- **Catálogo de capacidade** (referência GLOBAL — NÃO herda `EntidadeBase`, sem `EmpresaId`):
  - **Modulo** (`acs_modulos`) e **Funcionalidade** (`acs_funcionalidades`), chave natural = código.
  - **Fonte da verdade é o CÓDIGO**: cada módulo declara suas funcionalidades em `*.Contratos`
    (constantes + `IModulo.Funcionalidades()`); o host agrega o manifesto dos módulos ativos e o
    seeder do Acesso reconcilia para essas tabelas no startup (idempotente; o que sai do código
    vira `Obsoleta`, nunca é apagado). Convenção: `<modulo>.<recurso>.<acao>`.

No first-run de um tenant sem usuários, o seeder cria o perfil "Administrador" (`ConcedeTodas`) e
um usuário admin inicial a partir de `Acesso:AdminInicial:Login`/`:Senha` (user-secrets/env — nunca
hardcode; ausente ⇒ avisa e pula).

```csharp
public interface IAcessoConsulta
{
    Task<UsuarioDto?> ObterUsuario(string empresaId, string usuarioId);
    Task<PerfilDto?>  ObterPerfil(string empresaId, string perfilId);
}
```

**Autenticação (implementada):** login em `/acesso/login` valida a senha (`IHashSenha`), monta as
permissões (união dos perfis ativos ou `ConcedeTodas`) e emite um **JWT HS256 curto** (claims `sub`,
`empresa`, `login`, `stamp`, e `perm_all`/`func`) + um **refresh token revogável** persistido em
`acs_refresh_tokens` (256 bits; no banco só o SHA-256). `/acesso/refresh` **rotaciona** (revoga o
antigo, emite novo par) e recusa se o `StampSeguranca` mudou; reuso de token revogado ⇒ revoga todos
(anti-roubo). `/acesso/trocar-senha` rotaciona o stamp (invalida tokens) e `/acesso/logout` revoga.
Tempos configuráveis em `Acesso:Jwt:*`; **chave de assinatura só em user-secrets/env**.

`IContextoUsuario` (Plataforma.Dominio) + `IContextoEmpresa` viram **scoped**, lidos das claims via
`IHttpContextAccessor` (fallback: tenant configurado do servidor). Autorização por política
`func:<codigo>` (`FuncionalidadePolicyProvider` + `IContextoUsuario.Pode`); endpoints usam
`RequireAuthorization(PoliticaAcesso.Funcionalidade(codigo))`. Handlers Wolverine tomam o tenant da
mensagem, não do contexto scoped.

---

## 7. Catálogo de eventos de domínio

Eventos são `record` imutáveis em `*.Contratos`. Nomeados no passado (fato ocorrido).

| Evento | Publicado por | Consumido por |
|--------|---------------|---------------|
| `VendaFechada` | Vendas | Estoque, Financeiro, Fiscal |
| `EstoqueBaixado` / `EstoqueInsuficiente` | Estoque | Vendas (compensação), Frente (UI) |
| `TitulosGerados` | Financeiro | Contabilidade |
| `NotaAutorizada` / `NotaRejeitada` | Fiscal | Vendas, Frente (imprime DANFE / alerta) |
| `RecebimentoConfirmado` | Compras | Estoque, Financeiro |
| `PagamentoBaixado` | Financeiro | Contabilidade |

### 7.1 Fluxo de uma venda (o coração do sistema)

```
Frente (PDV/Restaurante/...) monta a Venda e chama Vendas.Fechar()
        │
        ▼
[Vendas]  grava Venda (Postgres do servidor da loja, transação) ──► publica VendaFechada
        │                                          │ (mesma transação via outbox)
        ├──────────────► [Estoque]  baixa itens ──► EstoqueBaixado
        │                            (ou EstoqueInsuficiente ► bloqueia/avisa)
        │
        ├──────────────► [Financeiro] gera títulos conforme pagamentos:
        │                   à vista → quita no caixa
        │                   a prazo → Conta a Receber (checa limite no CRM)
        │                            └► TitulosGerados ─► [Contabilidade] lança
        │
        └──────────────► [Fiscal] ENFILEIRA emissão (NÃO bloqueia a venda)
                            │  worker assíncrono chama API fiscal
                            ▼
                          NotaAutorizada ─► Frente imprime DANFE/cupom
                          NotaRejeitada  ─► alerta + fila de correção
```

Pontos não-negociáveis:
1. Estoque e Financeiro rodam **na mesma transação** da venda (consistência imediata).
2. Fiscal é **assíncrono** (chamada externa não pode travar o caixa). Contingência
   offline da NFC-e e o SAT (SP) combinam com isso: emite/imprime e transmite depois.
3. Tudo passa pelo **outbox**: se a máquina cair entre fechar a venda e emitir a
   nota, o evento sobrevive e o worker retoma.

---

## 8. Frentes + Licenciamento (como 1 produto vira N verticais)

A licença do cliente liga módulos e UMA frente:

```csharp
public interface ILicenca
{
    bool ModuloAtivo(string modulo);       // "Estoque", "Financeiro", "Fiscal"...
    string FrenteAtiva { get; }            // "PdvVarejo", "Restaurante", "OrdemServico"...
    SyncMode Sync { get; }                 // LocalOnly | LocalComSync
}
```

Exemplos (mesmo binário):
- **Padaria** → frente `PdvVarejo`, módulos `[Estoque(validade), Financeiro, Fiscal]`.
- **Oficina** → frente `OrdemServico`, módulos `[Autopecas, Estoque, Financeiro, Fiscal]`.
- **Restaurante** → frente `Restaurante`, módulos `[Estoque, Financeiro, Fiscal]`.

A frente é a "camada fina": monta a `Venda` do jeito da vertical (mesa/comanda no
restaurante, OS na oficina, leitura rápida no PDV), mas o fluxo da seção 7 é o mesmo.

> **Front-end (casca única).** Toda frente compartilha a **mesma casca visual** — muda só o
> conjunto de módulos ativos. Sidebar, rotas e widgets são dirigidos por `ModuloAtivo` +
> permissão (`func:<codigo>`). O padrão de design, UX e stack de UI (Tauri 2 + React/TS,
> Tailwind + Radix/shadcn, tokens claro+escuro) é a regra em **`DESIGN_1.md`**.

---

## 9. Motor de sincronização

Só ativo quando `SyncMode = LocalComSync` (consolidação **multi-loja**; dentro de uma
loja os caixas já compartilham o servidor, sem sync). Reusa o outbox do Wolverine.

```
[Postgres da loja]  mutação ─► outbox (Wolverine, durável)
                              │  worker de sync (quando online)
                              ▼
                        push de deltas ─► [Api.Central] ─► [Postgres central]
                              ▲
                        pull de deltas ◄─ (preços, cadastros vindos da matriz)
```

- **Push**: envia registros com `Versao`/`AtualizadoEm` desde o último cursor.
- **Pull**: recebe alterações da central (ex.: tabela de preço atualizada na matriz).
- **Conflito**: last-write-wins por `AtualizadoEm`; tombstone garante exclusão propagada.
- `SyncMode = LocalOnly` → o worker simplesmente não roda. Zero diferença de código.

---

## 10. Camada fiscal (detalhe)

- Provedor: **PlugNotas** ou **Focus NFe** (decidir por DX/cobertura municipal).
- O provedor cuida de assinatura, XML, contingência e atualização das regras.
- **Reforma Tributária 2026**: CST/layout mudaram — mais um motivo para terceirizar.
- **NFS-e modelo nacional**: obrigatório para todos os municípios desde jan/2026.
- **Arquivamento**: XML por 11 anos. O provedor arquiva; guarde chave/link localmente.
- Certificado A1 fica no Agente Local (ou no provedor, conforme o modelo escolhido).

Fluxo: `IFiscalServico.EnfileirarEmissao` → outbox → worker → API → webhook/polling
do resultado → publica `NotaAutorizada`/`NotaRejeitada`.

---

## 11. Banco — providers EF Core

Um `DbContext` por módulo (cada um dono das suas tabelas). Provider escolhido **por
configuração** (seção `Banco`), não hard-coded. A decisão mora num único helper
(`AdicionarDbContextConfiguravel<T>` em `Plataforma.Infraestrutura`) + no outbox do Wolverine:

```csharp
// Servidor da loja (principal) — appsettings "Banco": { "Provider": "Postgres" }
services.AddDbContext<EstoqueDbContext>(o => o.UseNpgsql(conn));

// Contingência local futura por PDV (opcional) — "Provider": "Sqlite"
services.AddDbContext<EstoqueDbContext>(o => o.UseSqlite(localConn));
```

Cuidados (denominador comum entre Postgres e SQLite):
- Não usar `jsonb`/arrays nativos do Postgres no modelo compartilhado.
- `decimal` para dinheiro (cuidado com a tipagem frouxa do SQLite — mapear explícito).
- `DateTimeOffset` em UTC sempre.
- **Migrations por provider** (pastas/assemblies separadas): o SQL gerado difere. Hoje só
  o **Postgres** tem migrations; a assembly de migrations do SQLite entra junto com a
  contingência local, quando ela for construída.
- Separação lógica por **prefixo de tabela ou schema** por módulo (`estoque_*`, `fin_*`).

---

## 12. Stack e versões alvo

| Camada | Tecnologia |
|--------|-----------|
| Runtime | .NET 10 (LTS) — fallback .NET 9 |
| ORM | EF Core 10 |
| Bus + Outbox | Wolverine 6 (MIT) |
| Banco do servidor da loja (principal) | PostgreSQL 17 |
| Contingência local futura (por PDV) | SQLite |
| Banco central (consolidação multi-loja) | PostgreSQL 17 |
| Desktop | Tauri 2 + React 19 + TypeScript |
| Back-office | Next.js (Cloudflare Pages/Workers) |
| Fiscal | PlugNotas / Focus NFe (API) |
| E-mail | Resend |
| Pagamento (back-office) | Rede (Itaú) |
| Testes de fronteira | NetArchTest / ArchUnitNET |

---

## 13. Ordem de implementação (fases)

1. **Fundação**: BuildingBlocks + Plataforma (Identidade, Tenant, Licença, Outbox) +
   Cadastros. Testes de arquitetura ligados.
2. **Sistema vendável mínimo**: Estoque + Vendas + Fiscal + frente `PdvVarejo`.
   Já emite NFC-e e baixa estoque.
3. **Financeiro** (CR/CP/Caixa) + centro de custo como dimensão.
4. **Segunda frente** (ex.: `Restaurante` ou `OrdemServico`) — valida que a camada
   fina é mesmo fina.
5. **Sync** (LocalComSync + Api.Central + Postgres) quando aparecer o primeiro
   cliente multi-loja.
6. **Contabilidade/RH** — via integração primeiro.

---

## 14. CLAUDE.md sugerido (colar na raiz do repo)

```markdown
# Sistema de Automação Comercial

Modular monolith cliente-servidor em .NET (servidor da loja Postgres + PDVs na LAN).
Um produto, várias verticais (frentes).

## Regras inegociáveis
- PK = ULID (string). NUNCA autoincrement.
- Soft delete (Excluido=true) no que sincroniza. NUNCA DELETE físico.
- Toda entidade sincronizável herda EntidadeBase (Id, EmpresaId, CriadoEm,
  AtualizadoEm, Versao, Excluido).
- Toda query filtra por EmpresaId (multi-tenant).
- Módulos só se comunicam por *.Contratos (interfaces + DTOs + eventos) e por
  eventos do Wolverine. PROIBIDO referenciar Dominio/Infraestrutura de outro módulo.
- A venda fecha no servidor da loja (Postgres, transação). Fiscal é assíncrono (não trava o caixa).
- Use Wolverine (MIT) para bus + outbox. NÃO usar MediatR/MassTransit (comerciais).

## Estrutura
src/Compartilhado, src/Plataforma, src/Modulos/<Nome>/{Contratos,Dominio,Aplicacao,
Infraestrutura}, src/Frentes/Frente.<Vertical>, src/Hosts/{AgenteLocal,Api.Central}.

## Ao adicionar um módulo
1. Crie os 4 projetos (Contratos, Dominio, Aplicacao, Infraestrutura).
2. Implemente IModulo para auto-registro.
3. DbContext próprio, tabelas com prefixo do módulo.
4. Publique eventos em Contratos; consuma via handlers Wolverine.
5. Adicione teste de arquitetura confirmando que não vaza internals.
```

---

## 15. Primeiros comandos no Claude Code

```bash
# 1. Solução e building blocks
dotnet new sln -n AutomacaoComercial
dotnet new classlib -o src/Compartilhado/BuildingBlocks

# 2. Primeiro módulo (Cadastros) com a fronteira de 4 projetos
dotnet new classlib -o src/Modulos/Cadastros/Cadastros.Contratos
dotnet new classlib -o src/Modulos/Cadastros/Cadastros.Dominio
dotnet new classlib -o src/Modulos/Cadastros/Cadastros.Aplicacao
dotnet new classlib -o src/Modulos/Cadastros/Cadastros.Infraestrutura

# 3. Host do agente local
dotnet new worker -o src/Hosts/AgenteLocal

# 4. Testes de arquitetura (a fronteira que se auto-impõe)
dotnet new xunit -o tests/Arquitetura.Tests
```

Sugestão de prompt inicial para o Claude Code:
> "Leia o CLAUDE.md. Implemente os BuildingBlocks (EntidadeBase, Result, IModulo,
> Ulid helper) e o módulo Cadastros completo com Cliente e Produto, DbContext
> Postgres, migration inicial e um teste de arquitetura garantindo que Cadastros.Dominio
> não depende de internals de outros módulos."
