using BuildingBlocks;
using Cadastros.Contratos;
using Cadastros.Dominio;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura.Auditoria;

namespace Cadastros.Infraestrutura;

/// <summary>Leitura da trilha de auditoria do módulo (cad_auditoria) via o leitor compartilhado.</summary>
public sealed class AuditoriaRepositorio(CadastrosDbContext db) : IAuditoriaRepositorio
{
    public Task<PaginaResultado<AuditoriaDto>> Listar(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default) =>
        LeitorAuditoria.Consultar(db, empresaId, filtro, ct);
}

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

    public async Task<IReadOnlyList<Cliente>> Listar(string empresaId, FiltroClientes filtro, CancellationToken ct = default)
    {
        var consulta = db.Clientes
            .Include(c => c.Enderecos)
            .Where(c => c.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var t = $"%{filtro.Busca.Trim()}%";
            consulta = consulta.Where(c => EF.Functions.ILike(c.Nome, t) || EF.Functions.ILike(c.Documento, t));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Cidade))
        {
            var t = $"%{filtro.Cidade.Trim()}%";
            consulta = consulta.Where(c => c.Enderecos.Any(e => EF.Functions.ILike(e.Municipio, t)));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Bairro))
        {
            var t = $"%{filtro.Bairro.Trim()}%";
            consulta = consulta.Where(c => c.Enderecos.Any(e => EF.Functions.ILike(e.Bairro, t)));
        }
        if (filtro.Ativo is not null)
            consulta = consulta.Where(c => c.Ativo == filtro.Ativo);
        if (filtro.MesAniversario is >= 1 and <= 12)
            consulta = consulta.Where(c => c.DataNascimento != null && c.DataNascimento.Value.Month == filtro.MesAniversario);

        return await consulta.OrderBy(c => c.Nome).ToListAsync(ct);
    }
}

/// <summary>Implementação EF Core da porta de localidades (IBGE — tabelas de referência global).</summary>
public sealed class LocalidadeRepositorio(CadastrosDbContext db) : ILocalidadeRepositorio
{
    public async Task<IReadOnlyList<Estado>> ListarEstados(CancellationToken ct = default) =>
        await db.Estados.OrderBy(e => e.Nome).ToListAsync(ct);

    public async Task<IReadOnlyList<Municipio>> ListarMunicipios(string uf, CancellationToken ct = default) =>
        await db.Municipios
            .Where(m => m.Uf == uf.ToUpper())
            .OrderBy(m => m.Nome)
            .ToListAsync(ct);

    public async Task<bool> Vazio(CancellationToken ct = default) => !await db.Estados.AnyAsync(ct);

    public async Task SemearAsync(IEnumerable<Estado> estados, IEnumerable<Municipio> municipios, CancellationToken ct = default)
    {
        await db.Estados.AddRangeAsync(estados, ct);
        await db.Municipios.AddRangeAsync(municipios, ct);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Implementação EF Core da porta de unidades de medida (referência global).</summary>
public sealed class UnidadeRepositorio(CadastrosDbContext db) : IUnidadeRepositorio
{
    public async Task<IReadOnlyList<Unidade>> Listar(CancellationToken ct = default) =>
        await db.Unidades.OrderBy(u => u.Sigla).ToListAsync(ct);

    public Task<Unidade?> ObterPorSigla(string sigla, CancellationToken ct = default) =>
        db.Unidades.FirstOrDefaultAsync(u => u.Sigla == sigla.ToUpper(), ct);

    public async Task<bool> Vazio(CancellationToken ct = default) => !await db.Unidades.AnyAsync(ct);

    public async Task SemearAsync(IEnumerable<Unidade> unidades, CancellationToken ct = default)
    {
        await db.Unidades.AddRangeAsync(unidades, ct);
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Implementação EF Core das portas de Produto. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class ProdutoRepositorio(CadastrosDbContext db) : IProdutoRepositorio
{
    public async Task Adicionar(Produto produto, CancellationToken ct = default) =>
        await db.Produtos.AddAsync(produto, ct);

    public Task<Produto?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Produtos.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == id, ct);

    public Task<Produto?> ObterPorCodigo(string empresaId, string codigo, CancellationToken ct = default) =>
        db.Produtos.FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.CodigoInterno == codigo, ct);

    public async Task<IReadOnlyList<Produto>> Listar(string empresaId, FiltroProdutos filtro, CancellationToken ct = default)
    {
        var consulta = db.Produtos.Where(p => p.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var t = $"%{filtro.Busca.Trim()}%";
            consulta = consulta.Where(p =>
                (p.CodigoInterno != null && EF.Functions.ILike(p.CodigoInterno, t)) ||
                EF.Functions.ILike(p.Descricao, t) ||
                (p.CodigoBarras != null && EF.Functions.ILike(p.CodigoBarras, t)));
        }
        if (filtro.Ativo is not null)
            consulta = consulta.Where(p => p.Ativo == filtro.Ativo);

        return await consulta.OrderBy(p => p.Descricao).ToListAsync(ct);
    }
}

/// <summary>Implementação EF Core das portas de Serviço. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class ServicoRepositorio(CadastrosDbContext db) : IServicoRepositorio
{
    public async Task Adicionar(Servico servico, CancellationToken ct = default) =>
        await db.Servicos.AddAsync(servico, ct);

    public Task<Servico?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Servicos.FirstOrDefaultAsync(s => s.EmpresaId == empresaId && s.Id == id, ct);

    public Task<Servico?> ObterPorCodigo(string empresaId, string codigo, CancellationToken ct = default) =>
        db.Servicos.FirstOrDefaultAsync(s => s.EmpresaId == empresaId && s.CodigoInterno == codigo, ct);

    public async Task<IReadOnlyList<Servico>> Listar(string empresaId, FiltroServicos filtro, CancellationToken ct = default)
    {
        var consulta = db.Servicos.Where(s => s.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var t = $"%{filtro.Busca.Trim()}%";
            consulta = consulta.Where(s =>
                (s.CodigoInterno != null && EF.Functions.ILike(s.CodigoInterno, t)) ||
                EF.Functions.ILike(s.Descricao, t));
        }
        if (filtro.Ativo is not null)
            consulta = consulta.Where(s => s.Ativo == filtro.Ativo);

        return await consulta.OrderBy(s => s.Descricao).ToListAsync(ct);
    }
}
