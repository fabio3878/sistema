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
    TipoPessoa TipoPessoa = TipoPessoa.Fisica,
    IndicadorIe IndicadorIe = IndicadorIe.NaoContribuinte,
    string? NomeFantasia = null,
    string? Email = null,
    string? EmailFinanceiro = null,
    string? Telefone = null,
    string? Celular = null,
    string? Whatsapp = null,
    string? Site = null,
    DateOnly? DataNascimento = null,
    string? Rg = null,
    string? OrgaoEmissorRg = null,
    string? InscricaoEstadual = null,
    string? InscricaoMunicipal = null,
    RegimeTributario? RegimeTributario = null,
    decimal? LimiteCredito = null,
    string? Origem = null,
    string? Preferencias = null,
    string? Observacoes = null,
    bool AceitaEmail = false,
    bool AceitaSms = false,
    bool AceitaWhatsapp = false,
    bool AceitaLigacoes = false,
    bool AceitouTermosLgpd = false,
    DateTimeOffset? DataAceiteLgpd = null);

/// <summary>
/// Endereço vindo do cliente numa edição: <c>Id</c> nulo = endereço novo; com <c>Id</c> =
/// endereço existente a atualizar. Endereços ausentes da lista sofrem soft delete.
/// </summary>
public sealed record EnderecoSync(string? Id, DadosEndereco Dados);

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

    // Contato
    public string? Email { get; private set; }
    public string? EmailFinanceiro { get; private set; }
    public string? Telefone { get; private set; }
    public string? Celular { get; private set; }
    public string? Whatsapp { get; private set; }
    public string? Site { get; private set; }

    // Pessoa física
    public DateOnly? DataNascimento { get; private set; }
    public string? Rg { get; private set; }
    public string? OrgaoEmissorRg { get; private set; }

    // Fiscal (pessoa jurídica)
    public string? InscricaoEstadual { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public IndicadorIe IndicadorIe { get; private set; }
    public RegimeTributario? RegimeTributario { get; private set; }

    // Adicionais
    public decimal? LimiteCredito { get; private set; }
    public string? Origem { get; private set; }
    public string? Preferencias { get; private set; }
    public string? Observacoes { get; private set; }

    // Marketing / LGPD
    public bool AceitaEmail { get; private set; }
    public bool AceitaSms { get; private set; }
    public bool AceitaWhatsapp { get; private set; }
    public bool AceitaLigacoes { get; private set; }
    public bool AceitouTermosLgpd { get; private set; }
    public DateTimeOffset? DataAceiteLgpd { get; private set; }

    /// <summary>Status de negócio: um cliente inativo continua visível, mas bloqueado. Difere do soft delete (<see cref="EntidadeBase.Excluido"/>).</summary>
    public bool Ativo { get; private set; } = true;

    public IReadOnlyCollection<ClienteEndereco> Enderecos => _enderecos.AsReadOnly();

    // Construtor para o EF Core materializar.
    private Cliente() { }

    /// <summary>Cria um Cliente validado. Regras de negócio via <see cref="Result{T}"/>, não exceção.</summary>
    public static Result<Cliente> Criar(string empresaId, DadosCliente dados)
    {
        var validacao = Validar(empresaId, dados);
        if (validacao.Falhou)
            return Result<Cliente>.Falha(validacao.Erro!);

        var cliente = new Cliente { EmpresaId = empresaId };
        cliente.AplicarDados(dados);
        return Result<Cliente>.Ok(cliente);
    }

    /// <summary>Atualiza os dados de contato/fiscais. Não mexe em <see cref="Ativo"/> (ver <see cref="Ativar"/>/<see cref="Inativar"/>) nem em CriadoEm (imutável).</summary>
    public Result Atualizar(DadosCliente dados)
    {
        var validacao = Validar(EmpresaId, dados);
        if (validacao.Falhou)
            return validacao;

        AplicarDados(dados);
        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>Regras de negócio compartilhadas por <see cref="Criar"/> e <see cref="Atualizar"/>.</summary>
    private static Result Validar(string empresaId, DadosCliente dados)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Nome))
            return Result.Falha("Nome é obrigatório.");

        // Documento coerente com o tipo escolhido + dígito verificador.
        var doc = SomenteDigitos(dados.Documento);
        if (dados.TipoPessoa == TipoPessoa.Fisica)
        {
            if (doc.Length != 11)
                return Result.Falha("Pessoa física exige um CPF (11 dígitos).");
            if (!CpfValido(doc))
                return Result.Falha("CPF inválido.");
        }
        else
        {
            if (doc.Length != 14)
                return Result.Falha("Pessoa jurídica exige um CNPJ (14 dígitos).");
            if (!CnpjValido(doc))
                return Result.Falha("CNPJ inválido.");
        }

        var ie = string.IsNullOrWhiteSpace(dados.InscricaoEstadual) ? null : dados.InscricaoEstadual.Trim();
        if (dados.IndicadorIe == IndicadorIe.Contribuinte && ie is null)
            return Result.Falha("Contribuinte de ICMS exige Inscrição Estadual.");

        if (!string.IsNullOrWhiteSpace(dados.Email) && !dados.Email.Contains('@'))
            return Result.Falha("E-mail inválido.");
        if (!string.IsNullOrWhiteSpace(dados.EmailFinanceiro) && !dados.EmailFinanceiro.Contains('@'))
            return Result.Falha("E-mail financeiro inválido.");

        if (dados.LimiteCredito is < 0)
            return Result.Falha("Limite de crédito não pode ser negativo.");

        return Result.Ok();
    }

    /// <summary>Aplica os dados já validados aos campos (normalizando documento/telefones).</summary>
    private void AplicarDados(DadosCliente dados)
    {
        Nome = dados.Nome.Trim();
        Documento = SomenteDigitos(dados.Documento);
        TipoPessoa = dados.TipoPessoa;
        NomeFantasia = Limpar(dados.NomeFantasia);

        Email = Limpar(dados.Email);
        EmailFinanceiro = Limpar(dados.EmailFinanceiro);
        Telefone = SomenteDigitosOuNulo(dados.Telefone);
        Celular = SomenteDigitosOuNulo(dados.Celular);
        Whatsapp = SomenteDigitosOuNulo(dados.Whatsapp);
        Site = Limpar(dados.Site);

        DataNascimento = dados.DataNascimento;
        Rg = Limpar(dados.Rg);
        OrgaoEmissorRg = Limpar(dados.OrgaoEmissorRg);

        InscricaoEstadual = Limpar(dados.InscricaoEstadual);
        InscricaoMunicipal = Limpar(dados.InscricaoMunicipal);
        IndicadorIe = dados.IndicadorIe;
        RegimeTributario = dados.RegimeTributario;

        LimiteCredito = dados.LimiteCredito;
        Origem = Limpar(dados.Origem);
        Preferencias = Limpar(dados.Preferencias);
        Observacoes = Limpar(dados.Observacoes);

        AceitaEmail = dados.AceitaEmail;
        AceitaSms = dados.AceitaSms;
        AceitaWhatsapp = dados.AceitaWhatsapp;
        AceitaLigacoes = dados.AceitaLigacoes;
        AceitouTermosLgpd = dados.AceitouTermosLgpd;
        DataAceiteLgpd = dados.DataAceiteLgpd;
    }

    /// <summary>Valida CPF pelos dois dígitos verificadores (rejeita sequências repetidas).</summary>
    private static bool CpfValido(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1) return false;
        int Digito(int ate, int pesoInicial)
        {
            var soma = 0;
            for (var i = 0; i < ate; i++) soma += (cpf[i] - '0') * (pesoInicial - i);
            var r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }
        return Digito(9, 10) == cpf[9] - '0' && Digito(10, 11) == cpf[10] - '0';
    }

    /// <summary>Valida CNPJ pelos dois dígitos verificadores (rejeita sequências repetidas).</summary>
    private static bool CnpjValido(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1) return false;
        int[] p1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] p2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int Digito(int[] pesos)
        {
            var soma = 0;
            for (var i = 0; i < pesos.Length; i++) soma += (cnpj[i] - '0') * pesos[i];
            var r = soma % 11;
            return r < 2 ? 0 : 11 - r;
        }
        return Digito(p1) == cnpj[12] - '0' && Digito(p2) == cnpj[13] - '0';
    }

    private static string? SomenteDigitosOuNulo(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : SomenteDigitos(valor);

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

    /// <summary>
    /// Reconcilia a coleção de endereços com o que veio da edição (all-or-nothing): valida tudo
    /// antes de aplicar. Endereços com Id são atualizados, sem Id são criados, e os ausentes da
    /// lista sofrem <b>soft delete</b> (nunca remoção física — regra inegociável de sync).
    /// </summary>
    public Result SincronizarEnderecos(IReadOnlyList<EnderecoSync> entradas)
    {
        var edicoes = new List<(ClienteEndereco Alvo, DadosEndereco Dados)>();
        var novos = new List<DadosEndereco>();

        foreach (var entrada in entradas)
        {
            if (entrada.Id is null)
            {
                novos.Add(entrada.Dados);
                continue;
            }

            var alvo = _enderecos.FirstOrDefault(e => e.Id == entrada.Id);
            if (alvo is null)
                return Result.Falha("Endereço não encontrado para atualização.");
            edicoes.Add((alvo, entrada.Dados));
        }

        // Valida tudo antes de qualquer mutação (reaproveita as regras da factory).
        foreach (var dados in edicoes.Select(e => e.Dados).Concat(novos))
        {
            var validado = ClienteEndereco.Criar(EmpresaId, Id, dados);
            if (validado.Falhou)
                return Result.Falha(validado.Erro!);
        }

        // Soft delete dos que sumiram da lista.
        var idsMantidos = entradas.Where(e => e.Id is not null).Select(e => e.Id!).ToHashSet();
        foreach (var ausente in _enderecos.Where(e => !idsMantidos.Contains(e.Id)).ToList())
            ausente.MarcarRemovido();

        foreach (var (alvo, dados) in edicoes)
            alvo.Atualizar(dados);

        foreach (var dados in novos)
            AdicionarEndereco(dados);

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
