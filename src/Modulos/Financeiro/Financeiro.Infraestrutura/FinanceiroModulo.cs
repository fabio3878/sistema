using BuildingBlocks;
using Financeiro.Aplicacao;
using Financeiro.Contratos;
using Financeiro.Dominio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plataforma.Infraestrutura;

namespace Financeiro.Infraestrutura;

/// <summary>
/// Auto-registro do módulo Financeiro. O host varre os <see cref="IModulo"/> e ativa os
/// habilitados pela licença (<c>ModuloAtivo("Financeiro")</c>).
/// </summary>
public sealed class FinanceiroModulo : IModulo
{
    public string Nome => "Financeiro";

    public void RegistrarServicos(IServiceCollection services, IConfiguration config)
    {
        services.AdicionarDbContextConfiguravel<FinanceiroDbContext>(config);

        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<FinanceiroDbContext>());
        services.AddScoped<IContaReceberRepositorio, ContaReceberRepositorio>();
        services.AddScoped<IFormaPagamentoRepositorio, FormaPagamentoRepositorio>();
        services.AddScoped<IParametrosRepositorio, ParametrosRepositorio>();
        services.AddScoped<IAuditoriaRepositorio, AuditoriaRepositorio>();

        services.AddScoped<IFinanceiroConsulta, FinanceiroConsulta>();
        services.AddScoped<FinanceiroAppService>();
    }

    public void RegistrarMigrations(MigrationRegistry registry) => registry.Adicionar<FinanceiroDbContext>();

    public IEnumerable<FuncionalidadeManifesto> Funcionalidades() => FuncionalidadesFinanceiro.Manifesto();
}
