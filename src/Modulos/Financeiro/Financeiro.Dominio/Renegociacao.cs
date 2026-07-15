using BuildingBlocks;

namespace Financeiro.Dominio;

/// <summary>Dados de uma renegociação (entrada da operação no agregado).</summary>
public sealed record DadosRenegociacao(
    decimal ValorBase,
    decimal Desconto,
    decimal Entrada,
    DateOnly Data,
    string? UsuarioId = null,
    string? Observacoes = null);

/// <summary>
/// Registro (histórico) de uma renegociação numa <see cref="ContaReceber"/>: consolidou o saldo de
/// uma ou mais parcelas (as <b>origens</b>) e originou um novo plano de parcelas (as <b>geradas</b>).
/// Fato imutável. O vínculo com as parcelas é pelo <see cref="Parcela.RenegociacaoId"/> (origem =
/// Renegociada; gerada = não). Consumidores futuros (Caixa/Contabilidade) via evento.
/// </summary>
public sealed class Renegociacao : EntidadeBase
{
    public string ContaReceberId { get; private set; } = default!;

    /// <summary>Data da renegociação (negócio) — DateOnly para portabilidade Postgres/SQLite.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Saldo consolidado das parcelas de origem (principal, ou principal + encargos, conforme a operação).</summary>
    public decimal ValorBase { get; private set; }

    public decimal Desconto { get; private set; }

    /// <summary>Entrada paga no ato (abate o valor a parcelar; se &gt; 0, vira recebimento na 1ª parcela gerada).</summary>
    public decimal Entrada { get; private set; }

    /// <summary>Valor efetivamente reparcelado = <see cref="ValorBase"/> − <see cref="Desconto"/> − <see cref="Entrada"/>.</summary>
    public decimal ValorRenegociado { get; private set; }

    public string? UsuarioId { get; private set; }
    public string? Observacoes { get; private set; }

    private Renegociacao() { }

    internal static Result<Renegociacao> Criar(string empresaId, string contaId, DadosRenegociacao dados)
    {
        if (dados.ValorBase <= 0)
            return Result<Renegociacao>.Falha("Valor base da renegociação deve ser maior que zero.");
        if (dados.Desconto < 0 || dados.Entrada < 0)
            return Result<Renegociacao>.Falha("Desconto e entrada não podem ser negativos.");

        var valorRenegociado = dados.ValorBase - dados.Desconto - dados.Entrada;
        if (valorRenegociado <= 0)
            return Result<Renegociacao>.Falha("O valor a reparcelar (base − desconto − entrada) deve ser maior que zero.");

        return Result<Renegociacao>.Ok(new Renegociacao
        {
            EmpresaId = empresaId,
            ContaReceberId = contaId,
            Data = dados.Data,
            ValorBase = dados.ValorBase,
            Desconto = dados.Desconto,
            Entrada = dados.Entrada,
            ValorRenegociado = valorRenegociado,
            UsuarioId = dados.UsuarioId,
            Observacoes = string.IsNullOrWhiteSpace(dados.Observacoes) ? null : dados.Observacoes.Trim(),
        });
    }
}
