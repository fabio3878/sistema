using BuildingBlocks;
using Cadastros.Contratos;

namespace Cadastros.Dominio;

/// <summary>Dados de um endereço (entrada da factory).</summary>
public sealed record DadosEndereco(
    TipoEndereco Tipo,
    string Cep,
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string CodigoIbgeMunicipio,
    string? Complemento = null,
    string Pais = "Brasil");

/// <summary>
/// Endereço de um <see cref="Cliente"/> (1:N). Criado sempre pelo aggregate root via
/// <see cref="Cliente.AdicionarEndereco"/>, que fornece EmpresaId e ClienteId.
/// </summary>
public sealed class ClienteEndereco : EntidadeBase
{
    public string ClienteId { get; private set; } = default!;
    public TipoEndereco Tipo { get; private set; }
    public string Cep { get; private set; } = default!;
    public string Logradouro { get; private set; } = default!;
    public string Numero { get; private set; } = default!;
    public string? Complemento { get; private set; }
    public string Bairro { get; private set; } = default!;
    public string Municipio { get; private set; } = default!;
    public string Uf { get; private set; } = default!;
    public string CodigoIbgeMunicipio { get; private set; } = default!;
    public string Pais { get; private set; } = "Brasil";

    // Construtor para o EF Core materializar.
    private ClienteEndereco() { }

    internal static Result<ClienteEndereco> Criar(string empresaId, string clienteId, DadosEndereco dados)
    {
        var cep = SomenteDigitos(dados.Cep);
        if (cep.Length != 8)
            return Result<ClienteEndereco>.Falha("CEP deve ter 8 dígitos.");

        var uf = (dados.Uf ?? string.Empty).Trim().ToUpperInvariant();
        if (uf.Length != 2 || !uf.All(char.IsLetter))
            return Result<ClienteEndereco>.Falha("UF deve ter 2 letras.");

        var ibge = SomenteDigitos(dados.CodigoIbgeMunicipio);
        if (ibge.Length != 7)
            return Result<ClienteEndereco>.Falha("Código IBGE do município deve ter 7 dígitos.");

        if (string.IsNullOrWhiteSpace(dados.Logradouro))
            return Result<ClienteEndereco>.Falha("Logradouro é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Numero))
            return Result<ClienteEndereco>.Falha("Número é obrigatório (use \"S/N\" se não houver).");
        if (string.IsNullOrWhiteSpace(dados.Bairro))
            return Result<ClienteEndereco>.Falha("Bairro é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Municipio))
            return Result<ClienteEndereco>.Falha("Município é obrigatório.");

        return Result<ClienteEndereco>.Ok(new ClienteEndereco
        {
            EmpresaId = empresaId,
            ClienteId = clienteId,
            Tipo = dados.Tipo,
            Cep = cep,
            Logradouro = dados.Logradouro.Trim(),
            Numero = dados.Numero.Trim(),
            Complemento = string.IsNullOrWhiteSpace(dados.Complemento) ? null : dados.Complemento.Trim(),
            Bairro = dados.Bairro.Trim(),
            Municipio = dados.Municipio.Trim(),
            Uf = uf,
            CodigoIbgeMunicipio = ibge,
            Pais = string.IsNullOrWhiteSpace(dados.Pais) ? "Brasil" : dados.Pais.Trim(),
        });
    }

    /// <summary>Revalida e reaplica os dados a um endereço existente (reusa as regras de <see cref="Criar"/>).</summary>
    internal Result Atualizar(DadosEndereco dados)
    {
        var validado = Criar(EmpresaId, ClienteId, dados);
        if (validado.Falhou)
            return Result.Falha(validado.Erro!);

        var v = validado.Valor!;
        Tipo = v.Tipo;
        Cep = v.Cep;
        Logradouro = v.Logradouro;
        Numero = v.Numero;
        Complemento = v.Complemento;
        Bairro = v.Bairro;
        Municipio = v.Municipio;
        Uf = v.Uf;
        CodigoIbgeMunicipio = v.CodigoIbgeMunicipio;
        Pais = v.Pais;
        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>Soft delete do endereço (tombstone). NUNCA remoção física — regra de sync.</summary>
    internal void MarcarRemovido()
    {
        Excluido = true;
        MarcarAtualizado();
    }

    private static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
