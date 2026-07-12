using BuildingBlocks;

namespace Financeiro.Dominio;

/// <summary>Dados de um recebimento (entrada da factory).</summary>
public sealed record DadosRecebimento(
    DateOnly Data,
    decimal ValorRecebido,
    string FormaPagamentoId,
    decimal Desconto = 0,
    decimal Juros = 0,
    decimal Multa = 0,
    decimal Acrescimos = 0,
    string? Observacoes = null,
    string? UsuarioId = null);

/// <summary>
/// Recebimento (baixa) de uma <see cref="Parcela"/> — append-only. Nunca é editado: para corrigir,
/// estorna-se (marca <see cref="Estornado"/>) e registra-se um novo. Criado sempre pela parcela.
/// </summary>
public sealed class Recebimento : EntidadeBase
{
    public string ParcelaId { get; private set; } = default!;

    /// <summary>Data do recebimento (negócio) — DateOnly para portabilidade Postgres/SQLite.</summary>
    public DateOnly Data { get; private set; }

    /// <summary>Valor que amortiza o saldo principal da parcela.</summary>
    public decimal ValorRecebido { get; private set; }

    public decimal Desconto { get; private set; }
    public decimal Juros { get; private set; }
    public decimal Multa { get; private set; }
    public decimal Acrescimos { get; private set; }

    public string FormaPagamentoId { get; private set; } = default!;
    public string? Observacoes { get; private set; }
    public string? UsuarioId { get; private set; }

    /// <summary>Estorno = reversão lógica (preserva o histórico). Não deduz o saldo enquanto estornado.</summary>
    public bool Estornado { get; private set; }
    public DateTimeOffset? EstornadoEm { get; private set; }
    public string? EstornoMotivo { get; private set; }

    private Recebimento() { }

    internal static Result<Recebimento> Criar(string empresaId, string parcelaId, DadosRecebimento dados)
    {
        if (dados.ValorRecebido <= 0)
            return Result<Recebimento>.Falha("Valor recebido deve ser maior que zero.");
        if (dados.Desconto < 0 || dados.Juros < 0 || dados.Multa < 0 || dados.Acrescimos < 0)
            return Result<Recebimento>.Falha("Desconto, juros, multa e acréscimos não podem ser negativos.");
        if (string.IsNullOrWhiteSpace(dados.FormaPagamentoId))
            return Result<Recebimento>.Falha("Forma de pagamento é obrigatória.");

        return Result<Recebimento>.Ok(new Recebimento
        {
            EmpresaId = empresaId,
            ParcelaId = parcelaId,
            Data = dados.Data,
            ValorRecebido = dados.ValorRecebido,
            Desconto = dados.Desconto,
            Juros = dados.Juros,
            Multa = dados.Multa,
            Acrescimos = dados.Acrescimos,
            FormaPagamentoId = dados.FormaPagamentoId,
            Observacoes = string.IsNullOrWhiteSpace(dados.Observacoes) ? null : dados.Observacoes.Trim(),
            UsuarioId = dados.UsuarioId,
        });
    }

    internal Result Estornar(string? motivo)
    {
        if (Estornado)
            return Result.Falha("Recebimento já estornado.");

        Estornado = true;
        EstornadoEm = DateTimeOffset.UtcNow;
        EstornoMotivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        MarcarAtualizado();
        return Result.Ok();
    }
}
