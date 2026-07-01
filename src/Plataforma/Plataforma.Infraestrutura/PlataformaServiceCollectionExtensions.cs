using Microsoft.Extensions.DependencyInjection;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>Registro dos serviços da Plataforma (shared kernel) no container de DI.</summary>
public static class PlataformaServiceCollectionExtensions
{
    public static IServiceCollection AdicionarPlataforma(this IServiceCollection services)
    {
        services.AddSingleton<ILicenca, LicencaLocal>();
        services.AddSingleton<IContextoEmpresa>(new ContextoEmpresaFixo());
        return services;
    }
}
