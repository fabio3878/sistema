using Acesso.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Acesso.Infraestrutura;

/// <summary>Implementação EF Core das portas de Usuario. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class UsuarioRepositorio(AcessoDbContext db) : IUsuarioRepositorio
{
    public async Task Adicionar(Usuario usuario, CancellationToken ct = default) =>
        await db.Usuarios.AddAsync(usuario, ct);

    public Task<Usuario?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Usuarios
            .Include(u => u.Perfis)
            .FirstOrDefaultAsync(u => u.EmpresaId == empresaId && u.Id == id, ct);

    public Task<Usuario?> ObterPorLogin(string empresaId, string loginNormalizado, CancellationToken ct = default) =>
        db.Usuarios
            .Include(u => u.Perfis)
            .FirstOrDefaultAsync(u => u.EmpresaId == empresaId && u.LoginNormalizado == loginNormalizado, ct);

    public Task<bool> ExisteAlgum(string empresaId, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.EmpresaId == empresaId, ct);

    public async Task<IReadOnlyList<Usuario>> Listar(string empresaId, CancellationToken ct = default) =>
        await db.Usuarios
            .Include(u => u.Perfis)
            .Where(u => u.EmpresaId == empresaId)
            .OrderBy(u => u.LoginNormalizado)
            .ToListAsync(ct);
}

/// <summary>Implementação EF Core das portas de Perfil.</summary>
public sealed class PerfilRepositorio(AcessoDbContext db) : IPerfilRepositorio
{
    public async Task Adicionar(Perfil perfil, CancellationToken ct = default) =>
        await db.Perfis.AddAsync(perfil, ct);

    public Task<Perfil?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Perfis
            .Include(p => p.Funcionalidades)
            .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == id, ct);

    public Task<Perfil?> ObterPorNome(string empresaId, string nome, CancellationToken ct = default) =>
        db.Perfis
            .Include(p => p.Funcionalidades)
            .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Nome == nome, ct);

    public async Task<IReadOnlyList<Perfil>> ObterPorIds(
        string empresaId, IReadOnlyCollection<string> ids, CancellationToken ct = default) =>
        await db.Perfis
            .Include(p => p.Funcionalidades)
            .Where(p => p.EmpresaId == empresaId && ids.Contains(p.Id))
            .ToListAsync(ct);
}

/// <summary>Implementação EF Core das portas de RefreshToken.</summary>
public sealed class RefreshTokenRepositorio(AcessoDbContext db) : IRefreshTokenRepositorio
{
    public async Task Adicionar(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> ObterPorHash(string empresaId, string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.EmpresaId == empresaId && t.TokenHash == tokenHash, ct);

    public async Task RevogarTodosDoUsuario(
        string empresaId, string usuarioId, string motivo, CancellationToken ct = default)
    {
        var agora = DateTimeOffset.UtcNow;
        var ativos = await db.RefreshTokens
            .Where(t => t.EmpresaId == empresaId && t.UsuarioId == usuarioId
                        && t.RevogadoEm == null && t.ExpiraEm > agora)
            .ToListAsync(ct);

        foreach (var t in ativos)
            t.Revogar(motivo);
    }
}
