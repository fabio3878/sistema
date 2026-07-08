using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cadastros.Infraestrutura;

/// <summary>
/// Fábrica usada apenas em tempo de DESIGN (pelo <c>dotnet ef migrations</c>). Fixada em
/// Postgres — provider principal do servidor da loja — porque migrations no EF são
/// específicas por provider. Não afeta a execução: em runtime o DbContext vem da DI do host.
/// </summary>
public sealed class CadastrosDbContextFactory : IDesignTimeDbContextFactory<CadastrosDbContext>
{
    public CadastrosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CadastrosDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=automacao;Username=postgres;Password=postgres")
            .Options;
        return new CadastrosDbContext(options);
    }
}
