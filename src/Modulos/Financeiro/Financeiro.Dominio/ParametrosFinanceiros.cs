using BuildingBlocks;

namespace Financeiro.Dominio;

/// <summary>
/// Parâmetros financeiros da empresa (1 linha por tenant): juros de mora ao mês e multa, usados
/// como padrão no cálculo de recebimentos em atraso (a parcela pode ter override próprio).
/// </summary>
public sealed class ParametrosFinanceiros : EntidadeBase
{
    /// <summary>Juros de mora ao mês, em % (ex.: 1 = 1% a.m.). Aplicado pro rata sobre os dias em atraso.</summary>
    public decimal JurosMoraMensalPercent { get; private set; }

    /// <summary>Multa por atraso, em % sobre o saldo (aplicada uma vez quando vencida).</summary>
    public decimal MultaPercent { get; private set; }

    private ParametrosFinanceiros() { }

    public static Result<ParametrosFinanceiros> Criar(string empresaId, decimal jurosMoraMensalPercent = 0, decimal multaPercent = 0)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<ParametrosFinanceiros>.Falha("EmpresaId é obrigatório.");
        if (jurosMoraMensalPercent < 0 || multaPercent < 0)
            return Result<ParametrosFinanceiros>.Falha("Percentuais não podem ser negativos.");

        return Result<ParametrosFinanceiros>.Ok(new ParametrosFinanceiros
        {
            EmpresaId = empresaId,
            JurosMoraMensalPercent = jurosMoraMensalPercent,
            MultaPercent = multaPercent,
        });
    }

    public Result Atualizar(decimal jurosMoraMensalPercent, decimal multaPercent)
    {
        if (jurosMoraMensalPercent < 0 || multaPercent < 0)
            return Result.Falha("Percentuais não podem ser negativos.");

        JurosMoraMensalPercent = jurosMoraMensalPercent;
        MultaPercent = multaPercent;
        MarcarAtualizado();
        return Result.Ok();
    }
}
