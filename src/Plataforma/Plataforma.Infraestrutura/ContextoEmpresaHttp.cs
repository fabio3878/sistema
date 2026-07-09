using Microsoft.AspNetCore.Http;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Adapter scoped de <see cref="IContextoEmpresa"/>: usa o tenant da claim <c>empresa</c> do usuário
/// autenticado; sem HttpContext/claim (seed, design-time, handler de fila) cai no tenant configurado
/// do servidor (<c>Plataforma:EmpresaId</c>). Num servidor de loja single-tenant os dois coincidem.
/// </summary>
public sealed class ContextoEmpresaHttp(IHttpContextAccessor acessor, string empresaPadrao) : IContextoEmpresa
{
    public string EmpresaId
    {
        get
        {
            var claim = acessor.HttpContext?.User?.FindFirst(ContextoUsuarioHttp.ClaimEmpresa)?.Value;
            return string.IsNullOrEmpty(claim) ? empresaPadrao : claim;
        }
    }
}
