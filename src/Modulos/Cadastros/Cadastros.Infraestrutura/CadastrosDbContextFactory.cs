using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cadastros.Infraestrutura;

/// <summary>
/// Fábrica usada apenas em tempo de DESIGN (pelo <c>dotnet ef migrations</c>).
/// Não afeta a execução — em runtime o DbContext vem da DI configurada no host.
/// </summary>
public sealed class CadastrosDbContextFactory : IDesignTimeDbContextFactory<CadastrosDbContext>
{
    public CadastrosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CadastrosDbContext>()
            .UseSqlite("Data Source=automacao.db")
            .Options;
        return new CadastrosDbContext(options);
    }
}
