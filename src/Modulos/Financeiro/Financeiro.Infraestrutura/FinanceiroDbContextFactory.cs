using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Financeiro.Infraestrutura;

/// <summary>
/// Fábrica usada apenas em tempo de DESIGN (pelo <c>dotnet ef migrations</c>). Fixada em Postgres —
/// provider principal do servidor da loja. Não afeta a execução: em runtime o DbContext vem da DI.
/// </summary>
public sealed class FinanceiroDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FinanceiroDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=automacao;Username=postgres;Password=postgres")
            .Options;
        return new FinanceiroDbContext(options);
    }
}
