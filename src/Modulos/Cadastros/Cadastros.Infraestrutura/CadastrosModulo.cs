using BuildingBlocks;
using Cadastros.Aplicacao;
using Cadastros.Contratos;
using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cadastros.Infraestrutura;

/// <summary>
/// Auto-registro do módulo Cadastros (seção 4.3). O host varre os <see cref="IModulo"/>
/// e ativa os habilitados pela licença.
/// </summary>
public sealed class CadastrosModulo : IModulo
{
    public string Nome => "Cadastros";

    public void RegistrarServicos(IServiceCollection services, IConfiguration config)
    {
        // Provider local = SQLite (seção 11). Um host central (Api.Central) trocaria por UseNpgsql.
        var conexao = config.GetConnectionString("Local") ?? "Data Source=automacao.db";
        services.AddDbContext<CadastrosDbContext>(o => o.UseSqlite(conexao));

        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<CadastrosDbContext>());
        services.AddScoped<IPessoaRepositorio, PessoaRepositorio>();
        services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();

        services.AddScoped<ICadastrosConsulta, CadastrosConsulta>();
        services.AddScoped<CadastrosAppService>();
    }

    public void RegistrarMigrations(MigrationRegistry registry) => registry.Adicionar<CadastrosDbContext>();
}
