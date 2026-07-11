using BuildingBlocks;

namespace Acesso.Dominio;

/// <summary>Dados de criação de um usuário (entrada da factory).</summary>
public sealed record DadosUsuario(
    string Login,
    string NomeExibicao,
    string SenhaInicial,
    string? Email = null,
    bool DeveTrocarSenha = true);

/// <summary>
/// Usuário do sistema. Aggregate root que agrega seus vínculos de perfil (<see cref="UsuarioPerfil"/>).
/// A senha nunca é guardada em claro — só o hash, produzido por <see cref="IHashSenha"/>.
/// </summary>
public sealed class Usuario : EntidadeBase
{
    private readonly List<UsuarioPerfil> _perfis = [];

    public string Login { get; private set; } = default!;

    /// <summary>Login normalizado (trim + minúsculas) para lookup e unicidade case-insensitive.</summary>
    public string LoginNormalizado { get; private set; } = default!;

    public string NomeExibicao { get; private set; } = default!;
    public string? Email { get; private set; }

    /// <summary>Hash da senha no formato PHC (algoritmo+iterações+salt+hash numa string). Nunca em claro.</summary>
    [NaoAuditar] // segredo: fora da trilha de auditoria
    public string SenhaHash { get; private set; } = default!;
    public DateTimeOffset SenhaAlteradaEm { get; private set; }

    /// <summary>Status de negócio: inativo continua visível, mas bloqueado. Difere do soft delete.</summary>
    public bool Ativo { get; private set; } = true;

    public bool DeveTrocarSenha { get; private set; }
    public DateTimeOffset? UltimoLoginEm { get; private set; }

    /// <summary>Carimbo de segurança: muda ao trocar senha / forçar logout. Base para revogar tokens (futuro).</summary>
    public string StampSeguranca { get; private set; } = default!;

    public IReadOnlyCollection<UsuarioPerfil> Perfis => _perfis.AsReadOnly();

    // Construtor para o EF Core materializar.
    private Usuario() { }

    /// <summary>Cria um usuário validado, já com a senha em hash. Regras via <see cref="Result{T}"/>.</summary>
    public static Result<Usuario> Criar(string empresaId, DadosUsuario dados, IHashSenha hash)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<Usuario>.Falha("EmpresaId é obrigatório.");

        var login = (dados.Login ?? string.Empty).Trim();
        if (login.Length < 3)
            return Result<Usuario>.Falha("Login deve ter ao menos 3 caracteres.");

        if (string.IsNullOrWhiteSpace(dados.NomeExibicao))
            return Result<Usuario>.Falha("Nome de exibição é obrigatório.");

        if (string.IsNullOrWhiteSpace(dados.SenhaInicial) || dados.SenhaInicial.Length < 6)
            return Result<Usuario>.Falha("Senha deve ter ao menos 6 caracteres.");

        if (!string.IsNullOrWhiteSpace(dados.Email) && !dados.Email.Contains('@'))
            return Result<Usuario>.Falha("E-mail inválido.");

        var agora = DateTimeOffset.UtcNow;
        return Result<Usuario>.Ok(new Usuario
        {
            EmpresaId = empresaId,
            Login = login,
            LoginNormalizado = login.ToLowerInvariant(),
            NomeExibicao = dados.NomeExibicao.Trim(),
            Email = string.IsNullOrWhiteSpace(dados.Email) ? null : dados.Email.Trim(),
            SenhaHash = hash.Hash(dados.SenhaInicial),
            SenhaAlteradaEm = agora,
            DeveTrocarSenha = dados.DeveTrocarSenha,
            StampSeguranca = Ulid.NewUlid().ToString(),
        });
    }

    /// <summary>Atribui um perfil ao usuário (idempotente: ignora se já vinculado e ativo).</summary>
    public Result AtribuirPerfil(string perfilId)
    {
        if (string.IsNullOrWhiteSpace(perfilId))
            return Result.Falha("PerfilId é obrigatório.");

        var existente = _perfis.FirstOrDefault(p => p.PerfilId == perfilId);
        if (existente is not null)
        {
            if (!existente.Excluido) return Result.Ok();
            existente.Restaurar();            // reativa vínculo antes revogado
        }
        else
        {
            _perfis.Add(UsuarioPerfil.Criar(EmpresaId, Id, perfilId));
        }

        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>Revoga um perfil (soft delete do vínculo — nunca DELETE físico no que sincroniza).</summary>
    public void RemoverPerfil(string perfilId)
    {
        var vinculo = _perfis.FirstOrDefault(p => p.PerfilId == perfilId && !p.Excluido);
        if (vinculo is null) return;
        vinculo.Excluir();
        MarcarAtualizado();
    }

    public void RegistrarLogin()
    {
        UltimoLoginEm = DateTimeOffset.UtcNow;
        MarcarAtualizado();
    }

    /// <summary>Troca a senha (recebe o hash já calculado) e **rotaciona o StampSeguranca**, o que
    /// invalida todos os tokens/refresh emitidos antes. Limpa a exigência de troca.</summary>
    public void AlterarSenha(string senhaHash)
    {
        SenhaHash = senhaHash;
        SenhaAlteradaEm = DateTimeOffset.UtcNow;
        DeveTrocarSenha = false;
        StampSeguranca = Ulid.NewUlid().ToString();
        MarcarAtualizado();
    }

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
        StampSeguranca = Ulid.NewUlid().ToString(); // invalida sessões/tokens existentes (futuro)
        MarcarAtualizado();
    }
}

/// <summary>
/// Vínculo usuário↔perfil (N:N). Herda <see cref="EntidadeBase"/> de propósito: conceder/revogar
/// é estado de tenant que precisa sincronizar e tombar via soft delete. Criado sempre pelo
/// aggregate root <see cref="Usuario"/>.
/// </summary>
public sealed class UsuarioPerfil : EntidadeBase
{
    public string UsuarioId { get; private set; } = default!;
    public string PerfilId { get; private set; } = default!;

    // Construtor para o EF Core materializar.
    private UsuarioPerfil() { }

    internal static UsuarioPerfil Criar(string empresaId, string usuarioId, string perfilId) => new()
    {
        EmpresaId = empresaId,
        UsuarioId = usuarioId,
        PerfilId = perfilId,
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
