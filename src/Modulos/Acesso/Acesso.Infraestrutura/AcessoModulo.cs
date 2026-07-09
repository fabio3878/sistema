using Acesso.Aplicacao;
using Acesso.Contratos;
using Acesso.Dominio;
using BuildingBlocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plataforma.Infraestrutura;

namespace Acesso.Infraestrutura;

/// <summary>
/// Auto-registro do módulo Acesso. Diferente dos demais, é SEMPRE ativo no host (autenticação não é
/// licenciável). Declara suas funcionalidades em <see cref="Funcionalidades"/> para o catálogo.
/// </summary>
public sealed class AcessoModulo : IModulo
{
    public string Nome => "Acesso";

    public void RegistrarServicos(IServiceCollection services, IConfiguration config)
    {
        services.AdicionarDbContextConfiguravel<AcessoDbContext>(config);

        services.AddScoped<IUnidadeDeTrabalho>(sp => sp.GetRequiredService<AcessoDbContext>());
        services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
        services.AddScoped<IPerfilRepositorio, PerfilRepositorio>();
        services.AddScoped<IRefreshTokenRepositorio, RefreshTokenRepositorio>();
        services.AddSingleton<IHashSenha, Pbkdf2HashSenha>();

        services.AddScoped<IAcessoConsulta, AcessoConsulta>();
        services.AddScoped<AcessoAppService>();
        services.AddScoped<AutenticacaoAppService>();

        // Autenticação JWT (valida token, lê Acesso:Jwt; chave via secret/env).
        services.AddSingleton<IServicoToken, ServicoTokenJwt>();
        services.AdicionarAutenticacaoAcesso(config);

        services.AddScoped<SeederAcesso>();
    }

    public void RegistrarMigrations(MigrationRegistry registry) => registry.Adicionar<AcessoDbContext>();

    public IEnumerable<FuncionalidadeManifesto> Funcionalidades() => FuncionalidadesAcesso.Manifesto();
}
