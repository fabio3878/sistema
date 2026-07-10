namespace Cadastros.Dominio;

/// <summary>
/// TIER B — dado de referência GLOBAL (IBGE). Como <c>acs_modulos</c>, NÃO herda EntidadeBase
/// (sem EmpresaId, sem soft delete, sem ULID): chave natural é a UF. Semeado no startup.
/// </summary>
public sealed class Estado
{
    public string Uf { get; private set; } = default!;      // "SP"
    public string Nome { get; private set; } = default!;    // "São Paulo"
    public string CodigoIbge { get; private set; } = default!; // "35"

    private Estado() { }

    public static Estado Criar(string uf, string nome, string codigoIbge) => new()
    {
        Uf = uf.Trim().ToUpperInvariant(),
        Nome = nome.Trim(),
        CodigoIbge = codigoIbge.Trim(),
    };
}

/// <summary>
/// TIER B — município do IBGE (referência global). Chave natural = código IBGE de 7 dígitos.
/// Preenche <c>ClienteEndereco.CodigoIbgeMunicipio</c> pela seleção (nunca digitado à mão).
/// </summary>
public sealed class Municipio
{
    public string CodigoIbge { get; private set; } = default!; // "3550308"
    public string Nome { get; private set; } = default!;       // "São Paulo"
    public string Uf { get; private set; } = default!;         // "SP"

    private Municipio() { }

    public static Municipio Criar(string codigoIbge, string nome, string uf) => new()
    {
        CodigoIbge = codigoIbge.Trim(),
        Nome = nome.Trim(),
        Uf = uf.Trim().ToUpperInvariant(),
    };
}
