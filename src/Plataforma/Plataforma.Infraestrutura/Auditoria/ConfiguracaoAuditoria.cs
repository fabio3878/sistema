using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Plataforma.Infraestrutura.Auditoria;

/// <summary>
/// Mapeia <see cref="RegistroAuditoria"/> para a tabela de trilha do módulo. O MESMO tipo CLR é
/// mapeado para tabelas diferentes por DbContext (cad_auditoria, acs_auditoria) — cada contexto
/// tem seu próprio modelo. Chamada no OnModelCreating de cada módulo.
/// </summary>
public static class ConfiguracaoAuditoria
{
    public static EntityTypeBuilder<RegistroAuditoria> ConfigurarAuditoria(
        this EntityTypeBuilder<RegistroAuditoria> e, string tabela)
    {
        e.ToTable(tabela);
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(26).IsRequired();
        e.Property(x => x.EmpresaId).HasMaxLength(26).IsRequired();
        e.Property(x => x.RegistroId).HasMaxLength(26).IsRequired();
        e.Property(x => x.UsuarioId).HasMaxLength(26);
        e.Property(x => x.UsuarioLogin).HasMaxLength(256);
        e.Property(x => x.Entidade).HasMaxLength(128).IsRequired();
        // Convenção do projeto: enum como int no banco.
        e.Property(x => x.Operacao).HasConversion<int>();
        // JSON como text (não jsonb) — denominador comum com SQLite (ARQUITETURA §11).
        e.Property(x => x.Alteracoes).IsRequired();

        // Consulta típica: histórico de um registro; e varredura por tempo.
        e.HasIndex(x => new { x.EmpresaId, x.Entidade, x.RegistroId });
        e.HasIndex(x => x.OcorridoEm);

        return e;
    }
}
