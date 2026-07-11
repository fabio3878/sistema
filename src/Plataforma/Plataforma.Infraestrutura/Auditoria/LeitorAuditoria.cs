using BuildingBlocks;
using Microsoft.EntityFrameworkCore;

namespace Plataforma.Infraestrutura.Auditoria;

/// <summary>
/// Consulta paginada da trilha sobre qualquer DbContext que mapeou <see cref="RegistroAuditoria"/>.
/// Query ÚNICA e compartilhada — cada módulo pluga seu próprio DbContext (cad_auditoria,
/// acs_auditoria) sem duplicar filtro/paginação/projeção.
/// </summary>
public static class LeitorAuditoria
{
    private const int TamanhoMaximo = 100;

    public static async Task<PaginaResultado<AuditoriaDto>> Consultar(
        DbContext db, string empresaId, FiltroAuditoria filtro, CancellationToken ct = default)
    {
        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamanho = filtro.Tamanho is < 1 or > TamanhoMaximo ? 20 : filtro.Tamanho;

        var q = db.Set<RegistroAuditoria>().AsNoTracking().Where(r => r.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Entidade))
            q = q.Where(r => r.Entidade == filtro.Entidade);
        if (!string.IsNullOrWhiteSpace(filtro.RegistroId))
            q = q.Where(r => r.RegistroId == filtro.RegistroId);
        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
        {
            var termo = $"%{filtro.Usuario.Trim()}%";
            q = q.Where(r => r.UsuarioLogin != null && EF.Functions.ILike(r.UsuarioLogin, termo));
        }
        if (filtro.Operacao is { } op)
            q = q.Where(r => r.Operacao == op);
        if (filtro.De is { } de)
            q = q.Where(r => r.OcorridoEm >= de);
        if (filtro.Ate is { } ate)
            q = q.Where(r => r.OcorridoEm <= ate);

        var total = await q.CountAsync(ct);

        // Ordena pelo Id (ULID): codifica o timestamp nos bits altos, então "Id desc" = mais
        // recente primeiro — e é ordenação de string, portátil (Postgres e SQLite; SQLite não
        // ordena por DateTimeOffset).
        var itens = await q
            .OrderByDescending(r => r.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Select(r => new AuditoriaDto(
                r.Id, r.OcorridoEm, r.UsuarioId, r.UsuarioLogin,
                r.Entidade, r.RegistroId, r.Operacao, r.Alteracoes))
            .ToListAsync(ct);

        return new PaginaResultado<AuditoriaDto>(itens, total, pagina, tamanho);
    }
}
