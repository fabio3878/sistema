using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Config comum de TODA entidade sincronizável (PK ULID, tenant, soft delete, carimbos).
/// Fonte ÚNICA — todo DbContext de módulo deve chamar isto por entidade no OnModelCreating
/// (ver CLAUDE.md). Garante comportamento uniforme em toda tabela, presente e futura.
/// </summary>
public static class ConfiguracaoEntidadeBase
{
    public static EntityTypeBuilder<T> ConfigurarEntidadeBase<T>(this EntityTypeBuilder<T> e)
        where T : EntidadeBase
    {
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(26).IsRequired();
        e.Property(x => x.EmpresaId).HasMaxLength(26).IsRequired();
        e.Property(x => x.OrigemId).HasMaxLength(26);
        e.HasIndex(x => x.EmpresaId);
        // Soft delete: some da query por padrão quem está excluído (seção 4.1).
        e.HasQueryFilter(x => !x.Excluido);

        // CriadoEm = data de insert: gravada uma vez, IMUTÁVEL depois. Update que a altere lança.
        e.Property(x => x.CriadoEm).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);

        return e;
    }
}
