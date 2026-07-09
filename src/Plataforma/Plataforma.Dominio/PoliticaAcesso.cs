namespace Plataforma.Dominio;

/// <summary>
/// Convenção de nome das políticas de autorização por funcionalidade. Um lugar só para o prefixo,
/// usado tanto pelo provedor de políticas (Infra) quanto pelos endpoints (RequireAuthorization).
/// </summary>
public static class PoliticaAcesso
{
    public const string PrefixoFuncionalidade = "func:";

    /// <summary>Nome de política que exige a funcionalidade (ex.: "func:acs.usuario.gerenciar").</summary>
    public static string Funcionalidade(string codigo) => PrefixoFuncionalidade + codigo;
}
