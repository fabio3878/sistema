using BuildingBlocks;
using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cadastros.Infraestrutura;

/// <summary>
/// DbContext do módulo Cadastros. Cada módulo é dono das suas tabelas (prefixo cad_).
/// Provider trocável (SQLite local / Postgres central) — configurado no host (seção 11).
/// </summary>
public sealed class CadastrosDbContext(DbContextOptions<CadastrosDbContext> options)
    : DbContext(options), IUnidadeDeTrabalho
{
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<Produto> Produtos => Set<Produto>();

    public Task<int> Salvar(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pessoa>(e =>
        {
            e.ToTable("cad_pessoas");
            ConfigurarBase(e);
            e.Property(p => p.Nome).HasMaxLength(200).IsRequired();
            e.Property(p => p.Documento).HasMaxLength(14).IsRequired();
            e.Property(p => p.Papeis).HasConversion<int>();
            e.HasIndex(p => new { p.EmpresaId, p.Documento });
        });

        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("cad_produtos");
            ConfigurarBase(e);
            e.Property(p => p.Sku).HasMaxLength(60).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(300).IsRequired();
            e.Property(p => p.CodigoBarras).HasMaxLength(60);
            e.Property(p => p.Ncm).HasMaxLength(8).IsRequired();
            // Precisão fixa para dinheiro, provider-neutra: cada provider escolhe seu tipo nativo
            // (numeric no Postgres; no SQLite, de tipagem frouxa, força o mapeamento explícito).
            e.Property(p => p.PrecoVenda).HasPrecision(18, 4);
            e.HasIndex(p => new { p.EmpresaId, p.Sku }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Configura o que TODA entidade sincronizável tem (PK ULID, tenant, soft delete).</summary>
    private static void ConfigurarBase<TEntidade>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntidade> e)
        where TEntidade : EntidadeBase
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(26).IsRequired();
        e.Property(x => x.EmpresaId).HasMaxLength(26).IsRequired();
        e.Property(x => x.OrigemId).HasMaxLength(26);
        e.HasIndex(x => x.EmpresaId);
        // Soft delete: some da query por padrão quem está excluído (seção 4.1).
        e.HasQueryFilter(x => !x.Excluido);
    }
}
