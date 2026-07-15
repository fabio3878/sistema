using BuildingBlocks;
using Financeiro.Contratos;
using Financeiro.Dominio;
using Microsoft.EntityFrameworkCore;
using Plataforma.Infraestrutura.Auditoria;

namespace Financeiro.Infraestrutura;

/// <summary>Leitura da trilha de auditoria do módulo (fin_auditoria) via o leitor compartilhado.</summary>
public sealed class AuditoriaRepositorio(FinanceiroDbContext db) : IAuditoriaRepositorio
{
    public Task<PaginaResultado<AuditoriaDto>> Listar(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default) =>
        LeitorAuditoria.Consultar(db, empresaId, filtro, ct);
}

/// <summary>Implementação EF Core das portas de Conta a Receber. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class ContaReceberRepositorio(FinanceiroDbContext db) : IContaReceberRepositorio
{
    public async Task Adicionar(ContaReceber conta, CancellationToken ct = default) =>
        await db.Contas.AddAsync(conta, ct);

    public Task<ContaReceber?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.Contas
            .Include(c => c.Parcelas).ThenInclude(p => p.Recebimentos)
            .Include(c => c.Renegociacoes)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Id == id, ct);

    public Task<ContaReceber?> ObterPorParcela(string empresaId, string parcelaId, CancellationToken ct = default) =>
        db.Contas
            .Include(c => c.Parcelas).ThenInclude(p => p.Recebimentos)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Parcelas.Any(p => p.Id == parcelaId), ct);

    public Task<Parcela?> ObterParcela(string empresaId, string parcelaId, CancellationToken ct = default) =>
        db.Parcelas
            .Include(p => p.Recebimentos)
            .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Id == parcelaId, ct);

    public async Task<PaginaResultado<ContaReceber>> Listar(
        string empresaId, FiltroContasReceber filtro, DateOnly hoje, CancellationToken ct = default)
    {
        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamanho = filtro.Tamanho is < 1 or > 100 ? 20 : filtro.Tamanho;

        var q = db.Contas.Where(c => c.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.ClienteId))
            q = q.Where(c => c.ClienteId == filtro.ClienteId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var t = $"%{filtro.Busca.Trim()}%";
            q = q.Where(c =>
                (c.Descricao != null && EF.Functions.ILike(c.Descricao, t)) ||
                (c.DocumentoOrigem != null && EF.Functions.ILike(c.DocumentoOrigem, t)) ||
                (c.NumeroDocumento != null && EF.Functions.ILike(c.NumeroDocumento, t)));
        }

        if (filtro.EmissaoDe is { } ed) q = q.Where(c => c.DataEmissao >= ed);
        if (filtro.EmissaoAte is { } ea) q = q.Where(c => c.DataEmissao <= ea);
        if (filtro.VencimentoDe is { } vd) q = q.Where(c => c.Parcelas.Any(p => p.Vencimento >= vd));
        if (filtro.VencimentoAte is { } va) q = q.Where(c => c.Parcelas.Any(p => p.Vencimento <= va));

        q = AplicarSituacao(q, filtro.Situacao, hoje);

        var total = await q.CountAsync(ct);

        var itens = await q
            .OrderByDescending(c => c.DataEmissao).ThenByDescending(c => c.Id)
            .Skip((pagina - 1) * tamanho).Take(tamanho)
            .Include(c => c.Parcelas).ThenInclude(p => p.Recebimentos)
            .Include(c => c.Renegociacoes)
            .ToListAsync(ct);

        return new PaginaResultado<ContaReceber>(itens, total, pagina, tamanho);
    }

    /// <summary>Traduz o filtro de situação (derivada) para predicados EXISTS/ALL sobre as parcelas — portável.</summary>
    private static IQueryable<ContaReceber> AplicarSituacao(IQueryable<ContaReceber> q, SituacaoConta? situacao, DateOnly hoje) =>
        situacao switch
        {
            SituacaoConta.PossuiParcelasVencidas => q.Where(c =>
                c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada && p.ValorOriginal - p.TotalPago > 0 && p.Vencimento < hoje)),

            SituacaoConta.Quitada => q.Where(c =>
                c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada) &&
                c.Parcelas.Where(p => !p.Cancelada && !p.Renegociada).All(p => p.ValorOriginal - p.TotalPago <= 0)),

            SituacaoConta.Cancelada => q.Where(c => c.Parcelas.All(p => p.Cancelada)),

            SituacaoConta.ParcialmenteRecebida => q.Where(c =>
                c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada && p.TotalPago > 0) &&
                c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada && p.ValorOriginal - p.TotalPago > 0) &&
                !c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada && p.ValorOriginal - p.TotalPago > 0 && p.Vencimento < hoje)),

            SituacaoConta.EmAberto => q.Where(c =>
                c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada) &&
                c.Parcelas.Where(p => !p.Cancelada && !p.Renegociada).All(p => p.TotalPago == 0) &&
                !c.Parcelas.Any(p => !p.Cancelada && !p.Renegociada && p.ValorOriginal - p.TotalPago > 0 && p.Vencimento < hoje)),

            _ => q,
        };
}

/// <summary>Implementação EF Core das portas de Forma de Pagamento. Toda leitura filtra por EmpresaId (tenant).</summary>
public sealed class FormaPagamentoRepositorio(FinanceiroDbContext db) : IFormaPagamentoRepositorio
{
    public async Task Adicionar(FormaPagamento forma, CancellationToken ct = default) =>
        await db.FormasPagamento.AddAsync(forma, ct);

    public Task<FormaPagamento?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        db.FormasPagamento.FirstOrDefaultAsync(f => f.EmpresaId == empresaId && f.Id == id, ct);

    public Task<FormaPagamento?> ObterPorNome(string empresaId, string nome, CancellationToken ct = default) =>
        db.FormasPagamento.FirstOrDefaultAsync(f => f.EmpresaId == empresaId && f.Nome == nome, ct);

    public async Task<IReadOnlyList<FormaPagamento>> Listar(string empresaId, FiltroFormasPagamento filtro, CancellationToken ct = default)
    {
        var q = db.FormasPagamento.Where(f => f.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var t = $"%{filtro.Busca.Trim()}%";
            q = q.Where(f => EF.Functions.ILike(f.Nome, t));
        }
        if (filtro.Ativo is not null)
            q = q.Where(f => f.Ativo == filtro.Ativo);

        return await q.OrderBy(f => f.Nome).ToListAsync(ct);
    }
}

/// <summary>Implementação EF Core da porta de Parâmetros Financeiros (1 linha por empresa).</summary>
public sealed class ParametrosRepositorio(FinanceiroDbContext db) : IParametrosRepositorio
{
    public Task<ParametrosFinanceiros?> Obter(string empresaId, CancellationToken ct = default) =>
        db.Parametros.FirstOrDefaultAsync(p => p.EmpresaId == empresaId, ct);

    public async Task Adicionar(ParametrosFinanceiros parametros, CancellationToken ct = default) =>
        await db.Parametros.AddAsync(parametros, ct);
}
