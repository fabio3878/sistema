namespace Plataforma.Dominio;

/// <summary>Provider de banco em uso (seção 11). Mesmo DbContext, provider trocável.</summary>
public enum ProviderBanco
{
    /// <summary>SQLite — banco local do AgenteLocal (arquivo único, in-process).</summary>
    Sqlite = 0,

    /// <summary>PostgreSQL — banco central (Api.Central), alvo de sync.</summary>
    Postgres = 1,
}

/// <summary>
/// Opções de conexão com o banco, lidas da configuração (seção "Banco" do appsettings).
/// POCO puro — sem dependência de EF — para viver no shared kernel.
/// </summary>
public sealed class OpcoesBanco
{
    /// <summary>Nome da seção na configuração.</summary>
    public const string Secao = "Banco";

    public ProviderBanco Provider { get; set; } = ProviderBanco.Sqlite;

    public string ConnectionString { get; set; } = "Data Source=automacao.db";
}
