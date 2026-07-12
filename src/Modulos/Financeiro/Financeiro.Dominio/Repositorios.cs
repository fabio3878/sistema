using BuildingBlocks;
using Financeiro.Contratos;

namespace Financeiro.Dominio;

/// <summary>
/// Portas de persistência do módulo (o Dominio define, a Infraestrutura implementa).
/// Toda leitura filtra por EmpresaId (tenant).
/// </summary>
public interface IContaReceberRepositorio
{
    Task Adicionar(ContaReceber conta, CancellationToken ct = default);

    /// <summary>Carrega a conta com parcelas e recebimentos (agregado completo).</summary>
    Task<ContaReceber?> ObterPorId(string empresaId, string id, CancellationToken ct = default);

    /// <summary>Carrega a conta que contém a parcela informada (agregado completo).</summary>
    Task<ContaReceber?> ObterPorParcela(string empresaId, string parcelaId, CancellationToken ct = default);

    /// <summary>Carrega só a parcela (com recebimentos) — para cálculo de sugestão de recebimento.</summary>
    Task<Parcela?> ObterParcela(string empresaId, string parcelaId, CancellationToken ct = default);

    /// <summary>Lista paginada das contas (com árvore), aplicando os filtros. <paramref name="hoje"/> resolve o filtro de vencidas.</summary>
    Task<PaginaResultado<ContaReceber>> Listar(string empresaId, FiltroContasReceber filtro, DateOnly hoje, CancellationToken ct = default);
}

public interface IFormaPagamentoRepositorio
{
    Task Adicionar(FormaPagamento forma, CancellationToken ct = default);
    Task<FormaPagamento?> ObterPorId(string empresaId, string id, CancellationToken ct = default);
    Task<FormaPagamento?> ObterPorNome(string empresaId, string nome, CancellationToken ct = default);
    Task<IReadOnlyList<FormaPagamento>> Listar(string empresaId, FiltroFormasPagamento filtro, CancellationToken ct = default);
}

public interface IParametrosRepositorio
{
    Task<ParametrosFinanceiros?> Obter(string empresaId, CancellationToken ct = default);
    Task Adicionar(ParametrosFinanceiros parametros, CancellationToken ct = default);
}

/// <summary>Confirma as mutações pendentes numa transação (Unit of Work do módulo).</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> Salvar(CancellationToken ct = default);
}

/// <summary>Porta de leitura paginada da trilha de auditoria do módulo (fin_auditoria).</summary>
public interface IAuditoriaRepositorio
{
    Task<PaginaResultado<AuditoriaDto>> Listar(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default);
}
