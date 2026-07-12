using BuildingBlocks;
using Financeiro.Contratos;

namespace Financeiro.Dominio;

/// <summary>
/// Parcela de uma <see cref="ContaReceber"/> (1:N) — cada uma tem vida financeira própria.
/// <b>Persiste só fatos imutáveis</b> (<see cref="TotalPago"/> + marcadores de cancelamento/renegociação);
/// saldo, status, juros e dias em atraso são <b>derivados na leitura</b> (ver <see cref="Calcular"/>),
/// porque o juros de mora corre com o tempo — um saldo gravado nasceria desatualizado.
/// </summary>
public sealed class Parcela : EntidadeBase
{
    private readonly List<Recebimento> _recebimentos = [];

    public string ContaReceberId { get; private set; } = default!;
    public int Numero { get; private set; }
    public int TotalParcelas { get; private set; }
    public decimal ValorOriginal { get; private set; }
    public DateOnly Vencimento { get; private set; }
    public DateOnly? DataPrevistaRecebimento { get; private set; }

    /// <summary>Override do juros de mora ao mês desta parcela (%). Nulo = usa o parâmetro geral da empresa.</summary>
    public decimal? PercentualJurosOverride { get; private set; }

    /// <summary>Soma dos recebimentos não estornados (fato imutável, independente da data). Único valor de saldo persistido.</summary>
    public decimal TotalPago { get; private set; }

    public bool Cancelada { get; private set; }
    public bool Renegociada { get; private set; }
    public string? Observacoes { get; private set; }

    public IReadOnlyCollection<Recebimento> Recebimentos => _recebimentos.AsReadOnly();

    /// <summary>Saldo principal (sem juros): valor original menos o já pago. Derivado — não mapeado.</summary>
    public decimal SaldoPrincipal => ValorOriginal - TotalPago;

    private Parcela() { }

    internal static Result<Parcela> Criar(string empresaId, string contaId, PlanoParcela plano)
    {
        if (plano.Numero <= 0)
            return Result<Parcela>.Falha("Número da parcela deve ser positivo.");
        if (plano.Valor <= 0)
            return Result<Parcela>.Falha("Valor da parcela deve ser maior que zero.");

        return Result<Parcela>.Ok(new Parcela
        {
            EmpresaId = empresaId,
            ContaReceberId = contaId,
            Numero = plano.Numero,
            TotalParcelas = plano.TotalParcelas,
            ValorOriginal = plano.Valor,
            Vencimento = plano.Vencimento,
            DataPrevistaRecebimento = plano.DataPrevistaRecebimento,
            PercentualJurosOverride = plano.PercentualJurosOverride,
        });
    }

    internal Result<Recebimento> RegistrarRecebimento(DadosRecebimento dados)
    {
        if (Cancelada)
            return Result<Recebimento>.Falha("Parcela cancelada não recebe pagamentos.");
        if (Renegociada)
            return Result<Recebimento>.Falha("Parcela renegociada não recebe pagamentos.");
        if (SaldoPrincipal <= 0)
            return Result<Recebimento>.Falha("Parcela já está quitada.");

        // Regra: não recebe mais que o saldo principal (tolerância de 1 centavo p/ arredondamento).
        if (dados.ValorRecebido > SaldoPrincipal + 0.01m)
            return Result<Recebimento>.Falha($"Valor recebido (R$ {dados.ValorRecebido:0.00}) maior que o saldo da parcela (R$ {SaldoPrincipal:0.00}).");

        var criacao = Recebimento.Criar(EmpresaId, Id, dados);
        if (criacao.Falhou)
            return criacao;

        _recebimentos.Add(criacao.Valor!);
        TotalPago += dados.ValorRecebido;
        MarcarAtualizado();
        return criacao;
    }

    internal Result<Recebimento> EstornarRecebimento(string recebimentoId, string? motivo)
    {
        var recebimento = _recebimentos.FirstOrDefault(r => r.Id == recebimentoId);
        if (recebimento is null)
            return Result<Recebimento>.Falha("Recebimento não encontrado nesta parcela.");

        var estorno = recebimento.Estornar(motivo);
        if (estorno.Falhou)
            return Result<Recebimento>.Falha(estorno.Erro!);

        TotalPago -= recebimento.ValorRecebido;
        MarcarAtualizado();
        return Result<Recebimento>.Ok(recebimento);
    }

    /// <summary>Cancela a parcela — só quando ainda não houve recebimento (regra: não excluir parcela com recebimentos).</summary>
    internal Result Cancelar()
    {
        if (Cancelada) return Result.Ok();
        if (TotalPago > 0)
            return Result.Falha("Parcela com recebimentos não pode ser cancelada — estorne os recebimentos antes.");

        Cancelada = true;
        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>Altera vencimento/valor/observações da parcela. Valor só pode mudar enquanto não houver recebimento.</summary>
    internal Result AlterarDados(decimal valor, DateOnly vencimento, DateOnly? dataPrevista, decimal? percentualOverride, string? observacoes)
    {
        if (Cancelada || Renegociada)
            return Result.Falha("Parcela cancelada ou renegociada não pode ser alterada.");
        if (valor <= 0)
            return Result.Falha("Valor da parcela deve ser maior que zero.");
        if (valor != ValorOriginal && TotalPago > 0)
            return Result.Falha("Não é possível alterar o valor de uma parcela que já teve recebimentos.");
        if (valor < TotalPago)
            return Result.Falha("Valor da parcela não pode ser menor que o total já recebido.");
        if (percentualOverride is < 0)
            return Result.Falha("Percentual de juros não pode ser negativo.");

        ValorOriginal = valor;
        Vencimento = vencimento;
        DataPrevistaRecebimento = dataPrevista;
        PercentualJurosOverride = percentualOverride;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>
    /// Cálculo derivado (na leitura): saldo, dias em atraso, juros/multa de mora até <paramref name="hoje"/>,
    /// saldo atualizado e status. O percentual efetivo é o override da parcela, senão o geral da empresa.
    /// </summary>
    public CalculoParcela Calcular(DateOnly hoje, decimal jurosMoraMensalPercent, decimal multaPercent)
    {
        var saldoPrincipal = SaldoPrincipal;

        if (Cancelada)
            return new CalculoParcela(0, 0, 0, 0, 0, StatusParcela.Cancelada);
        if (Renegociada)
            return new CalculoParcela(0, 0, 0, 0, 0, StatusParcela.Renegociada);
        if (saldoPrincipal <= 0)
            return new CalculoParcela(0, 0, 0, 0, 0, StatusParcela.Recebida);

        var diasAtraso = Math.Max(0, hoje.DayNumber - Vencimento.DayNumber);
        var emAtraso = diasAtraso > 0;

        var pct = PercentualJurosOverride ?? jurosMoraMensalPercent;
        var juros = emAtraso ? Math.Round(saldoPrincipal * pct / 100m * diasAtraso / 30m, 2, MidpointRounding.AwayFromZero) : 0m;
        var multa = emAtraso ? Math.Round(saldoPrincipal * multaPercent / 100m, 2, MidpointRounding.AwayFromZero) : 0m;
        var saldoAtualizado = saldoPrincipal + juros + multa;

        var status = emAtraso
            ? StatusParcela.Vencida
            : (TotalPago > 0 ? StatusParcela.RecebidaParcial : StatusParcela.Aberta);

        return new CalculoParcela(saldoPrincipal, diasAtraso, juros, multa, saldoAtualizado, status);
    }
}
