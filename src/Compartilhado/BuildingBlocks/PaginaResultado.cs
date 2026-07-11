namespace BuildingBlocks;

/// <summary>
/// Página de um resultado paginado no servidor: os itens da página + o total do conjunto filtrado
/// (para o cliente montar a navegação). <paramref name="Pagina"/> é 1-based.
/// </summary>
public sealed record PaginaResultado<T>(
    IReadOnlyList<T> Itens,
    int Total,
    int Pagina,
    int Tamanho);
