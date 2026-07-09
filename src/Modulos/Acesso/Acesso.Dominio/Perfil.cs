using BuildingBlocks;

namespace Acesso.Dominio;

/// <summary>
/// Perfil (papel) de acesso. Aggregate root que agrega suas concessões de funcionalidade
/// (<see cref="PerfilFuncionalidade"/>). A permissão efetiva de um usuário é a união dos seus perfis.
/// </summary>
public sealed class Perfil : EntidadeBase
{
    private readonly List<PerfilFuncionalidade> _funcionalidades = [];

    public string Nome { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public bool Ativo { get; private set; } = true;

    /// <summary>Perfil protegido (ex.: "Administrador" do seed) não pode ser apagado/renomeado.</summary>
    public bool Protegido { get; private set; }

    /// <summary>Super-perfil: concede TODAS as funcionalidades sem precisar listar uma a uma
    /// (upgrades trazem funcionalidades novas já cobertas, sem re-seed).</summary>
    public bool ConcedeTodas { get; private set; }

    public IReadOnlyCollection<PerfilFuncionalidade> Funcionalidades => _funcionalidades.AsReadOnly();

    // Construtor para o EF Core materializar.
    private Perfil() { }

    public static Result<Perfil> Criar(
        string empresaId, string nome, string? descricao = null,
        bool concedeTodas = false, bool protegido = false)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<Perfil>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome))
            return Result<Perfil>.Falha("Nome do perfil é obrigatório.");

        return Result<Perfil>.Ok(new Perfil
        {
            EmpresaId = empresaId,
            Nome = nome.Trim(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            ConcedeTodas = concedeTodas,
            Protegido = protegido,
        });
    }

    /// <summary>Concede uma funcionalidade ao perfil (idempotente). Referencia o catálogo pelo CÓDIGO.</summary>
    public Result Conceder(string funcionalidadeCodigo)
    {
        var codigo = (funcionalidadeCodigo ?? string.Empty).Trim();
        if (codigo.Length == 0)
            return Result.Falha("Código de funcionalidade é obrigatório.");

        var existente = _funcionalidades.FirstOrDefault(f => f.FuncionalidadeCodigo == codigo);
        if (existente is not null)
        {
            if (!existente.Excluido) return Result.Ok();
            existente.Restaurar();
        }
        else
        {
            _funcionalidades.Add(PerfilFuncionalidade.Criar(EmpresaId, Id, codigo));
        }

        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>Revoga uma funcionalidade (soft delete do vínculo).</summary>
    public void Revogar(string funcionalidadeCodigo)
    {
        var codigo = (funcionalidadeCodigo ?? string.Empty).Trim();
        var vinculo = _funcionalidades.FirstOrDefault(f => f.FuncionalidadeCodigo == codigo && !f.Excluido);
        if (vinculo is null) return;
        vinculo.Excluir();
        MarcarAtualizado();
    }

    /// <summary>Este perfil concede a funcionalidade? (super-perfil concede tudo).</summary>
    public bool Concede(string funcionalidadeCodigo) =>
        ConcedeTodas || _funcionalidades.Any(f => !f.Excluido && f.FuncionalidadeCodigo == funcionalidadeCodigo);

    public void Ativar()
    {
        if (Ativo) return;
        Ativo = true;
        MarcarAtualizado();
    }

    public void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        MarcarAtualizado();
    }
}

/// <summary>
/// Concessão perfil↔funcionalidade (N:N). Herda <see cref="EntidadeBase"/> para sincronizar/tombar.
/// Guarda o CÓDIGO da funcionalidade (string estável em todo banco), não FK-para-ULID — um ULID de
/// catálogo divergiria por ambiente/servidor e poluiria o sync.
/// </summary>
public sealed class PerfilFuncionalidade : EntidadeBase
{
    public string PerfilId { get; private set; } = default!;
    public string FuncionalidadeCodigo { get; private set; } = default!;

    // Construtor para o EF Core materializar.
    private PerfilFuncionalidade() { }

    internal static PerfilFuncionalidade Criar(string empresaId, string perfilId, string funcionalidadeCodigo) => new()
    {
        EmpresaId = empresaId,
        PerfilId = perfilId,
        FuncionalidadeCodigo = funcionalidadeCodigo,
    };

    internal void Excluir()
    {
        Excluido = true;
        MarcarAtualizado();
    }

    internal void Restaurar()
    {
        Excluido = false;
        MarcarAtualizado();
    }
}
