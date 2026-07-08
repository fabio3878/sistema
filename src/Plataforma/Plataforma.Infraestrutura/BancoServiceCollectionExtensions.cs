using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plataforma.Dominio;

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

        services.AddDbContext<TContexto>(o =>
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
        });

        return services;
    }
}
