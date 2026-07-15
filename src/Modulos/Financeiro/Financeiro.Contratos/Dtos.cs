namespace Financeiro.Contratos;

// ─────────────────────────────── Leitura (árvore Conta → Parcela → Recebimento) ───────────────────────────────

/// <summary>
/// Conta a Receber com a árvore completa (parcelas + recebimentos) e os campos <b>derivados</b>
/// (total recebido, saldo, situação) — calculados na leitura, nunca persistidos.
/// </summary>
public sealed record ContaReceberDto(
    string Id,
    string ClienteId,
    string? ClienteNome,
    string? Descricao,
    TipoOrigemConta TipoOrigem,
    string? DocumentoOrigem,
    string? NumeroDocumento,
    decimal ValorTotal,
    int QuantidadeParcelas,
    DateOnly DataEmissao,
    string? CategoriaFinanceira,
    string? Observacoes,
    decimal TotalRecebido,
    decimal SaldoTotal,
    SituacaoConta Situacao,
    IReadOnlyList<ParcelaDto> Parcelas,
    IReadOnlyList<RenegociacaoDto> Renegociacoes);

/// <summary>Parcela com seus recebimentos e os valores derivados (saldo, juros, status, dias em atraso).</summary>
public sealed record ParcelaDto(
    string Id,
    int Numero,
    int TotalParcelas,
    decimal ValorOriginal,
    DateOnly Vencimento,
    DateOnly? DataPrevistaRecebimento,
    decimal? PercentualJurosOverride,
    decimal TotalPago,
    decimal SaldoPrincipal,
    decimal Juros,
    decimal Multa,
    decimal SaldoAtualizado,
    int DiasAtraso,
    StatusParcela Status,
    string? Observacoes,
    string? RenegociacaoId,
    IReadOnlyList<RecebimentoDto> Recebimentos);

/// <summary>Recebimento (baixa) de uma parcela — imutável; correção é via estorno.</summary>
public sealed record RecebimentoDto(
    string Id,
    DateOnly Data,
    decimal ValorRecebido,
    decimal Desconto,
    decimal Juros,
    decimal Multa,
    decimal Acrescimos,
    string FormaPagamentoId,
    string? FormaPagamentoNome,
    string? Observacoes,
    string? UsuarioId,
    bool Estornado,
    DateTimeOffset? EstornadoEm,
    string? EstornoMotivo);

/// <summary>Sugestão de valores para um recebimento (saldo + juros/multa de mora até hoje).</summary>
public sealed record SugestaoRecebimentoDto(
    DateOnly Data,
    decimal SaldoPrincipal,
    int DiasAtraso,
    decimal Juros,
    decimal Multa,
    decimal SaldoAtualizado);

// ─────────────────────────────── Filtros ───────────────────────────────

/// <summary>Filtros da listagem de contas a receber (paginada, server-side).</summary>
public sealed record FiltroContasReceber(
    string? ClienteId = null,
    string? Busca = null,
    SituacaoConta? Situacao = null,
    DateOnly? VencimentoDe = null,
    DateOnly? VencimentoAte = null,
    DateOnly? EmissaoDe = null,
    DateOnly? EmissaoAte = null,
    int Pagina = 1,
    int Tamanho = 20);

/// <summary>Filtro da listagem de formas de pagamento.</summary>
public sealed record FiltroFormasPagamento(string? Busca = null, bool? Ativo = null);

// ─────────────────────────────── Entradas (escrita) ───────────────────────────────

/// <summary>Payload de criação de uma Conta a Receber. Se <see cref="Parcelas"/> vier vazio, o sistema gera o plano.</summary>
public sealed record ContaEntradaDto(
    string ClienteId,
    decimal ValorTotal,
    int QuantidadeParcelas,
    DateOnly DataEmissao,
    DateOnly PrimeiroVencimento,
    string? Descricao = null,
    TipoOrigemConta TipoOrigem = TipoOrigemConta.Manual,
    string? DocumentoOrigem = null,
    string? NumeroDocumento = null,
    string? CategoriaFinanceira = null,
    string? Observacoes = null,
    int IntervaloDias = 30,
    IReadOnlyList<ParcelaEntradaDto>? Parcelas = null);

