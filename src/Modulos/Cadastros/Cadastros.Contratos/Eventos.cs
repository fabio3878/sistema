using BuildingBlocks;

namespace Cadastros.Contratos;

/// <summary>Fato: uma Pessoa foi cadastrada. Consumível por outros módulos via Wolverine.</summary>
public sealed record PessoaCadastrada(string EmpresaId, string PessoaId, PapelPessoa Papeis) : IEventoDominio;

/// <summary>Fato: um Produto foi cadastrado.</summary>
public sealed record ProdutoCadastrado(string EmpresaId, string ProdutoId, string Sku) : IEventoDominio;
