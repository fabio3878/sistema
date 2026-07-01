using BuildingBlocks;
using Cadastros.Infraestrutura;
using JasperFx;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura;
using Wolverine;
using Wolverine.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Connection string local (SQLite). Mesmo arquivo para dados e para o outbox do Wolverine.
var conexaoLocal = builder.Configuration.GetConnectionString("Local") ?? "Data Source=automacao.db";

// Plataforma (shared kernel): licença + contexto de empresa.
builder.Services.AdicionarPlataforma();

// Descoberta de módulos: o host só ativa os habilitados pela licença (seção 8).
// Novos módulos entram nesta lista — cada um se auto-registra via IModulo.
var licenca = new LicencaLocal();
IModulo[] modulos = [new CadastrosModulo()];
var migrationRegistry = new MigrationRegistry();

foreach (var modulo in modulos)
{
    if (!licenca.ModuloAtivo(modulo.Nome))
        continue;

    modulo.RegistrarServicos(builder.Services, builder.Configuration);
    modulo.RegistrarMigrations(migrationRegistry);
}

builder.Services.AddSingleton(migrationRegistry);

// Wolverine = bus in-process + outbox durável sobre SQLite (seção 5).
builder.Host.UseWolverine(opts =>
{
    opts.PersistMessagesWithSqlite(conexaoLocal);
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
});

// Cria/atualiza o schema de mensageria do Wolverine no startup.
builder.Services.AddResourceSetupOnStartup();

var app = builder.Build();

// Aplica as migrations de cada módulo → cria o SQLite local com as tabelas (cad_*).
await AplicarMigrationsDosModulosAsync(app);

// Endpoint de saúde: prova que o host sobe.
app.MapGet("/health", () => Results.Ok(new { status = "ok", servico = "AgenteLocal" }));

return await app.RunJasperFxCommands(args);

static async Task AplicarMigrationsDosModulosAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var registry = scope.ServiceProvider.GetRequiredService<MigrationRegistry>();

    foreach (var tipoContexto in registry.Contextos)
    {
        var contexto = (DbContext)scope.ServiceProvider.GetRequiredService(tipoContexto);
        await contexto.Database.MigrateAsync();
    }
}
