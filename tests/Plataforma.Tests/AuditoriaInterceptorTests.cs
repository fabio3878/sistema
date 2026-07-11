using System.Text.Json;
using BuildingBlocks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Plataforma.Dominio;
using Plataforma.Infraestrutura;
using Plataforma.Infraestrutura.Auditoria;

namespace Plataforma.Tests;

/// <summary>
/// Verifica o motor de auditoria (<see cref="AuditoriaInterceptor"/>) contra um DbContext real
/// em SQLite in-memory: diff de/para, operações, omissão de campos [NaoAuditar] e contexto anônimo.
/// </summary>
public sealed class AuditoriaInterceptorTests
{
    // ---- Fakes de contexto (usuário/tenant) ----
    private sealed class UsuarioFake(string? id, string? login) : IContextoUsuario
    {
        public bool Autenticado => id is not null;
        public string? UsuarioId => id;
        public string? Login => login;
        public bool ConcedeTodas => false;
        public IReadOnlySet<string> Funcionalidades => new HashSet<string>();
        public bool Pode(string funcionalidade) => false;
    }

    private sealed class EmpresaFake(string id) : IContextoEmpresa
    {
        public string EmpresaId => id;
    }

    // ---- Entidade e contexto de teste ----
    private sealed class EntidadeFake : EntidadeBase
    {
        public string Nome { get; set; } = default!;
        public DateTimeOffset? DataNascimento { get; set; }

        [NaoAuditar]
        public string? Segredo { get; set; }
    }

