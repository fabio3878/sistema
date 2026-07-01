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
}
