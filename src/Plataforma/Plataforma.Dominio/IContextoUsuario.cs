namespace Plataforma.Dominio;

/// <summary>
/// Usuário autenticado da operação corrente e suas permissões efetivas. Seam transversal
/// (irmão de <see cref="IContextoEmpresa"/>): qualquer módulo checa acesso por AQUI, sem
/// depender do módulo Acesso. Populado a partir das claims do token pela Infraestrutura.
/// </summary>
public interface IContextoUsuario
{
    bool Autenticado { get; }
    string? UsuarioId { get; }
    string? Login { get; }

    /// <summary>Super-perfil: concede qualquer funcionalidade (vem da claim perm_all).</summary>
    bool ConcedeTodas { get; }

    /// <summary>Códigos de funcionalidade concedidos (união dos perfis), quando não é ConcedeTodas.</summary>
    IReadOnlySet<string> Funcionalidades { get; }

    /// <summary>O usuário pode a funcionalidade? (ConcedeTodas OU o código está no conjunto).</summary>
    bool Pode(string funcionalidade);
}
