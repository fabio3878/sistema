using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>Registro dos serviços da Plataforma (shared kernel) no container de DI.</summary>
public static class PlataformaServiceCollectionExtensions
{
    /// <summary>
    /// Registra licença + contextos transversais. Tenant e usuário são **scoped**, derivados das
    /// claims do token (via HttpContext); sem requisição HTTP caem no tenant configurado do servidor
    /// (<c>Plataforma:EmpresaId</c>, padrão <see cref="ContextoEmpresaFixo.EmpresaPadrao"/>).
    /// </summary>
    public static IServiceCollection AdicionarPlataforma(this IServiceCollection services, IConfiguration config)
    {
        var empresaPadrao = config["Plataforma:EmpresaId"];
        if (string.IsNullOrWhiteSpace(empresaPadrao))
            empresaPadrao = ContextoEmpresaFixo.EmpresaPadrao;

        services.AddSingleton<ILicenca, LicencaLocal>();
        services.AddHttpContextAccessor();

        services.AddScoped<IContextoEmpresa>(sp =>
            new ContextoEmpresaHttp(sp.GetRequiredService<IHttpContextAccessor>(), empresaPadrao));
        services.AddScoped<IContextoUsuario, ContextoUsuarioHttp>();

        return services;
    }

    /// <summary>
    /// Habilita autorização por funcionalidade: políticas <c>func:&lt;codigo&gt;</c> resolvidas sob
    /// demanda contra <see cref="IContextoUsuario"/>. Chamar junto de AddAuthentication no host.
    /// </summary>
    public static IServiceCollection AdicionarAutorizacaoPorFuncionalidade(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, FuncionalidadePolicyProvider>();
        services.AddScoped<IAuthorizationHandler, FuncionalidadeHandler>();
        return services;
    }
}
