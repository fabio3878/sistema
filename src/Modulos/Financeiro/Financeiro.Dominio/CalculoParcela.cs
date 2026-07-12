using Financeiro.Contratos;

namespace Financeiro.Dominio;

/// <summary>
/// Resultado do cálculo <b>derivado</b> de uma parcela (nunca persistido). Saldo, juros de mora,
/// status e dias em atraso dependem da data de hoje — por isso são calculados na leitura.
/// </summary>
public readonly record struct CalculoParcela(
    decimal SaldoPrincipal,
    int DiasAtraso,
    decimal Juros,
    decimal Multa,
    decimal SaldoAtualizado,
    StatusParcela Status);
