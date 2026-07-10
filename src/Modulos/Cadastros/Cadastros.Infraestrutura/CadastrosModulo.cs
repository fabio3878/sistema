using BuildingBlocks;
using Cadastros.Aplicacao;
using Cadastros.Contratos;
using Cadastros.Dominio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plataforma.Infraestrutura;

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
        // Provider (SQLite/Postgres) e connection string vêm da configuração (seção 11).
        services.AdicionarDbContextConfiguravel<CadastrosDbContext>(config);

        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<CadastrosDbContext>());
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
        services.AddScoped<IServicoRepositorio, ServicoRepositorio>();
        services.AddScoped<ILocalidadeRepositorio, LocalidadeRepositorio>();
        services.AddScoped<IUnidadeRepositorio, UnidadeRepositorio>();

        services.AddScoped<ICadastrosConsulta, CadastrosConsulta>();
        services.AddScoped<CadastrosAppService>();
        services.AddScoped<SeederLocalidades>();
        services.AddScoped<SeederUnidades>();
    }

    public void RegistrarMigrations(MigrationRegistry registry) => registry.Adicionar<CadastrosDbContext>();

    // Cadastros declara também as funcionalidades de Produto sob o módulo "est" (Estoque): o
    // produto mora aqui (master data), mas seu gating é do Estoque, que o consome (ver DESIGN_1 §5).
    public IEnumerable<FuncionalidadeManifesto> Funcionalidades() =>
        [.. FuncionalidadesCadastro.Manifesto(), .. FuncionalidadesEstoque.Manifesto()];
}
