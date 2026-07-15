using Financeiro.Dominio;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura;
using Plataforma.Infraestrutura.Auditoria;

namespace Financeiro.Infraestrutura;

/// <summary>
/// DbContext do módulo Financeiro (prefixo fin_). Provider trocável (Postgres servidor / SQLite
/// local futuro). Datas de negócio são <c>DateOnly</c> (portáveis Postgres/SQLite); dinheiro é
/// <c>decimal</c> com precisão fixa; JSON da auditoria fica como text (§11).
/// </summary>
public sealed class FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options)
    : DbContext(options), IUnidadeDeTrabalho
{
    public DbSet<ContaReceber> Contas => Set<ContaReceber>();
    public DbSet<Parcela> Parcelas => Set<Parcela>();
    public DbSet<Recebimento> Recebimentos => Set<Recebimento>();
    public DbSet<Renegociacao> Renegociacoes => Set<Renegociacao>();
    public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();
    public DbSet<ParametrosFinanceiros> Parametros => Set<ParametrosFinanceiros>();

    public Task<int> Salvar(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContaReceber>(e =>
        {
            e.ToTable("fin_contas_receber");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.ClienteId).HasMaxLength(26).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(300);
            e.Property(p => p.TipoOrigem).HasConversion<int>();
            e.Property(p => p.DocumentoOrigem).HasMaxLength(60);
            e.Property(p => p.NumeroDocumento).HasMaxLength(60);
            e.Property(p => p.ValorTotal).HasPrecision(18, 4);
            e.Property(p => p.CategoriaFinanceira).HasMaxLength(100);
            e.Property(p => p.Observacoes).HasMaxLength(1000);
            e.Property(p => p.UsuarioResponsavelId).HasMaxLength(26);
            e.HasIndex(p => new { p.EmpresaId, p.ClienteId });
            e.HasIndex(p => new { p.EmpresaId, p.DataEmissao });

            e.HasMany(p => p.Parcelas)
                .WithOne()
                .HasForeignKey(pa => pa.ContaReceberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Parcelas).UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasMany(p => p.Renegociacoes)
                .WithOne()
                .HasForeignKey(r => r.ContaReceberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Renegociacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Parcela>(e =>
        {
            e.ToTable("fin_parcelas");
            e.ConfigurarEntidadeBase();
            e.Ignore(p => p.SaldoPrincipal); // derivado — nunca persistido
            e.Property(p => p.ContaReceberId).HasMaxLength(26).IsRequired();
            e.Property(p => p.ValorOriginal).HasPrecision(18, 4);
            e.Property(p => p.TotalPago).HasPrecision(18, 4);
            e.Property(p => p.PercentualJurosOverride).HasPrecision(9, 4);
            e.Property(p => p.Observacoes).HasMaxLength(500);
            e.Property(p => p.RenegociacaoId).HasMaxLength(26);
            e.HasIndex(p => new { p.EmpresaId, p.ContaReceberId });
            e.HasIndex(p => new { p.EmpresaId, p.Vencimento });
            e.HasIndex(p => new { p.EmpresaId, p.RenegociacaoId });

            e.HasMany(p => p.Recebimentos)
                .WithOne()
                .HasForeignKey(r => r.ParcelaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Recebimentos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Recebimento>(e =>
        {
            e.ToTable("fin_recebimentos");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.ParcelaId).HasMaxLength(26).IsRequired();
            e.Property(p => p.ValorRecebido).HasPrecision(18, 4);
            e.Property(p => p.Desconto).HasPrecision(18, 4);
            e.Property(p => p.Juros).HasPrecision(18, 4);
            e.Property(p => p.Multa).HasPrecision(18, 4);
            e.Property(p => p.Acrescimos).HasPrecision(18, 4);
            e.Property(p => p.FormaPagamentoId).HasMaxLength(26).IsRequired();
            e.Property(p => p.Observacoes).HasMaxLength(500);
            e.Property(p => p.UsuarioId).HasMaxLength(26);
            e.Property(p => p.EstornoMotivo).HasMaxLength(300);
            e.HasIndex(p => new { p.EmpresaId, p.ParcelaId });
        });

        modelBuilder.Entity<Renegociacao>(e =>
        {
            e.ToTable("fin_renegociacoes");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.ContaReceberId).HasMaxLength(26).IsRequired();
            e.Property(p => p.ValorBase).HasPrecision(18, 4);
            e.Property(p => p.Desconto).HasPrecision(18, 4);
            e.Property(p => p.Entrada).HasPrecision(18, 4);
            e.Property(p => p.ValorRenegociado).HasPrecision(18, 4);
            e.Property(p => p.UsuarioId).HasMaxLength(26);
            e.Property(p => p.Observacoes).HasMaxLength(1000);
            e.HasIndex(p => new { p.EmpresaId, p.ContaReceberId });
        });

        modelBuilder.Entity<FormaPagamento>(e =>
        {
            e.ToTable("fin_formas_pagamento");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.HasIndex(p => new { p.EmpresaId, p.Nome }).IsUnique();
        });

        modelBuilder.Entity<ParametrosFinanceiros>(e =>
        {
            e.ToTable("fin_parametros");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.JurosMoraMensalPercent).HasPrecision(9, 4);
            e.Property(p => p.MultaPercent).HasPrecision(9, 4);
            e.HasIndex(p => p.EmpresaId).IsUnique(); // 1 linha por empresa
        });

        // Trilha de auditoria do módulo (append-only) — ver AuditoriaInterceptor.
        modelBuilder.Entity<RegistroAuditoria>().ConfigurarAuditoria("fin_auditoria");

        base.OnModelCreating(modelBuilder);
    }
}
