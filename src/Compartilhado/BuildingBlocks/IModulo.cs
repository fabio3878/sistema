using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks;

/// <summary>
/// Contrato de módulo plugável (seção 4.3). Cada módulo expõe um <see cref="IModulo"/>
/// para auto-registro no host. O host varre os módulos e ativa só os habilitados pela licença.
/// </summary>
public interface IModulo
{
    /// <summary>Nome do módulo (ex.: "Cadastros", "Estoque").</summary>
    string Nome { get; }

    /// <summary>Registra serviços do módulo no container de DI.</summary>
    void RegistrarServicos(IServiceCollection services, IConfiguration config);

    /// <summary>Registra os DbContexts do módulo para o host aplicar migrations no startup.</summary>
    void RegistrarMigrations(MigrationRegistry registry);

    /// <summary>
    /// Funcionalidades (permissões) que o módulo expõe, declaradas em código. O host agrega o
    /// manifesto de todos os módulos ativos e o módulo Acesso reconcilia para o catálogo no
    /// startup. Default vazio: módulos sem controle de acesso próprio não precisam sobrescrever.
    /// </summary>
    IEnumerable<FuncionalidadeManifesto> Funcionalidades() => [];
}
