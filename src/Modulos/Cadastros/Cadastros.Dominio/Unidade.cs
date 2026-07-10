namespace Cadastros.Dominio;

/// <summary>
/// TIER B — unidade de medida (referência GLOBAL). Como <c>cad_estados</c>/<c>acs_modulos</c>,
/// NÃO herda EntidadeBase (sem EmpresaId, sem soft delete, sem ULID): chave natural = sigla.
/// Semeada no startup. O produto referencia a <see cref="Sigla"/>; <see cref="CasasDecimais"/>
/// governa quantas casas a quantidade exibe (UN=0, KG=3) — a quantidade em si mora no Estoque.
/// </summary>
public sealed class Unidade
{
    public string Sigla { get; private set; } = default!;   // "UN", "KG"
    public string Descricao { get; private set; } = default!; // "Unidade", "Quilograma"
    public int CasasDecimais { get; private set; }            // 0 = inteiro; 3 = fraciona
    public bool Fracionavel { get; private set; }             // false para UN/CX/PC

    private Unidade() { }

    public static Unidade Criar(string sigla, string descricao, int casasDecimais, bool fracionavel) => new()
    {
        Sigla = sigla.Trim().ToUpperInvariant(),
        Descricao = descricao.Trim(),
        CasasDecimais = casasDecimais,
        Fracionavel = fracionavel,
    };
}
