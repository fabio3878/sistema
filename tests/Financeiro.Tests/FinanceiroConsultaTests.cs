using BuildingBlocks;
using Cadastros.Contratos;
using Financeiro.Aplicacao;
using Financeiro.Contratos;
using Financeiro.Dominio;
using Financeiro.Infraestrutura;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Tests;

/// <summary>
/// Leitura da árvore de contas contra SQLite in-memory: filtros de situação (predicados portáveis
/// sobre as parcelas), paginação/ordenação e os valores <b>derivados</b> (saldo, situação) calculados
/// na consulta. Ordena por emissão/Id — nunca por DateTimeOffset (SQLite não traduz).
/// </summary>
public sealed class FinanceiroConsultaTests
{
    private const string Emp = "EMP_1";
    private static readonly DateOnly Passado = new(2020, 1, 10);   // vencida (antes de hoje)
    private static readonly DateOnly Futuro = new(2099, 12, 1);    // a vencer

    /// <summary>Fake da API do Cadastros: o read-side só usa o nome do cliente (aqui, ausente).</summary>
    private sealed class CadastrosFake : ICadastrosConsulta
    {
        public Task<PaginaResultado<AuditoriaDto>> ListarAuditoria(string e, FiltroAuditoria f, CancellationToken ct = default) =>
            Task.FromResult(new PaginaResultado<AuditoriaDto>([], 0, 1, 20));
        public Task<IReadOnlyList<ClienteResumoDto>> ListarClientes(string e, FiltroClientes f, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ClienteResumoDto>>([]);
        public Task<ClienteDto?> ObterCliente(string e, string id, CancellationToken ct = default) =>
            Task.FromResult<ClienteDto?>(null);
        public Task<IReadOnlyList<ProdutoResumoDto>> ListarProdutos(string e, FiltroProdutos f, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ProdutoResumoDto>>([]);
        public Task<ProdutoDto?> ObterProduto(string e, string id, CancellationToken ct = default) => Task.FromResult<ProdutoDto?>(null);
        public Task<IReadOnlyList<ServicoResumoDto>> ListarServicos(string e, FiltroServicos f, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ServicoResumoDto>>([]);
        public Task<ServicoDto?> ObterServico(string e, string id, CancellationToken ct = default) => Task.FromResult<ServicoDto?>(null);
        public Task<IReadOnlyList<EstadoDto>> ListarEstados(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EstadoDto>>([]);
        public Task<IReadOnlyList<MunicipioDto>> ListarMunicipios(string uf, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<MunicipioDto>>([]);
        public Task<IReadOnlyList<UnidadeDto>> ListarUnidades(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UnidadeDto>>([]);
    }

    private static async Task<FinanceiroDbContext> NovoContexto()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var ctx = new FinanceiroDbContext(new DbContextOptionsBuilder<FinanceiroDbContext>().UseSqlite(conn).Options);
        await ctx.Database.EnsureCreatedAsync();
        return ctx;
    }

    private static FinanceiroConsulta Consulta(FinanceiroDbContext ctx) =>
        new(new ContaReceberRepositorio(ctx), new FormaPagamentoRepositorio(ctx),
            new ParametrosRepositorio(ctx), new CadastrosFake(), new AuditoriaRepositorio(ctx));

    /// <summary>Cria uma conta de 1 parcela (valor, vencimento) e opcionalmente já a recebe (parcial/total).</summary>
    private static ContaReceber ContaUmaParcela(string clienteId, DateOnly emissao, decimal valor, DateOnly vencimento, decimal pago = 0, string? numeroDoc = null)
    {
        var plano = new[] { new PlanoParcela(1, 1, valor, vencimento) };
        var conta = ContaReceber.Criar(Emp, new DadosConta(clienteId, valor, 1, emissao, NumeroDocumento: numeroDoc), plano).Valor!;
        if (pago > 0)
            conta.RegistrarRecebimento(conta.Parcelas.Single().Id, new DadosRecebimento(emissao, pago, "FP_1"));
        return conta;
    }

    [Fact]
    public async Task Lista_conta_com_valores_derivados_e_situacao()
    {
        using var ctx = await NovoContexto();
        // Conta com 2 parcelas: uma quitada, outra a vencer → parcialmente recebida.
        var plano = new[]
        {
            new PlanoParcela(1, 2, 500m, Futuro),
            new PlanoParcela(2, 2, 500m, Futuro),
        };
        var conta = ContaReceber.Criar(Emp, new DadosConta("CLI_1", 1000m, 2, new DateOnly(2026, 1, 1)), plano).Valor!;
        conta.RegistrarRecebimento(conta.Parcelas.First().Id, new DadosRecebimento(new DateOnly(2026, 1, 5), 500m, "FP_1"));
        await ctx.Contas.AddAsync(conta);
        await ctx.SaveChangesAsync();

        var pagina = await Consulta(ctx).ListarContas(Emp, new FiltroContasReceber());
        var dto = Assert.Single(pagina.Itens);

        Assert.Equal(500m, dto.TotalRecebido);
        Assert.Equal(500m, dto.SaldoTotal);
        Assert.Equal(SituacaoConta.ParcialmenteRecebida, dto.Situacao);
        Assert.Equal(2, dto.Parcelas.Count);
        Assert.Contains(dto.Parcelas, p => p.Status == StatusParcela.Recebida);
    }

    [Fact]
    public async Task Filtra_por_situacao_vencida_e_quitada()
    {
        using var ctx = await NovoContexto();
        await ctx.Contas.AddAsync(ContaUmaParcela("CLI_1", new DateOnly(2026, 1, 1), 100m, Passado));            // vencida
        await ctx.Contas.AddAsync(ContaUmaParcela("CLI_1", new DateOnly(2026, 1, 2), 200m, Futuro, pago: 200m)); // quitada
        await ctx.Contas.AddAsync(ContaUmaParcela("CLI_1", new DateOnly(2026, 1, 3), 300m, Futuro));             // em aberto
        await ctx.SaveChangesAsync();

        var vencidas = await Consulta(ctx).ListarContas(Emp, new FiltroContasReceber(Situacao: SituacaoConta.PossuiParcelasVencidas));
        Assert.Equal(1, vencidas.Total);
        Assert.Equal(SituacaoConta.PossuiParcelasVencidas, Assert.Single(vencidas.Itens).Situacao);

        var quitadas = await Consulta(ctx).ListarContas(Emp, new FiltroContasReceber(Situacao: SituacaoConta.Quitada));
        Assert.Equal(1, quitadas.Total);
        Assert.Equal(SituacaoConta.Quitada, Assert.Single(quitadas.Itens).Situacao);
    }

    [Fact]
    public async Task Filtra_por_cliente_isola()
    {
        using var ctx = await NovoContexto();
        await ctx.Contas.AddAsync(ContaUmaParcela("CLI_1", new DateOnly(2026, 1, 1), 100m, Futuro));
        await ctx.Contas.AddAsync(ContaUmaParcela("CLI_2", new DateOnly(2026, 1, 2), 200m, Futuro));
        await ctx.SaveChangesAsync();

        var doCli2 = await Consulta(ctx).ListarContas(Emp, new FiltroContasReceber(ClienteId: "CLI_2"));
        Assert.Equal("CLI_2", Assert.Single(doCli2.Itens).ClienteId);
    }

    [Fact]
    public async Task Pagina_e_ordena_por_emissao_desc()
    {
        using var ctx = await NovoContexto();
        for (var i = 0; i < 25; i++)
            await ctx.Contas.AddAsync(ContaUmaParcela("CLI_1", new DateOnly(2026, 1, 1).AddDays(i), 100m, Futuro, numeroDoc: i.ToString("00")));
        await ctx.SaveChangesAsync();

        var consulta = Consulta(ctx);
        var p1 = await consulta.ListarContas(Emp, new FiltroContasReceber(Pagina: 1, Tamanho: 10));
        Assert.Equal(25, p1.Total);
        Assert.Equal(10, p1.Itens.Count);
        Assert.Equal("24", p1.Itens[0].NumeroDocumento); // emissão mais recente primeiro

        var p3 = await consulta.ListarContas(Emp, new FiltroContasReceber(Pagina: 3, Tamanho: 10));
        Assert.Equal(5, p3.Itens.Count);
    }
}
