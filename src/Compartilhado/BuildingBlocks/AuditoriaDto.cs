namespace BuildingBlocks;

/// <summary>
/// Projeção pública de uma linha da trilha de auditoria (leitura). Traz o diff inteiro
/// (<see cref="Alteracoes"/>, JSON) para a tela renderizar sem um segundo fetch.
/// </summary>
public sealed record AuditoriaDto(
    string Id,
    DateTimeOffset OcorridoEm,
    string? UsuarioId,
    string? UsuarioLogin,
    string Entidade,
    string RegistroId,
    OperacaoAuditoria Operacao,
    string Alteracoes);

/// <summary>
/// Filtro de consulta da trilha. Todos os campos são opcionais (nulo = não filtra). Paginação
/// 1-based; <see cref="Tamanho"/> é limitado no servidor.
/// </summary>
public sealed record FiltroAuditoria(
    string? Entidade = null,
    string? RegistroId = null,
    string? Usuario = null,
    OperacaoAuditoria? Operacao = null,
    DateTimeOffset? De = null,
    DateTimeOffset? Ate = null,
    int Pagina = 1,
    int Tamanho = 20);
