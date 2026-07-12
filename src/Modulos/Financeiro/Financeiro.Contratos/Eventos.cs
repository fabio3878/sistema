using BuildingBlocks;

namespace Financeiro.Contratos;

/// <summary>Um recebimento (baixa) foi confirmado numa parcela. Consumidores futuros: Caixa, Contabilidade.</summary>
public sealed record RecebimentoConfirmado(
    string EmpresaId,
    string ContaReceberId,
    string ParcelaId,
    string RecebimentoId,
    decimal ValorRecebido) : IEventoDominio;

/// <summary>Um recebimento foi estornado. Consumidores futuros: Caixa, Contabilidade.</summary>
public sealed record RecebimentoEstornado(
    string EmpresaId,
    string ContaReceberId,
    string ParcelaId,
    string RecebimentoId,
    decimal ValorEstornado) : IEventoDominio;

/// <summary>
/// Títulos (contas a receber) gerados a partir de uma venda a prazo — RESERVADO para a integração
/// Vendas → Financeiro (fase 2). Declarado agora para o contrato nascer estável.
/// </summary>
public sealed record TitulosGerados(
    string EmpresaId,
    string ContaReceberId,
    string? VendaId) : IEventoDominio;
