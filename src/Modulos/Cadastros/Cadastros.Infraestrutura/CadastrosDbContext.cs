using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura;

namespace Cadastros.Infraestrutura;

/// <summary>
/// DbContext do módulo Cadastros. Cada módulo é dono das suas tabelas (prefixo cad_).
/// Provider trocável (Postgres servidor / SQLite local futuro) — configurado no host (seção 11).
/// </summary>
public sealed class CadastrosDbContext(DbContextOptions<CadastrosDbContext> options)
    : DbContext(options), IUnidadeDeTrabalho
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ClienteEndereco> ClienteEnderecos => Set<ClienteEndereco>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Servico> Servicos => Set<Servico>();

    // Tier B — referência global (IBGE + unidades), sem EmpresaId/soft delete/ULID.
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Unidade> Unidades => Set<Unidade>();

    public Task<int> Salvar(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("cad_clientes");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.Nome).HasMaxLength(200).IsRequired();
            e.Property(p => p.Documento).HasMaxLength(14).IsRequired();
            e.Property(p => p.TipoPessoa).HasConversion<int>();
            e.Property(p => p.NomeFantasia).HasMaxLength(200);
            e.Property(p => p.Email).HasMaxLength(200);
            e.Property(p => p.EmailFinanceiro).HasMaxLength(200);
            e.Property(p => p.Telefone).HasMaxLength(20);
            e.Property(p => p.Celular).HasMaxLength(20);
            e.Property(p => p.Whatsapp).HasMaxLength(20);
            e.Property(p => p.Site).HasMaxLength(200);
            e.Property(p => p.Rg).HasMaxLength(20);
            e.Property(p => p.OrgaoEmissorRg).HasMaxLength(20);
            e.Property(p => p.InscricaoEstadual).HasMaxLength(20);
            e.Property(p => p.InscricaoMunicipal).HasMaxLength(20);
            e.Property(p => p.IndicadorIe).HasConversion<int>();
            e.Property(p => p.RegimeTributario).HasConversion<int>();
            e.Property(p => p.LimiteCredito).HasPrecision(18, 4);
            e.Property(p => p.Origem).HasMaxLength(100);
            e.Property(p => p.Preferencias).HasMaxLength(500);
            e.Property(p => p.Observacoes).HasMaxLength(1000);
            e.HasIndex(p => new { p.EmpresaId, p.Documento }).IsUnique();

            // Endereços: 1:N, agregados sob o Cliente. Cascata segue o ciclo de vida do cliente.
            e.HasMany(p => p.Enderecos)
                .WithOne()
                .HasForeignKey(en => en.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Enderecos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ClienteEndereco>(e =>
        {
            e.ToTable("cad_cliente_enderecos");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.ClienteId).HasMaxLength(26).IsRequired();
            e.Property(p => p.Tipo).HasConversion<int>();
            e.Property(p => p.Cep).HasMaxLength(8).IsRequired();
            e.Property(p => p.Logradouro).HasMaxLength(200).IsRequired();
            e.Property(p => p.Numero).HasMaxLength(20).IsRequired();
            e.Property(p => p.Complemento).HasMaxLength(100);
            e.Property(p => p.Bairro).HasMaxLength(100).IsRequired();
            e.Property(p => p.Municipio).HasMaxLength(100).IsRequired();
            e.Property(p => p.Uf).HasMaxLength(2).IsFixedLength().IsRequired();
            e.Property(p => p.CodigoIbgeMunicipio).HasMaxLength(7).IsRequired();
            e.Property(p => p.Pais).HasMaxLength(60).IsRequired();
            e.HasIndex(p => new { p.EmpresaId, p.ClienteId });
        });

        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("cad_produtos");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.CodigoInterno).HasMaxLength(60);
            e.Property(p => p.Descricao).HasMaxLength(300).IsRequired();
            e.Property(p => p.CodigoBarras).HasMaxLength(60);
            e.Property(p => p.Unidade).HasMaxLength(6).IsRequired();
            e.Property(p => p.Ncm).HasMaxLength(8).IsRequired();
            e.Property(p => p.Cest).HasMaxLength(7);
            e.Property(p => p.Origem).HasConversion<int>();
            // Precisão fixa para dinheiro, provider-neutra: cada provider escolhe seu tipo nativo
            // (numeric no Postgres; no SQLite, de tipagem frouxa, força o mapeamento explícito).
            e.Property(p => p.PrecoVenda).HasPrecision(18, 4);
            // Único por empresa quando informado; no Postgres NULLs são distintos (vários sem código convivem).
            e.HasIndex(p => new { p.EmpresaId, p.CodigoInterno }).IsUnique();
        });

        modelBuilder.Entity<Servico>(e =>
        {
            e.ToTable("cad_servicos");
            e.ConfigurarEntidadeBase();
            e.Property(s => s.CodigoInterno).HasMaxLength(60);
            e.Property(s => s.Descricao).HasMaxLength(300).IsRequired();
            e.Property(s => s.Unidade).HasMaxLength(6).IsRequired();
            e.Property(s => s.PrecoVenda).HasPrecision(18, 4);
            e.HasIndex(s => new { s.EmpresaId, s.CodigoInterno }).IsUnique();
        });

        // Tier B — referência global IBGE (chave natural, sem ConfigurarEntidadeBase — como acs_modulos).
        modelBuilder.Entity<Estado>(e =>
        {
            e.ToTable("cad_estados");
            e.HasKey(p => p.Uf);
            e.Property(p => p.Uf).HasMaxLength(2).IsFixedLength();
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.Property(p => p.CodigoIbge).HasMaxLength(2).IsRequired();
        });

        modelBuilder.Entity<Municipio>(e =>
        {
            e.ToTable("cad_municipios");
            e.HasKey(p => p.CodigoIbge);
            e.Property(p => p.CodigoIbge).HasMaxLength(7);
            e.Property(p => p.Nome).HasMaxLength(120).IsRequired();
            e.Property(p => p.Uf).HasMaxLength(2).IsRequired();
            e.HasIndex(p => p.Uf);
        });

        modelBuilder.Entity<Unidade>(e =>
        {
            e.ToTable("cad_unidades");
            e.HasKey(p => p.Sigla);
            e.Property(p => p.Sigla).HasMaxLength(6);
            e.Property(p => p.Descricao).HasMaxLength(60).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
