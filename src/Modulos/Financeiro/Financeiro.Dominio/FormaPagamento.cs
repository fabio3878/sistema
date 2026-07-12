using BuildingBlocks;

namespace Financeiro.Dominio;

/// <summary>
/// Forma de pagamento/recebimento (master data por-tenant): Dinheiro, Pix, Cartão, Boleto...
/// Cadastro editável pela empresa (não é enum) — cada recebimento aponta para uma delas.
/// </summary>
public sealed class FormaPagamento : EntidadeBase
{
    public string Nome { get; private set; } = default!;

    /// <summary>Status de negócio: inativa continua visível em históricos, mas não pode ser escolhida.</summary>
    public bool Ativo { get; private set; } = true;

    private FormaPagamento() { }

    public static Result<FormaPagamento> Criar(string empresaId, string nome)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<FormaPagamento>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            return Result<FormaPagamento>.Falha("Nome é obrigatório.");

        return Result<FormaPagamento>.Ok(new FormaPagamento { EmpresaId = empresaId, Nome = nome.Trim() });
    }

    public Result Atualizar(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Falha("Nome é obrigatório.");

        Nome = nome.Trim();
        MarcarAtualizado();
        return Result.Ok();
    }

    public void Ativar()
    {
        if (Ativo) return;
        Ativo = true;
        MarcarAtualizado();
    }

    public void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        MarcarAtualizado();
    }
}
