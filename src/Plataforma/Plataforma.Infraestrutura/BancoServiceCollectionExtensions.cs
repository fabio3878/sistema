using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Plataforma.Dominio;
using Plataforma.Infraestrutura.Auditoria;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Registra um DbContext com o provider escolhido por configuração (seção 11):
/// Postgres (servidor da loja — principal) ou SQLite (contingência local futura por PDV).
/// Mesmo código de módulo — a decisão de provider mora só aqui. Todo módulo novo usa este helper.
/// </summary>
public static class BancoServiceCollectionExtensions
{
    public static IServiceCollection AdicionarDbContextConfiguravel<TContexto>(
        this IServiceCollection services, IConfiguration config)
        where TContexto : DbContext
    {
        var opcoes = config.GetSection(OpcoesBanco.Secao).Get<OpcoesBanco>() ?? new OpcoesBanco();

        // Trilha de auditoria: transversal a todo DbContext que passa por aqui (fonte única).
        // Scoped para enxergar o usuário/tenant da requisição corrente.
        services.TryAddScoped<AuditoriaInterceptor>();

        services.AddDbContext<TContexto>((sp, o) =>
        {
            switch (opcoes.Provider)
            {
                case ProviderBanco.Postgres:
                    o.UseNpgsql(opcoes.ConnectionString);
                    break;
                default:
                    // SQLite: contingência local por PDV. Ainda sem migrations próprias — a
                    // migrations-assembly do SQLite entra quando o modo offline for construído.
                    o.UseSqlite(opcoes.ConnectionString);
                    break;
            }

            o.AddInterceptors(sp.GetRequiredService<AuditoriaInterceptor>());
        });

        return services;
    }
}
