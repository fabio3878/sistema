using Financeiro.Contratos;
using Financeiro.Dominio;

namespace Financeiro.Tests;

/// <summary>
/// Regras da renegociação (domínio puro): consolida o saldo das parcelas selecionadas, fecha-as como
/// <see cref="Parcela.Renegociada"/> e anexa um novo plano à mesma conta. Guardas de elegibilidade,
/// conferência de soma e efeito no cálculo derivado (renegociada → saldo 0 / status Renegociada).
/// </summary>
public sealed class RenegociacaoDominioTests
{
    private const string Emp = "EMP_1";
    private static readonly DateOnly Emissao = new(2026, 1, 1);
    private static readonly DateOnly Hoje = new(2026, 6, 1);

    private static ContaReceber CriarConta(decimal total, int qtd, DateOnly primeiroVencimento)
    {
        var plano = ContaReceber.GerarPlano(total, qtd, primeiroVencimento);
        var r = ContaReceber.Criar(Emp, new DadosConta("CLI_1", total, qtd, Emissao), plano);
        Assert.True(r.Sucesso, r.Erro);
        return r.Valor!;
    }

    private static DadosRenegociacao Info(decimal valorBase, decimal desconto = 0, decimal entrada = 0) =>
        new(valorBase, desconto, entrada, Hoje);

    [Fact]
    public void Renegocia_marca_origens_e_gera_novo_plano_na_mesma_conta()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();

        var novoPlano = ContaReceber.GerarPlano(1000m, 2, new DateOnly(2026, 7, 1));
        var r = conta.Renegociar([origem.Id], novoPlano, Info(1000m));

        Assert.True(r.Sucesso, r.Erro);
        Assert.True(origem.Renegociada);
        Assert.Equal(r.Valor!.Id, origem.RenegociacaoId);
        Assert.Equal(StatusParcela.Renegociada, origem.Calcular(Hoje, 2m, 2m).Status);

        var geradas = conta.Parcelas.Where(p => !p.Renegociada && p.RenegociacaoId == r.Valor!.Id).ToList();
        Assert.Equal(2, geradas.Count);
        Assert.Equal(1000m, geradas.Sum(p => p.SaldoPrincipal));
        // Numeradas continuando após a origem (1) → 2 e 3.
        Assert.Equal([2, 3], geradas.Select(p => p.Numero).OrderBy(n => n).ToArray());
        Assert.Single(conta.Renegociacoes);
        Assert.Equal(1000m, r.Valor!.ValorRenegociado);
    }

    [Fact]
    public void Desconto_e_entrada_reduzem_o_valor_a_reparcelar()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();

        // base 1000 − desconto 100 − entrada 300 = 600 a reparcelar.
        var novoPlano = ContaReceber.GerarPlano(600m, 3, new DateOnly(2026, 7, 1));
        var r = conta.Renegociar([origem.Id], novoPlano, Info(1000m, desconto: 100m, entrada: 300m));

        Assert.True(r.Sucesso, r.Erro);
        Assert.Equal(600m, r.Valor!.ValorRenegociado);
        var geradas = conta.Parcelas.Where(p => !p.Renegociada && p.RenegociacaoId == r.Valor!.Id).ToList();
        Assert.Equal(600m, geradas.Sum(p => p.SaldoPrincipal));
    }

    [Fact]
    public void Soma_do_plano_diferente_do_valor_a_reparcelar_e_bloqueada()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();

        var planoErrado = ContaReceber.GerarPlano(900m, 2, new DateOnly(2026, 7, 1)); // soma 900 ≠ 1000
        var r = conta.Renegociar([origem.Id], planoErrado, Info(1000m));

        Assert.True(r.Falhou);
        Assert.False(origem.Renegociada); // nada foi mutado
    }

    [Fact]
    public void Parcela_quitada_nao_pode_ser_renegociada()
    {
        var conta = CriarConta(500m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();
        conta.RegistrarRecebimento(origem.Id, new DadosRecebimento(new DateOnly(2026, 1, 20), 500m, "FP_1"));

        var novoPlano = ContaReceber.GerarPlano(500m, 2, new DateOnly(2026, 7, 1));
        var r = conta.Renegociar([origem.Id], novoPlano, Info(500m));

        Assert.True(r.Falhou);
    }

    [Fact]
    public void Parcela_ja_renegociada_nao_renegocia_de_novo()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();
        conta.Renegociar([origem.Id], ContaReceber.GerarPlano(1000m, 2, new DateOnly(2026, 7, 1)), Info(1000m));

        var r = conta.Renegociar([origem.Id], ContaReceber.GerarPlano(1000m, 1, new DateOnly(2026, 8, 1)), Info(1000m));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Parcela_com_pagamento_parcial_renegocia_so_o_saldo()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var origem = conta.Parcelas.Single();
        conta.RegistrarRecebimento(origem.Id, new DadosRecebimento(new DateOnly(2026, 1, 20), 300m, "FP_1"));
        Assert.Equal(700m, origem.SaldoPrincipal);

        // Base = saldo remanescente (700). O TotalPago histórico permanece na parcela de origem.
        var novoPlano = ContaReceber.GerarPlano(700m, 2, new DateOnly(2026, 7, 1));
        var r = conta.Renegociar([origem.Id], novoPlano, Info(700m));

        Assert.True(r.Sucesso, r.Erro);
        Assert.Equal(300m, origem.TotalPago);
        var geradas = conta.Parcelas.Where(p => !p.Renegociada && p.RenegociacaoId == r.Valor!.Id).ToList();
        Assert.Equal(700m, geradas.Sum(p => p.SaldoPrincipal));
    }

    [Fact]
    public void Parcela_inexistente_na_conta_e_rejeitada()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 2, 1));
        var novoPlano = ContaReceber.GerarPlano(1000m, 1, new DateOnly(2026, 7, 1));

        var r = conta.Renegociar(["PARC_INEXISTENTE"], novoPlano, Info(1000m));
        Assert.True(r.Falhou);
    }
}