/// <summary>Uma parcela do plano (na criação ou edição de vencimentos/valores antes da confirmação).</summary>
public sealed record ParcelaEntradaDto(
    int Numero,
    decimal Valor,
    DateOnly Vencimento,
    DateOnly? DataPrevistaRecebimento = null,
    decimal? PercentualJurosOverride = null);

/// <summary>Payload de edição do cabeçalho de uma Conta a Receber (não altera o plano de parcelas).</summary>
public sealed record ContaCabecalhoEntradaDto(
    string? Descricao,
    string? DocumentoOrigem,
    string? NumeroDocumento,
    string? CategoriaFinanceira,
    string? Observacoes);

/// <summary>Payload de edição de uma parcela (só permitido conforme regras: valor apenas sem recebimento).</summary>
public sealed record ParcelaEdicaoEntradaDto(
    decimal Valor,
    DateOnly Vencimento,
    DateOnly? DataPrevistaRecebimento,
    decimal? PercentualJurosOverride,
    string? Observacoes);

/// <summary>Payload de registro de um recebimento sobre uma parcela.</summary>
public sealed record RecebimentoEntradaDto(
    DateOnly Data,
    decimal ValorRecebido,
    string FormaPagamentoId,
    decimal Desconto = 0,
    decimal Juros = 0,
    decimal Multa = 0,
    decimal Acrescimos = 0,
    string? Observacoes = null);

/// <summary>Payload de estorno de um recebimento.</summary>
public sealed record EstornoEntradaDto(string? Motivo);

// ─────────────────────────────── Renegociação ───────────────────────────────

/// <summary>Uma renegociação de uma conta: consolidou parcelas de origem e gerou um novo plano.</summary>
public sealed record RenegociacaoDto(
    string Id,
    DateOnly Data,
    decimal ValorBase,
    decimal Desconto,
    decimal Entrada,
    decimal ValorRenegociado,
    string? Observacoes);

/// <summary>Sugestão para renegociar as parcelas selecionadas (saldo consolidado + encargos até hoje).</summary>
public sealed record SugestaoRenegociacaoDto(
    decimal SaldoPrincipal,
    decimal Juros,
    decimal Multa,
    decimal SaldoAtualizado,
    int QtdParcelas);

/// <summary>
/// Payload de renegociação. As <see cref="ParcelaIds"/> (todas da mesma conta) são consolidadas:
/// base = saldo principal, ou saldo atualizado (com juros/multa) se <see cref="IncluirEncargos"/>.
/// O valor a reparcelar = base − <see cref="Desconto"/> − <see cref="Entrada"/>. Se
/// <see cref="Parcelas"/> vier vazio, o novo plano é gerado por quantidade/vencimento/intervalo.
/// Se <see cref="Entrada"/> &gt; 0, vira um recebimento na 1ª parcela gerada (exige a forma).
/// </summary>
public sealed record RenegociacaoEntradaDto(
    IReadOnlyList<string> ParcelaIds,
    DateOnly PrimeiroVencimento,
    int QuantidadeParcelas,
    bool IncluirEncargos = false,
    decimal Desconto = 0,
    decimal Entrada = 0,
    string? EntradaFormaPagamentoId = null,
    int IntervaloDias = 30,
    IReadOnlyList<ParcelaEntradaDto>? Parcelas = null,
    string? Observacoes = null);

// ─────────────────────────────── Forma de pagamento (cadastro) ───────────────────────────────

public sealed record FormaPagamentoDto(string Id, string Nome, bool Ativo);

public sealed record FormaPagamentoEntradaDto(string Nome);

// ─────────────────────────────── Parâmetros financeiros ───────────────────────────────

public sealed record ParametrosFinanceirosDto(decimal JurosMoraMensalPercent, decimal MultaPercent);

public sealed record ParametrosEntradaDto(decimal JurosMoraMensalPercent, decimal MultaPercent);
