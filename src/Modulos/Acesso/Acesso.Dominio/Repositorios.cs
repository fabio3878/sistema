using BuildingBlocks;

namespace Acesso.Dominio;

/// <summary>
/// Porta de hash de senha (o Dominio define o contrato; a Infraestrutura implementa com um KDF).
/// Mantém o domínio livre de detalhes de criptografia e permite trocar o algoritmo sem tocar nas regras.
/// </summary>
public interface IHashSenha
{
    /// <summary>Produz o hash (formato PHC: algoritmo+iterações+salt+hash numa string) de uma senha em claro.</summary>
    string Hash(string senha);

    /// <summary>Confere uma senha em claro contra o hash armazenado.</summary>
    bool Verificar(string senha, string hashArmazenado);
}

/// <summary>Portas de persistência de Usuario (o Dominio define, a Infraestrutura implementa).</summary>
public interface IUsuarioRepositorio
{
    Task Adicionar(Usuario usuario, CancellationToken ct = default);
    Task<Usuario?> ObterPorId(string empresaId, string id, CancellationToken ct = default);

    /// <summary>Busca pelo login normalizado (trim + minúsculas) — usado no login e na checagem de duplicidade.</summary>
    Task<Usuario?> ObterPorLogin(string empresaId, string loginNormalizado, CancellationToken ct = default);

    /// <summary>Existe algum usuário para o tenant? (decide o seed do admin no first-run).</summary>
    Task<bool> ExisteAlgum(string empresaId, CancellationToken ct = default);

    /// <summary>Lista os usuários do tenant (com seus perfis).</summary>
    Task<IReadOnlyList<Usuario>> Listar(string empresaId, CancellationToken ct = default);
}

/// <summary>Portas de persistência de Perfil.</summary>
public interface IPerfilRepositorio
{
    Task Adicionar(Perfil perfil, CancellationToken ct = default);
    Task<Perfil?> ObterPorId(string empresaId, string id, CancellationToken ct = default);
    Task<Perfil?> ObterPorNome(string empresaId, string nome, CancellationToken ct = default);

    /// <summary>Carrega vários perfis (com suas funcionalidades) para montar as permissões no login.</summary>
    Task<IReadOnlyList<Perfil>> ObterPorIds(string empresaId, IReadOnlyCollection<string> ids, CancellationToken ct = default);
}

/// <summary>Portas de persistência de RefreshToken.</summary>
public interface IRefreshTokenRepositorio
{
    Task Adicionar(RefreshToken token, CancellationToken ct = default);

    /// <summary>Busca pelo hash do token bruto (o valor recebido do cliente é hasheado antes).</summary>
    Task<RefreshToken?> ObterPorHash(string empresaId, string tokenHash, CancellationToken ct = default);

    /// <summary>Revoga todos os tokens ativos do usuário (logout total / troca de senha / suspeita de roubo).</summary>
    Task RevogarTodosDoUsuario(string empresaId, string usuarioId, string motivo, CancellationToken ct = default);
}

/// <summary>Confirma as mutações pendentes numa transação (Unit of Work do módulo).</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> Salvar(CancellationToken ct = default);
}

/// <summary>Porta de leitura paginada da trilha de auditoria do módulo (acs_auditoria).</summary>
public interface IAuditoriaRepositorio
{
    Task<PaginaResultado<AuditoriaDto>> Listar(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default);
}
