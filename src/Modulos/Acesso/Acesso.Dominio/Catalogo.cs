namespace Acesso.Dominio;

/// <summary>
/// TIER B — catálogo de capacidade. Dado de referência GLOBAL (não de tenant): reflete o que o
/// software sabe fazer, reconciliado do código no startup. Propositalmente NÃO herda EntidadeBase
/// (sem EmpresaId, sem soft delete, sem ULID) — chave natural é o próprio código.
/// </summary>
public sealed class Modulo
{
    public string Codigo { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }

    // Construtor para o EF Core materializar.
    private Modulo() { }

    public static Modulo Criar(string codigo, string nome, string? descricao = null) => new()
    {
        Codigo = codigo.Trim(),
        Nome = nome.Trim(),
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
    };

    public void Atualizar(string nome, string? descricao)
    {
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
    }
}

/// <summary>
/// TIER B — uma funcionalidade do catálogo (permissão granular), agrupada por <see cref="Modulo"/>.
/// Chave natural = <see cref="Codigo"/> (ex.: "cad.cliente.criar"), idêntico em todo banco/servidor.
/// </summary>
public sealed class Funcionalidade
{
    public string Codigo { get; private set; } = default!;
    public string ModuloCodigo { get; private set; } = default!;
    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }

    /// <summary>Marca quando o código sai do manifesto do código. Nunca apaga: grants antigos ainda apontam.</summary>
    public bool Obsoleta { get; private set; }

    // Construtor para o EF Core materializar.
    private Funcionalidade() { }

    public static Funcionalidade Criar(string codigo, string moduloCodigo, string nome, string? descricao = null) => new()
    {
        Codigo = codigo.Trim(),
        ModuloCodigo = moduloCodigo.Trim(),
        Nome = nome.Trim(),
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
    };

    public void Atualizar(string moduloCodigo, string nome, string? descricao)
    {
        ModuloCodigo = moduloCodigo.Trim();
        Nome = nome.Trim();
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        Obsoleta = false; // reapareceu no manifesto → volta a valer
    }

    public void MarcarObsoleta() => Obsoleta = true;
}
