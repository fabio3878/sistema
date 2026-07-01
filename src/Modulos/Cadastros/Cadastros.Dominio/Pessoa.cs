using BuildingBlocks;
using Cadastros.Contratos;

namespace Cadastros.Dominio;

/// <summary>
/// Pessoa unificada com papéis (seção 6.2): o mesmo CPF/CNPJ é cliente, fornecedor,
/// funcionário ou transportadora — sem cadastro duplicado.
/// </summary>
public sealed class Pessoa : EntidadeBase
{
    public string Nome { get; private set; } = default!;

    /// <summary>CPF ou CNPJ (só dígitos).</summary>
    public string Documento { get; private set; } = default!;

    public PapelPessoa Papeis { get; private set; }

    // Construtor para o EF Core materializar.
    private Pessoa() { }

    /// <summary>Cria uma Pessoa validada. Regras de negócio via <see cref="Result{T}"/>, não exceção.</summary>
    public static Result<Pessoa> Criar(string empresaId, string nome, string documento, PapelPessoa papeis)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<Pessoa>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            return Result<Pessoa>.Falha("Nome é obrigatório.");

        var doc = SomenteDigitos(documento);
        if (doc.Length is not (11 or 14))
            return Result<Pessoa>.Falha("Documento deve ser um CPF (11) ou CNPJ (14) dígitos.");
        if (papeis == PapelPessoa.Nenhum)
            return Result<Pessoa>.Falha("Informe ao menos um papel para a pessoa.");

        return Result<Pessoa>.Ok(new Pessoa
        {
            EmpresaId = empresaId,
            Nome = nome.Trim(),
            Documento = doc,
            Papeis = papeis,
        });
    }

    /// <summary>Acrescenta um papel (idempotente).</summary>
    public void AdicionarPapel(PapelPessoa papel)
    {
        Papeis |= papel;
        MarcarAtualizado();
    }

    public bool TemPapel(PapelPessoa papel) => (Papeis & papel) == papel;

    private static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
