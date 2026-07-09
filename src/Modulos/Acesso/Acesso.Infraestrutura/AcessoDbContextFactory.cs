using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Acesso.Infraestrutura;

/// <summary>
/// Fábrica usada apenas em tempo de DESIGN (pelo <c>dotnet ef migrations</c>). Fixada em Postgres —
/// provider principal do servidor da loja — porque migrations no EF são específicas por provider.
/// Não afeta a execução: em runtime o DbContext vem da DI do host.
/// </summary>
public sealed class AcessoDbContextFactory : IDesignTimeDbContextFactory<AcessoDbContext>
{
    public AcessoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AcessoDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=automacao;Username=postgres;Password=postgres")
            .Options;
        return new AcessoDbContext(options);
    }
}
