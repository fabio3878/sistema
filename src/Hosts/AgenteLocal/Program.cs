using BuildingBlocks;
using Cadastros.Infraestrutura;
using JasperFx;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Plataforma.Dominio;
using Plataforma.Infraestrutura;
using Wolverine;
using Wolverine.Postgresql;
using Wolverine.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Opções de banco (seção "Banco"): provider + connection string. Principal = Postgres no
// servidor da loja; SQLite fica para contingência local futura por PDV. A mesma connection
// string serve tanto os dados dos módulos quanto o outbox do Wolverine.
var opcoesBanco = builder.Configuration.GetSection(OpcoesBanco.Secao).Get<OpcoesBanco>()
    ?? new OpcoesBanco();

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

// Wolverine = bus in-process + outbox durável (seção 5). O provider do outbox segue o
// mesmo OpcoesBanco.Provider dos módulos: Postgres no servidor da loja, SQLite na
// contingência local futura.
builder.Host.UseWolverine(opts =>
{
    if (opcoesBanco.Provider == ProviderBanco.Postgres)
        opts.PersistMessagesWithPostgresql(opcoesBanco.ConnectionString);
    else
        opts.PersistMessagesWithSqlite(opcoesBanco.ConnectionString);

    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableLocalQueues();
});

// Cria/atualiza o schema de mensageria do Wolverine no startup.
builder.Services.AddResourceSetupOnStartup();

var app = builder.Build();

// Aplica as migrations de cada módulo → cria as tabelas (cad_*) no banco do servidor.
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
