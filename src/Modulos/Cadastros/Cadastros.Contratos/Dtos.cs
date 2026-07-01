namespace Cadastros.Contratos;

/// <summary>Projeção pública de uma Pessoa (cliente/fornecedor/etc.).</summary>
public sealed record PessoaDto(
    string Id,
    string EmpresaId,
    string Nome,
    string Documento,
    PapelPessoa Papeis);

/// <summary>Projeção pública de um Produto/Serviço.</summary>
public sealed record ProdutoDto(
    string Id,
    string EmpresaId,
    string Sku,
    string Descricao,
    string? CodigoBarras,
    string Ncm,
    decimal PrecoVenda);
