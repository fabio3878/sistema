using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Adapter scoped de <see cref="IContextoUsuario"/> que lê as claims do usuário autenticado do
/// <see cref="HttpContext"/>. Fora de requisição HTTP (seed, design-time, handler de fila) →
/// não autenticado, sem permissões. Nomes de claim batem com os emitidos no token (MapInboundClaims=false).
/// </summary>
public sealed class ContextoUsuarioHttp(IHttpContextAccessor acessor) : IContextoUsuario
{
    public const string ClaimSub = "sub";
    public const string ClaimLogin = "login";
    public const string ClaimEmpresa = "empresa";
    public const string ClaimStamp = "stamp";
    public const string ClaimPermAll = "perm_all";
    public const string ClaimFunc = "func";

    private ClaimsPrincipal? Usuario => acessor.HttpContext?.User;

    public bool Autenticado => Usuario?.Identity?.IsAuthenticated ?? false;

    public string? UsuarioId => Usuario?.FindFirst(ClaimSub)?.Value;
    public string? Login => Usuario?.FindFirst(ClaimLogin)?.Value;

    public bool ConcedeTodas =>
        string.Equals(Usuario?.FindFirst(ClaimPermAll)?.Value, "true", StringComparison.OrdinalIgnoreCase);

    public IReadOnlySet<string> Funcionalidades =>
        Usuario?.FindAll(ClaimFunc).Select(c => c.Value).ToHashSet() ?? [];

    public bool Pode(string funcionalidade) =>
        Autenticado && (ConcedeTodas || Funcionalidades.Contains(funcionalidade));
}
