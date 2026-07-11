using BuildingBlocks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura.Auditoria;

namespace Plataforma.Tests;

/// <summary>
/// Verifica a consulta paginada da trilha (<see cref="LeitorAuditoria"/>) contra SQLite in-memory:
/// filtros por entidade/registro/operação/período, isolamento por tenant, ordenação e paginação.
/// (O filtro por usuário usa <c>ILike</c> do Postgres e é coberto na verificação ponta a ponta.)
/// </summary>
public sealed class LeitorAuditoriaTests
{
    private sealed class ContextoTrilha(DbContextOptions<ContextoTrilha> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<RegistroAuditoria>().ConfigurarAuditoria("fake_auditoria");
            base.OnModelCreating(b);
        }
    }

    private static readonly DateTimeOffset Base_ = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    private static RegistroAuditoria Reg(
        string empresa, string entidade, string registroId, OperacaoAuditoria op, int minutos) => new()
    {
        // Id determinístico e ordenável por 'minutos' — espelha o ULID (que ordena por tempo) sem
        // depender da monotonicidade do gerador dentro do mesmo milissegundo.
        Id = minutos.ToString("D26"),
        EmpresaId = empresa,
        Entidade = entidade,
        RegistroId = registroId,
        Operacao = op,
        OcorridoEm = Base_.AddMinutes(minutos),
        UsuarioId = "USR_1",
        UsuarioLogin = "admin",
        Alteracoes = "{}",
    };

    private static async Task<ContextoTrilha> Semear(params RegistroAuditoria[] regs)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var ctx = new ContextoTrilha(new DbContextOptionsBuilder<ContextoTrilha>().UseSqlite(conn).Options);
        await ctx.Database.EnsureCreatedAsync();
        ctx.Set<RegistroAuditoria>().AddRange(regs);
        await ctx.SaveChangesAsync();
        return ctx;
    }

    [Fact]
    public async Task Filtra_por_tenant_entidade_e_registro()
    {
        using var ctx = await Semear(
            Reg("EMP_1", "Produto", "P1", OperacaoAuditoria.Criacao, 1),
            Reg("EMP_1", "Produto", "P1", OperacaoAuditoria.Alteracao, 2),
            Reg("EMP_1", "Cliente", "C1", OperacaoAuditoria.Criacao, 3),
            Reg("EMP_2", "Produto", "P1", OperacaoAuditoria.Criacao, 4));

        var porTenantEEntidade = await LeitorAuditoria.Consultar(ctx, "EMP_1", new FiltroAuditoria(Entidade: "Produto"));
        Assert.Equal(2, porTenantEEntidade.Total);
        Assert.All(porTenantEEntidade.Itens, r => Assert.Equal("Produto", r.Entidade));

        var porRegistro = await LeitorAuditoria.Consultar(ctx, "EMP_1", new FiltroAuditoria(RegistroId: "C1"));
        Assert.Equal("Cliente", Assert.Single(porRegistro.Itens).Entidade);
    }

    [Fact]
    public async Task Filtra_por_operacao()
    {
        using var ctx = await Semear(
            Reg("EMP_1", "Produto", "P1", OperacaoAuditoria.Criacao, 0),
            Reg("EMP_1", "Produto", "P1", OperacaoAuditoria.Alteracao, 10),
            Reg("EMP_1", "Produto", "P1", OperacaoAuditoria.Exclusao, 20));

        var soAlteracao = await LeitorAuditoria.Consultar(ctx, "EMP_1", new FiltroAuditoria(Operacao: OperacaoAuditoria.Alteracao));
        Assert.Equal(OperacaoAuditoria.Alteracao, Assert.Single(soAlteracao.Itens).Operacao);

        // O filtro por período (De/Ate em DateTimeOffset) só é traduzível no Postgres — coberto na
        // verificação ponta a ponta, não aqui (SQLite não compara DateTimeOffset em WHERE).
    }

    [Fact]
    public async Task Ordena_desc_por_data_e_pagina()
    {
        var regs = Enumerable.Range(0, 25)
            .Select(i => Reg("EMP_1", "Produto", $"P{i:00}", OperacaoAuditoria.Criacao, i))
            .ToArray();
        using var ctx = await Semear(regs);

        var p1 = await LeitorAuditoria.Consultar(ctx, "EMP_1", new FiltroAuditoria(Pagina: 1, Tamanho: 10));
        Assert.Equal(25, p1.Total);
        Assert.Equal(10, p1.Itens.Count);
        // Mais recente primeiro (minuto 24).
        Assert.Equal(Base_.AddMinutes(24), p1.Itens[0].OcorridoEm);
        Assert.True(p1.Itens[0].OcorridoEm > p1.Itens[9].OcorridoEm);

        var p3 = await LeitorAuditoria.Consultar(ctx, "EMP_1", new FiltroAuditoria(Pagina: 3, Tamanho: 10));
        Assert.Equal(5, p3.Itens.Count); // 25 = 10 + 10 + 5
    }
}
