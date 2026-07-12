using BuildingBlocks;

namespace Financeiro.Contratos;

/// <summary>
/// API pública de consulta do módulo Financeiro. Outros módulos consultam por AQUI —
/// nunca acessando o Dominio/Infra do Financeiro direto.
/// </summary>
public interface IFinanceiroConsulta
{
    /// <summary>Contas a receber (com árvore de parcelas/recebimentos), paginadas.</summary>
    Task<PaginaResultado<ContaReceberDto>> ListarContas(string empresaId, FiltroContasReceber filtro, CancellationToken ct = default);

    /// <summary>Uma conta a receber com a árvore completa.</summary>
    Task<ContaReceberDto?> ObterConta(string empresaId, string contaId, CancellationToken ct = default);

    /// <summary>Sugestão de valores (saldo + juros/multa de mora até hoje) para baixar uma parcela.</summary>
    Task<SugestaoRecebimentoDto?> SugerirRecebimento(string empresaId, string parcelaId, CancellationToken ct = default);

    Task<IReadOnlyList<FormaPagamentoDto>> ListarFormasPagamento(string empresaId, FiltroFormasPagamento filtro, CancellationToken ct = default);
    Task<FormaPagamentoDto?> ObterFormaPagamento(string empresaId, string formaId, CancellationToken ct = default);

    /// <summary>Parâmetros financeiros da empresa (juros de mora + multa). Nunca nulo — devolve zeros se ainda não configurado.</summary>
    Task<ParametrosFinanceirosDto> ObterParametros(string empresaId, CancellationToken ct = default);

    /// <summary>Trilha de auditoria do módulo (fin_auditoria), paginada.</summary>
    Task<PaginaResultado<AuditoriaDto>> ListarAuditoria(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default);
}
