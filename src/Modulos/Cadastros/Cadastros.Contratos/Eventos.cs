using BuildingBlocks;

namespace Cadastros.Contratos;

/// <summary>Fato: um Cliente foi cadastrado. Consumível por outros módulos via Wolverine.</summary>
public sealed record ClienteCadastrado(string EmpresaId, string ClienteId) : IEventoDominio;

/// <summary>Fato: um Produto foi cadastrado.</summary>
public sealed record ProdutoCadastrado(string EmpresaId, string ProdutoId, string Sku) : IEventoDominio;
