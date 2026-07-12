using Financeiro.Contratos;
using Financeiro.Dominio;

namespace Financeiro.Tests;

/// <summary>
/// Regras do agregado Conta a Receber → Parcela → Recebimento (domínio puro, sem EF):
/// geração de parcelas, recebimento parcial/total, bloqueio de excesso, estorno, cancelamento,
/// e o cálculo <b>derivado</b> (saldo/juros/status) que cresce com os dias em atraso.
/// </summary>
public sealed class ContaReceberDominioTests
{
    private const string Emp = "EMP_1";
    private static readonly DateOnly Emissao = new(2026, 1, 1);

    private static ContaReceber CriarConta(decimal total, int qtd, DateOnly primeiroVencimento, out IReadOnlyList<PlanoParcela> plano)
    {
        plano = ContaReceber.GerarPlano(total, qtd, primeiroVencimento);
        var dados = new DadosConta("CLI_1", total, qtd, Emissao);
        var r = ContaReceber.Criar(Emp, dados, plano);
        Assert.True(r.Sucesso, r.Erro);
        return r.Valor!;
    }

    private static DadosRecebimento Rec(decimal valor, DateOnly data) =>
        new(data, valor, "FP_1");

    [Fact]
    public void GerarPlano_soma_confere_e_ajusta_centavos_na_ultima()
    {
        var plano = ContaReceber.GerarPlano(100m, 3, new DateOnly(2026, 2, 1));

        Assert.Equal(3, plano.Count);
        Assert.Equal(100m, plano.Sum(p => p.Valor));
        Assert.Equal(33.33m, plano[0].Valor);
        Assert.Equal(33.34m, plano[2].Valor); // última absorve a diferença
        // Vencimentos periódicos (30 dias).
        Assert.Equal(new DateOnly(2026, 2, 1), plano[0].Vencimento);
        Assert.Equal(new DateOnly(2026, 3, 3), plano[1].Vencimento);
    }

