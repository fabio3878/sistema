using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Cadastros.Infraestrutura;

/// <summary>Implementação EF Core das portas de Pessoa. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class PessoaRepositorio(CadastrosDbContext db) : IPessoaRepositorio
{
    public async Task Adicionar(Pessoa pessoa, CancellationToken ct = default) =>
        await db.Pessoas.AddAsync(pessoa, ct);

    public Task<Pessoa?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Pessoas.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == id, ct);
}

/// <summary>Implementação EF Core das portas de Produto.</summary>
public sealed class ProdutoRepositorio(CadastrosDbContext db) : IProdutoRepositorio
{
    public async Task Adicionar(Produto produto, CancellationToken ct = default) =>
        await db.Produtos.AddAsync(produto, ct);

    public Task<Produto?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Produtos.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == id, ct);
}
