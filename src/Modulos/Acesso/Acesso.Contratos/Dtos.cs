namespace Acesso.Contratos;

/// <summary>Projeção pública de um usuário (NUNCA expõe hash de senha).</summary>
public sealed record UsuarioDto(
    string Id,
    string EmpresaId,
    string Login,
    string NomeExibicao,
    string? Email,
    bool Ativo,
    bool DeveTrocarSenha,
    DateTimeOffset? UltimoLoginEm,
    IReadOnlyList<string> Perfis);

/// <summary>Projeção pública de um perfil e suas funcionalidades concedidas.</summary>
public sealed record PerfilDto(
    string Id,
    string EmpresaId,
    string Nome,
    string? Descricao,
    bool Ativo,
    bool Protegido,
    bool ConcedeTodas,
    IReadOnlyList<string> Funcionalidades);
