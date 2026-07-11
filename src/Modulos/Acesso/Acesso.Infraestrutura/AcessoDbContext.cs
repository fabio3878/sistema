using Acesso.Dominio;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura;
using Plataforma.Infraestrutura.Auditoria;

namespace Acesso.Infraestrutura;

/// <summary>
/// DbContext do módulo Acesso (prefixo acs_). Provider trocável (Postgres servidor / SQLite local
/// futuro) — configurado no host. Duas camadas: dados de tenant (Tier A, herdam EntidadeBase) e o
/// catálogo de capacidade (Tier B, dado de referência global que NÃO herda EntidadeBase).
/// </summary>
public sealed class AcessoDbContext(DbContextOptions<AcessoDbContext> options)
    : DbContext(options), IUnidadeDeTrabalho
{
    // Tier A — dados de tenant
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<UsuarioPerfil> UsuarioPerfis => Set<UsuarioPerfil>();
    public DbSet<PerfilFuncionalidade> PerfilFuncionalidades => Set<PerfilFuncionalidade>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Tier B — catálogo de capacidade (referência global)
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();

    public Task<int> Salvar(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("acs_usuarios");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.Login).HasMaxLength(100).IsRequired();
            e.Property(p => p.LoginNormalizado).HasMaxLength(100).IsRequired();
            e.Property(p => p.NomeExibicao).HasMaxLength(200).IsRequired();
            e.Property(p => p.Email).HasMaxLength(200);
            e.Property(p => p.SenhaHash).HasMaxLength(200).IsRequired();
            e.Property(p => p.StampSeguranca).HasMaxLength(26).IsRequired();
            e.HasIndex(p => new { p.EmpresaId, p.LoginNormalizado }).IsUnique();

            // Perfis do usuário: 1:N agregado. Cascata segue o ciclo de vida do usuário.
            e.HasMany(p => p.Perfis)
                .WithOne()
                .HasForeignKey(up => up.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Perfis).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Perfil>(e =>
        {
            e.ToTable("acs_perfis");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(300);
            e.HasIndex(p => new { p.EmpresaId, p.Nome }).IsUnique();

            e.HasMany(p => p.Funcionalidades)
                .WithOne()
                .HasForeignKey(pf => pf.PerfilId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(p => p.Funcionalidades).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UsuarioPerfil>(e =>
        {
            e.ToTable("acs_usuario_perfis");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.UsuarioId).HasMaxLength(26).IsRequired();
            e.Property(p => p.PerfilId).HasMaxLength(26).IsRequired();
            e.HasIndex(p => new { p.EmpresaId, p.UsuarioId, p.PerfilId }).IsUnique();
        });

        modelBuilder.Entity<PerfilFuncionalidade>(e =>
        {
            e.ToTable("acs_perfil_funcionalidades");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.PerfilId).HasMaxLength(26).IsRequired();
            e.Property(p => p.FuncionalidadeCodigo).HasMaxLength(100).IsRequired();
            e.HasIndex(p => new { p.EmpresaId, p.PerfilId, p.FuncionalidadeCodigo }).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("acs_refresh_tokens");
            e.ConfigurarEntidadeBase();
            e.Property(p => p.UsuarioId).HasMaxLength(26).IsRequired();
            e.Property(p => p.StampSeguranca).HasMaxLength(26).IsRequired();
            e.Property(p => p.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(p => p.MotivoRevogacao).HasMaxLength(200);
            e.Property(p => p.SubstituidoPorId).HasMaxLength(26);
            e.HasIndex(p => new { p.EmpresaId, p.TokenHash }).IsUnique();
            e.HasIndex(p => new { p.EmpresaId, p.UsuarioId });
        });

        // TIER B — catálogo. Dado de referência GLOBAL: chave natural (Codigo), sem EmpresaId, sem
        // soft delete, sem ULID. Por isso NÃO chama ConfigurarEntidadeBase() — desvio consciente.
        modelBuilder.Entity<Modulo>(e =>
        {
            e.ToTable("acs_modulos");
            e.HasKey(p => p.Codigo);
            e.Property(p => p.Codigo).HasMaxLength(30);
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(300);
        });

        modelBuilder.Entity<Funcionalidade>(e =>
        {
            e.ToTable("acs_funcionalidades");
            e.HasKey(p => p.Codigo);
            e.Property(p => p.Codigo).HasMaxLength(100);
            e.Property(p => p.ModuloCodigo).HasMaxLength(30).IsRequired();
            e.Property(p => p.Nome).HasMaxLength(150).IsRequired();
            e.Property(p => p.Descricao).HasMaxLength(300);
            e.HasIndex(p => p.ModuloCodigo);
            e.HasOne<Modulo>()
                .WithMany()
                .HasForeignKey(p => p.ModuloCodigo)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Trilha de auditoria do módulo (append-only) — ver AuditoriaInterceptor.
        modelBuilder.Entity<RegistroAuditoria>().ConfigurarAuditoria("acs_auditoria");

        base.OnModelCreating(modelBuilder);
    }
}
