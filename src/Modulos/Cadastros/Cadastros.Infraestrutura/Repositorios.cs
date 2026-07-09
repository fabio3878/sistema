using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cadastros.Infraestrutura;

/// <summary>Implementação EF Core das portas de Cliente. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class ClienteRepositorio(CadastrosDbContext db) : IClienteRepositorio
{
    public async Task Adicionar(Cliente cliente, CancellationToken ct = default) =>
        await db.Clientes.AddAsync(cliente, ct);

    public Task<Cliente?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Clientes
            .Include(c => c.Enderecos)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Id == id, ct);

    public Task<Cliente?> ObterPorDocumento(string empresaId, string documento, CancellationToken ct = default) =>
        db.Clientes
            .Include(c => c.Enderecos)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Documento == documento, ct);
}

/// <summary>Implementação EF Core das portas de Produto.</summary>
public sealed class ProdutoRepositorio(CadastrosDbContext db) : IProdutoRepositorio
{
    public async Task Adicionar(Produto produto, CancellationToken ct = default) =>
        await db.Produtos.AddAsync(produto, ct);

    public Task<Produto?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Produtos.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == id, ct);
}
