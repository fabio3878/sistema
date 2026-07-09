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
}
