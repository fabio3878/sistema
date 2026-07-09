using BuildingBlocks;
using Cadastros.Contratos;

namespace Cadastros.Dominio;

/// <summary>
/// Dados de contato/fiscais de um cliente (entrada da factory). Agrupa os campos opcionais
/// para a assinatura de <see cref="Cliente.Criar"/> não explodir em parâmetros posicionais.
/// </summary>
public sealed record DadosCliente(
    string Nome,
    string Documento,
    IndicadorIe IndicadorIe = IndicadorIe.NaoContribuinte,
    string? NomeFantasia = null,
    string? Email = null,
    string? Telefone = null,
    DateOnly? DataNascimento = null,
    string? Rg = null,
    string? OrgaoEmissorRg = null,
    string? InscricaoEstadual = null,
    string? InscricaoMunicipal = null,
    RegimeTributario? RegimeTributario = null,
    decimal? LimiteCredito = null,
    string? Observacoes = null);

/// <summary>
/// Cliente é um cadastro próprio e independente (seção 6.2). Fornecedor/funcionário terão
/// entidades separadas — não há mais "Pessoa unificada com papéis".
/// </summary>
public sealed class Cliente : EntidadeBase
{
    private readonly List<ClienteEndereco> _enderecos = [];

    public string Nome { get; private set; } = default!;

    /// <summary>CPF ou CNPJ (só dígitos).</summary>
    public string Documento { get; private set; } = default!;

    public TipoPessoa TipoPessoa { get; private set; }

    public string? NomeFantasia { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    public string? Rg { get; private set; }
    public string? OrgaoEmissorRg { get; private set; }
    public string? InscricaoEstadual { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public IndicadorIe IndicadorIe { get; private set; }
    public RegimeTributario? RegimeTributario { get; private set; }
    public decimal? LimiteCredito { get; private set; }
    public string? Observacoes { get; private set; }

    /// <summary>Status de negócio: um cliente inativo continua visível, mas bloqueado. Difere do soft delete (<see cref="EntidadeBase.Excluido"/>).</summary>
    public bool Ativo { get; private set; } = true;

    public IReadOnlyCollection<ClienteEndereco> Enderecos => _enderecos.AsReadOnly();

    // Construtor para o EF Core materializar.
    private Cliente() { }

    /// <summary>Cria um Cliente validado. Regras de negócio via <see cref="Result{T}"/>, não exceção.</summary>
    public static Result<Cliente> Criar(string empresaId, DadosCliente dados)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<Cliente>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Nome))
            return Result<Cliente>.Falha("Nome é obrigatório.");

        var doc = SomenteDigitos(dados.Documento);
        if (doc.Length is not (11 or 14))
            return Result<Cliente>.Falha("Documento deve ser um CPF (11) ou CNPJ (14) dígitos.");

        var ie = string.IsNullOrWhiteSpace(dados.InscricaoEstadual) ? null : dados.InscricaoEstadual.Trim();
        if (dados.IndicadorIe == IndicadorIe.Contribuinte && ie is null)
            return Result<Cliente>.Falha("Contribuinte de ICMS exige Inscrição Estadual.");

        if (!string.IsNullOrWhiteSpace(dados.Email) && !dados.Email.Contains('@'))
            return Result<Cliente>.Falha("E-mail inválido.");

        if (dados.LimiteCredito is < 0)
            return Result<Cliente>.Falha("Limite de crédito não pode ser negativo.");

        return Result<Cliente>.Ok(new Cliente
        {
            EmpresaId = empresaId,
            Nome = dados.Nome.Trim(),
            Documento = doc,
            TipoPessoa = doc.Length == 11 ? TipoPessoa.Fisica : TipoPessoa.Juridica,
            NomeFantasia = Limpar(dados.NomeFantasia),
            Email = Limpar(dados.Email),
            Telefone = string.IsNullOrWhiteSpace(dados.Telefone) ? null : SomenteDigitos(dados.Telefone),
            DataNascimento = dados.DataNascimento,
            Rg = Limpar(dados.Rg),
            OrgaoEmissorRg = Limpar(dados.OrgaoEmissorRg),
            InscricaoEstadual = ie,
            InscricaoMunicipal = Limpar(dados.InscricaoMunicipal),
            IndicadorIe = dados.IndicadorIe,
            RegimeTributario = dados.RegimeTributario,
            LimiteCredito = dados.LimiteCredito,
            Observacoes = Limpar(dados.Observacoes),
        });
    }

    /// <summary>Acrescenta um endereço validado ao cliente.</summary>
    public Result AdicionarEndereco(DadosEndereco dados)
    {
        var criacao = ClienteEndereco.Criar(EmpresaId, Id, dados);
        if (criacao.Falhou)
            return Result.Falha(criacao.Erro!);

        _enderecos.Add(criacao.Valor!);
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

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