    private sealed class ContextoFake(DbContextOptions<ContextoFake> options) : DbContext(options)
    {
        public DbSet<EntidadeFake> Fakes => Set<EntidadeFake>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<EntidadeFake>(e =>
            {
                e.ToTable("fake");
                e.ConfigurarEntidadeBase();
                e.Property(x => x.Nome).IsRequired();
            });
            b.Entity<RegistroAuditoria>().ConfigurarAuditoria("fake_auditoria");
            base.OnModelCreating(b);
        }
    }

    /// <summary>Contexto que NÃO mapeia a trilha — para provar a blindagem do interceptor.</summary>
    private sealed class ContextoSemTrilha(DbContextOptions<ContextoSemTrilha> options) : DbContext(options)
    {
        public DbSet<EntidadeFake> Fakes => Set<EntidadeFake>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<EntidadeFake>(e =>
            {
                e.ToTable("fake");
                e.ConfigurarEntidadeBase();
                e.Property(x => x.Nome).IsRequired();
            });
            // Repare: sem ConfigurarAuditoria — o módulo "esqueceu" de mapear a trilha.
            base.OnModelCreating(b);
        }
    }

    private static (ContextoFake ctx, SqliteConnection conn) Criar(IContextoUsuario? usuario = null)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var interceptor = new AuditoriaInterceptor(
            usuario ?? new UsuarioFake("USR_ADMIN", "admin"),
            new EmpresaFake("EMP_1"));
        var options = new DbContextOptionsBuilder<ContextoFake>()
            .UseSqlite(conn)
            .AddInterceptors(interceptor)
            .Options;
        var ctx = new ContextoFake(options);
        ctx.Database.EnsureCreated();
        return (ctx, conn);
    }

    private static List<RegistroAuditoria> Trilha(ContextoFake ctx) =>
        ctx.Set<RegistroAuditoria>().AsNoTracking().OrderBy(x => x.Id).ToList();

    private static EntidadeFake NovaEntidade() => new()
    {
        EmpresaId = "EMP_1",
        Nome = "Ana",
        DataNascimento = new DateTimeOffset(1990, 5, 20, 0, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public async Task Criacao_gera_um_registro_com_snapshot_e_usuario()
    {
        var (ctx, conn) = Criar();
        using var conexao = conn;

        var e = NovaEntidade();
        ctx.Fakes.Add(e);
        await ctx.SaveChangesAsync();

        var trilha = Trilha(ctx);
        var reg = Assert.Single(trilha);
        Assert.Equal(OperacaoAuditoria.Criacao, reg.Operacao);
        Assert.Equal(e.Id, reg.RegistroId);
        Assert.Equal("EntidadeFake", reg.Entidade);
        Assert.Equal("EMP_1", reg.EmpresaId);
        Assert.Equal("USR_ADMIN", reg.UsuarioId);
        Assert.Equal("admin", reg.UsuarioLogin);

        var doc = JsonDocument.Parse(reg.Alteracoes).RootElement;
        Assert.Equal("Ana", doc.GetProperty("Nome").GetProperty("para").GetString());
        Assert.Equal(JsonValueKind.Null, doc.GetProperty("Nome").GetProperty("de").ValueKind);
    }

    [Fact]
    public async Task Alteracao_registra_apenas_o_campo_mudado_com_de_e_para()
    {
        var (ctx, conn) = Criar();
        using var conexao = conn;

        var e = NovaEntidade();
        ctx.Fakes.Add(e);
        await ctx.SaveChangesAsync();

        e.Nome = "Beatriz";
        e.MarcarAtualizado();
        await ctx.SaveChangesAsync();

        var alteracao = Trilha(ctx).Single(r => r.Operacao == OperacaoAuditoria.Alteracao);
        var doc = JsonDocument.Parse(alteracao.Alteracoes).RootElement;

        Assert.Equal("Ana", doc.GetProperty("Nome").GetProperty("de").GetString());
        Assert.Equal("Beatriz", doc.GetProperty("Nome").GetProperty("para").GetString());
        // Campo não tocado não entra no diff.
        Assert.False(doc.TryGetProperty("DataNascimento", out _));
        // Ruído de sync fica de fora.
        Assert.False(doc.TryGetProperty("Versao", out _));
        Assert.False(doc.TryGetProperty("AtualizadoEm", out _));
    }

    [Fact]
    public async Task Soft_delete_registra_exclusao()
    {
        var (ctx, conn) = Criar();
        using var conexao = conn;

        var e = NovaEntidade();
        ctx.Fakes.Add(e);
        await ctx.SaveChangesAsync();

        e.Excluido = true;
        e.MarcarAtualizado();
        await ctx.SaveChangesAsync();

        var exclusao = Assert.Single(Trilha(ctx), r => r.Operacao == OperacaoAuditoria.Exclusao);
        var doc = JsonDocument.Parse(exclusao.Alteracoes).RootElement;
        Assert.True(doc.GetProperty("Excluido").GetProperty("para").GetBoolean());
    }

    [Fact]
    public async Task Campo_marcado_NaoAuditar_nunca_aparece_e_nao_gera_trilha_sozinho()
    {
        var (ctx, conn) = Criar();
        using var conexao = conn;

        var e = NovaEntidade();
        e.Segredo = "hash-secreto";
        ctx.Fakes.Add(e);
        await ctx.SaveChangesAsync();

        // Na criação o campo secreto não aparece.
        var criacao = Trilha(ctx).Single();
        Assert.DoesNotContain("Segredo", criacao.Alteracoes);
        Assert.DoesNotContain("hash-secreto", criacao.Alteracoes);

        // Alterar SÓ o campo secreto não gera nova linha de trilha.
        e.Segredo = "outro-hash";
        e.MarcarAtualizado();
        await ctx.SaveChangesAsync();

        Assert.Single(Trilha(ctx)); // continua só a da criação
    }

    [Fact]
    public async Task Sem_usuario_grava_com_usuarioid_nulo()
    {
        var (ctx, conn) = Criar(new UsuarioFake(null, null));
        using var conexao = conn;

        ctx.Fakes.Add(NovaEntidade());
        await ctx.SaveChangesAsync();

        var reg = Trilha(ctx).Single();
        Assert.Null(reg.UsuarioId);
        Assert.Null(reg.UsuarioLogin);
        Assert.Equal(OperacaoAuditoria.Criacao, reg.Operacao);
    }

    [Fact]
    public async Task Contexto_sem_trilha_mapeada_nao_lanca_e_salva_normalmente()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var interceptor = new AuditoriaInterceptor(new UsuarioFake("USR_ADMIN", "admin"), new EmpresaFake("EMP_1"));
        var options = new DbContextOptionsBuilder<ContextoSemTrilha>()
            .UseSqlite(conn).AddInterceptors(interceptor).Options;
        using var ctx = new ContextoSemTrilha(options);
        ctx.Database.EnsureCreated();

        ctx.Fakes.Add(NovaEntidade());
        // Blindagem: sem RegistroAuditoria no modelo, o interceptor pula em vez de lançar.
        var salvos = await ctx.SaveChangesAsync();

        Assert.Equal(1, salvos);
        Assert.Equal(1, await ctx.Fakes.CountAsync());
    }
}