    [Fact]
    public void Criar_rejeita_plano_com_soma_diferente_do_total()
    {
        var planoErrado = new[] { new PlanoParcela(1, 1, 90m, new DateOnly(2026, 2, 1)) };
        var r = ContaReceber.Criar(Emp, new DadosConta("CLI_1", 100m, 1, Emissao), planoErrado);
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Recebimento_parcial_soma_total_pago_e_status_fica_parcial()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 12, 1), out _);
        var parcela = conta.Parcelas.Single();

        var r = conta.RegistrarRecebimento(parcela.Id, Rec(400m, new DateOnly(2026, 11, 1)));
        Assert.True(r.Sucesso, r.Erro);
        Assert.Equal(400m, parcela.TotalPago);
        Assert.Equal(600m, parcela.SaldoPrincipal);

        // Ainda não venceu (hoje antes do vencimento) → RecebidaParcial.
        var calc = parcela.Calcular(new DateOnly(2026, 11, 15), 2m, 2m);
        Assert.Equal(StatusParcela.RecebidaParcial, calc.Status);
        Assert.Equal(600m, calc.SaldoPrincipal);
        Assert.Equal(0m, calc.Juros);
    }

    [Fact]
    public void Recebimento_maior_que_saldo_e_bloqueado()
    {
        var conta = CriarConta(500m, 1, new DateOnly(2026, 12, 1), out _);
        var parcela = conta.Parcelas.Single();

        var r = conta.RegistrarRecebimento(parcela.Id, Rec(500.02m, new DateOnly(2026, 11, 1)));
        Assert.True(r.Falhou);
        Assert.Equal(0m, parcela.TotalPago);
    }

    [Fact]
    public void Recebimento_total_quita_a_parcela()
    {
        var conta = CriarConta(500m, 1, new DateOnly(2026, 12, 1), out _);
        var parcela = conta.Parcelas.Single();

        conta.RegistrarRecebimento(parcela.Id, Rec(300m, new DateOnly(2026, 11, 1)));
        conta.RegistrarRecebimento(parcela.Id, Rec(200m, new DateOnly(2026, 11, 10)));

        Assert.Equal(0m, parcela.SaldoPrincipal);
        Assert.Equal(StatusParcela.Recebida, parcela.Calcular(new DateOnly(2026, 12, 5), 2m, 2m).Status);
    }

    [Fact]
    public void Estorno_devolve_o_saldo_e_reabre_a_parcela()
    {
        var conta = CriarConta(500m, 1, new DateOnly(2026, 12, 1), out _);
        var parcela = conta.Parcelas.Single();

        var rec = conta.RegistrarRecebimento(parcela.Id, Rec(500m, new DateOnly(2026, 11, 1))).Valor!;
        Assert.Equal(0m, parcela.SaldoPrincipal);

        var estorno = conta.EstornarRecebimento(parcela.Id, rec.Id, "erro de digitação");
        Assert.True(estorno.Sucesso, estorno.Erro);
        Assert.Equal(500m, parcela.SaldoPrincipal);
        Assert.True(rec.Estornado);
        // O recebimento estornado permanece no histórico.
        Assert.Single(parcela.Recebimentos);
    }

    [Fact]
    public void Calcula_vencida_com_juros_de_mora_e_multa()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 1, 10), out _);
        var parcela = conta.Parcelas.Single();

        // 30 dias de atraso, juros 2% a.m. → 20; multa 2% → 20; saldo atualizado 1040.
        var calc = parcela.Calcular(new DateOnly(2026, 2, 9), jurosMoraMensalPercent: 2m, multaPercent: 2m);

        Assert.Equal(StatusParcela.Vencida, calc.Status);
        Assert.Equal(30, calc.DiasAtraso);
        Assert.Equal(20m, calc.Juros);
        Assert.Equal(20m, calc.Multa);
        Assert.Equal(1040m, calc.SaldoAtualizado);
    }

    [Fact]
    public void Saldo_atualizado_cresce_com_os_dias_em_atraso()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 1, 10), out _);
        var parcela = conta.Parcelas.Single();

        var em15 = parcela.Calcular(new DateOnly(2026, 1, 25), 2m, 0m); // 15 dias
        var em45 = parcela.Calcular(new DateOnly(2026, 2, 24), 2m, 0m); // 45 dias

        Assert.True(em45.Juros > em15.Juros);
        Assert.True(em45.SaldoAtualizado > em15.SaldoAtualizado);
    }

    [Fact]
    public void Override_de_juros_da_parcela_prevalece_sobre_o_geral()
    {
        var plano = new[] { new PlanoParcela(1, 1, 1000m, new DateOnly(2026, 1, 10), null, PercentualJurosOverride: 5m) };
        var conta = ContaReceber.Criar(Emp, new DadosConta("CLI_1", 1000m, 1, Emissao), plano).Valor!;
        var parcela = conta.Parcelas.Single();

        // 30 dias; override 5% prevalece sobre o geral 2% → juros 50 (não 20).
        var calc = parcela.Calcular(new DateOnly(2026, 2, 9), jurosMoraMensalPercent: 2m, multaPercent: 0m);
        Assert.Equal(50m, calc.Juros);
    }

    [Fact]
    public void Cancelar_conta_cancela_so_parcelas_em_aberto()
    {
        var conta = CriarConta(1000m, 2, new DateOnly(2026, 3, 1), out _);
        var p1 = conta.Parcelas.First();
        var p2 = conta.Parcelas.Last();

        // p1 recebe algo; p2 fica em aberto.
        conta.RegistrarRecebimento(p1.Id, Rec(200m, new DateOnly(2026, 2, 20)));

        conta.Cancelar();

        Assert.False(p1.Cancelada); // tem recebimento → permanece
        Assert.True(p2.Cancelada);  // em aberto → cancelada
    }

    [Fact]
    public void Nao_altera_valor_de_parcela_com_recebimento()
    {
        var conta = CriarConta(1000m, 1, new DateOnly(2026, 3, 1), out _);
        var parcela = conta.Parcelas.Single();
        conta.RegistrarRecebimento(parcela.Id, Rec(200m, new DateOnly(2026, 2, 20)));

        var r = conta.AlterarParcela(parcela.Id, 800m, parcela.Vencimento, null, null, null);
        Assert.True(r.Falhou);
    }
}
