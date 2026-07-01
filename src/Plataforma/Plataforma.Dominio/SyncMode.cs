namespace Plataforma.Dominio;

/// <summary>
/// Modo de sincronização (seção 5/8/9). O mesmo binário roda offline puro ou com sync;
/// só muda a configuração.
/// </summary>
public enum SyncMode
{
    /// <summary>Só banco local. O worker de sync nem roda.</summary>
    LocalOnly = 0,

    /// <summary>Local + push/pull de deltas contra a central (Postgres).</summary>
    LocalComSync = 1,
}
