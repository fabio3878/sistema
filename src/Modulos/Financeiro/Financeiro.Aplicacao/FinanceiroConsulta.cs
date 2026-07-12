using BuildingBlocks;
using Cadastros.Contratos;
using Financeiro.Contratos;
using Financeiro.Dominio;

namespace Financeiro.Aplicacao;

/// <summary>
/// Implementa a API pública de consulta (<see cref="IFinanceiroConsulta"/>). O repositório traz os
/// campos crus; os valores <b>derivados</b> (saldo, juros, status, situação) são calculados aqui,
/// em C#, com a data de hoje — nunca vêm persistidos. Depende só das portas do Dominio (nada de EF).
/// </summary>
public sealed class FinanceiroConsulta(
    IContaReceberRepositorio contas,
    IFormaPagamentoRepositorio formas,
    IParametrosRepositorio parametros,
    ICadastrosConsulta cadastros,
    IAuditoriaRepositorio auditoria)
    : IFinanceiroConsulta
{
    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<PaginaResultado<ContaReceberDto>> ListarContas(string empresaId, FiltroContasReceber filtro, CancellationToken ct = default)
    {
        var hoje = Hoje;
        var (juros, multa) = await CarregarParametros(empresaId, ct);
        var formasNome = await MapaFormas(empresaId, ct);

        var pagina = await contas.Listar(empresaId, filtro, hoje, ct);

        // Resolve o nome do cliente uma vez por id distinto na página.
        var nomes = new Dictionary<string, string?>();
        foreach (var id in pagina.Itens.Select(c => c.ClienteId).Distinct())
            nomes[id] = (await cadastros.ObterCliente(empresaId, id, ct))?.Nome;

        var itens = pagina.Itens
            .Select(c => MapConta(c, hoje, juros, multa, formasNome, nomes.GetValueOrDefault(c.ClienteId)))
            .ToArray();

        return new PaginaResultado<ContaReceberDto>(itens, pagina.Total, pagina.Pagina, pagina.Tamanho);
    }

    public async Task<ContaReceberDto?> ObterConta(string empresaId, string contaId, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorId(empresaId, contaId, ct);
        if (conta is null) return null;

        var hoje = Hoje;
        var (juros, multa) = await CarregarParametros(empresaId, ct);
        var formasNome = await MapaFormas(empresaId, ct);
        var clienteNome = (await cadastros.ObterCliente(empresaId, conta.ClienteId, ct))?.Nome;

        return MapConta(conta, hoje, juros, multa, formasNome, clienteNome);
    }

    public async Task<SugestaoRecebimentoDto?> SugerirRecebimento(string empresaId, string parcelaId, CancellationToken ct = default)
    {
        var parcela = await contas.ObterParcela(empresaId, parcelaId, ct);
        if (parcela is null) return null;

        var hoje = Hoje;
        var (juros, multa) = await CarregarParametros(empresaId, ct);
        var calc = parcela.Calcular(hoje, juros, multa);

        return new SugestaoRecebimentoDto(hoje, calc.SaldoPrincipal, calc.DiasAtraso, calc.Juros, calc.Multa, calc.SaldoAtualizado);
    }

    public async Task<IReadOnlyList<FormaPagamentoDto>> ListarFormasPagamento(string empresaId, FiltroFormasPagamento filtro, CancellationToken ct = default)
    {
        var lista = await formas.Listar(empresaId, filtro, ct);
        return lista.Select(f => new FormaPagamentoDto(f.Id, f.Nome, f.Ativo)).ToArray();
    }

    public async Task<FormaPagamentoDto?> ObterFormaPagamento(string empresaId, string formaId, CancellationToken ct = default)
    {
        var f = await formas.ObterPorId(empresaId, formaId, ct);
        return f is null ? null : new FormaPagamentoDto(f.Id, f.Nome, f.Ativo);
    }

    public async Task<ParametrosFinanceirosDto> ObterParametros(string empresaId, CancellationToken ct = default)
    {
        var (juros, multa) = await CarregarParametros(empresaId, ct);
        return new ParametrosFinanceirosDto(juros, multa);
    }

    public Task<PaginaResultado<AuditoriaDto>> ListarAuditoria(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default) =>
        auditoria.Listar(empresaId, filtro, ct);

    // ─────────────────────────────── Mapeamento + derivação ───────────────────────────────

    private static ContaReceberDto MapConta(
        ContaReceber conta, DateOnly hoje, decimal juros, decimal multa,
        IReadOnlyDictionary<string, string> formasNome, string? clienteNome)
    {
        var parcelasOrdenadas = conta.Parcelas.OrderBy(p => p.Numero).ToArray();
        var parcelasDto = new List<ParcelaDto>(parcelasOrdenadas.Length);
        var totalRecebido = 0m;
        var saldoTotal = 0m;
        var statuses = new List<StatusParcela>(parcelasOrdenadas.Length);

        foreach (var p in parcelasOrdenadas)
        {
            var calc = p.Calcular(hoje, juros, multa);
            totalRecebido += p.TotalPago;
            saldoTotal += calc.SaldoAtualizado;
            statuses.Add(calc.Status);

            var recebimentos = p.Recebimentos
                .OrderBy(r => r.Data).ThenBy(r => r.Id)
                .Select(r => new RecebimentoDto(
                    r.Id, r.Data, r.ValorRecebido, r.Desconto, r.Juros, r.Multa, r.Acrescimos,
                    r.FormaPagamentoId, formasNome.GetValueOrDefault(r.FormaPagamentoId),
                    r.Observacoes, r.UsuarioId, r.Estornado, r.EstornadoEm, r.EstornoMotivo))
                .ToArray();

            parcelasDto.Add(new ParcelaDto(
                p.Id, p.Numero, p.TotalParcelas, p.ValorOriginal, p.Vencimento, p.DataPrevistaRecebimento,
                p.PercentualJurosOverride, p.TotalPago, calc.SaldoPrincipal, calc.Juros, calc.Multa,
                calc.SaldoAtualizado, calc.DiasAtraso, calc.Status, p.Observacoes, recebimentos));
        }

        return new ContaReceberDto(
            conta.Id, conta.ClienteId, clienteNome, conta.Descricao, conta.TipoOrigem,
            conta.DocumentoOrigem, conta.NumeroDocumento, conta.ValorTotal, conta.QuantidadeParcelas,
            conta.DataEmissao, conta.CategoriaFinanceira, conta.Observacoes,
            totalRecebido, saldoTotal, SituacaoDe(statuses), parcelasDto);
    }

    /// <summary>Situação geral da conta, derivada da situação das parcelas (precedência da regra de negócio).</summary>
    private static SituacaoConta SituacaoDe(IReadOnlyList<StatusParcela> statuses)
    {
        if (statuses.Count == 0) return SituacaoConta.EmAberto;
        if (statuses.All(s => s == StatusParcela.Cancelada)) return SituacaoConta.Cancelada;

        // Considera só as parcelas "vivas" (ignora canceladas/renegociadas).
        var vivas = statuses.Where(s => s is not (StatusParcela.Cancelada or StatusParcela.Renegociada)).ToArray();
        if (vivas.Length == 0) return SituacaoConta.Cancelada;

        if (vivas.All(s => s == StatusParcela.Recebida)) return SituacaoConta.Quitada;
        if (vivas.Any(s => s == StatusParcela.Vencida)) return SituacaoConta.PossuiParcelasVencidas;
        if (vivas.Any(s => s is StatusParcela.Recebida or StatusParcela.RecebidaParcial)) return SituacaoConta.ParcialmenteRecebida;
        return SituacaoConta.EmAberto;
    }

    private async Task<(decimal Juros, decimal Multa)> CarregarParametros(string empresaId, CancellationToken ct)
    {
        var p = await parametros.Obter(empresaId, ct);
        return p is null ? (0m, 0m) : (p.JurosMoraMensalPercent, p.MultaPercent);
    }

    private async Task<IReadOnlyDictionary<string, string>> MapaFormas(string empresaId, CancellationToken ct)
    {
        var lista = await formas.Listar(empresaId, new FiltroFormasPagamento(), ct);
        return lista.ToDictionary(f => f.Id, f => f.Nome);
    }
}
