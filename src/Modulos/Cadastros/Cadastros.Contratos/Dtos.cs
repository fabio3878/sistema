namespace Cadastros.Contratos;

/// <summary>Projeção pública de um Cliente (com seus endereços).</summary>
public sealed record ClienteDto(
    string Id,
    string EmpresaId,
    string Nome,
    string Documento,
    TipoPessoa TipoPessoa,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    DateOnly? DataNascimento,
    string? Rg,
    string? OrgaoEmissorRg,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    IndicadorIe IndicadorIe,
    RegimeTributario? RegimeTributario,
    decimal? LimiteCredito,
    string? Observacoes,
    bool Ativo,
    IReadOnlyList<EnderecoDto> Enderecos);

/// <summary>Projeção pública de um endereço de cliente.</summary>
public sealed record EnderecoDto(
    string Id,
    TipoEndereco Tipo,
    string Cep,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Municipio,
    string Uf,
    string CodigoIbgeMunicipio,
    string Pais);

/// <summary>Projeção pública de um Produto/Serviço.</summary>
public sealed record ProdutoDto(
    string Id,
    string EmpresaId,
    string Sku,
    string Descricao,
    string? CodigoBarras,
    string Ncm,
    decimal PrecoVenda);
