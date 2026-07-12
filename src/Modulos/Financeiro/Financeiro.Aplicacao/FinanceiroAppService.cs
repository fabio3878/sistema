using BuildingBlocks;
using Cadastros.Contratos;
using Financeiro.Contratos;
using Financeiro.Dominio;

namespace Financeiro.Aplicacao;

/// <summary>
/// Casos de uso de escrita do módulo Financeiro. Fala DTOs de <c>Financeiro.Contratos</c> na borda,
/// valida no domínio, persiste via portas e confirma na unidade de trabalho. Valida o Cliente pela
/// API pública do Cadastros (<see cref="ICadastrosConsulta"/>). Não conhece EF Core.
/// </summary>
public sealed class FinanceiroAppService(
    IContaReceberRepositorio contas,
    IFormaPagamentoRepositorio formas,
    IParametrosRepositorio parametros,
    ICadastrosConsulta cadastros,
    IUnidadeDeTrabalho uow)
{
    // ─────────────────────────────── Contas a Receber ───────────────────────────────

    public async Task<Result<string>> CriarConta(
        string empresaId, string? usuarioId, ContaEntradaDto dados, CancellationToken ct = default)
    {
        if (await cadastros.ObterCliente(empresaId, dados.ClienteId, ct) is null)
            return Result<string>.Falha("Cliente não encontrado.");

        // Plano: usa o que veio do cadastro (vencimentos/valores editados) ou gera automaticamente.
        var plano = (dados.Parcelas is { Count: > 0 })
            ? dados.Parcelas
                .OrderBy(p => p.Numero)
                .Select(p => new PlanoParcela(p.Numero, dados.QuantidadeParcelas, p.Valor, p.Vencimento, p.DataPrevistaRecebimento, p.PercentualJurosOverride))
                .ToArray()
            : ContaReceber.GerarPlano(dados.ValorTotal, dados.QuantidadeParcelas, dados.PrimeiroVencimento, dados.IntervaloDias);

        var dominio = new DadosConta(
            dados.ClienteId, dados.ValorTotal, dados.QuantidadeParcelas, dados.DataEmissao,
            dados.Descricao, dados.TipoOrigem, dados.DocumentoOrigem, dados.NumeroDocumento,
            dados.CategoriaFinanceira, dados.Observacoes, usuarioId);

        var criacao = ContaReceber.Criar(empresaId, dominio, plano);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        await contas.Adicionar(criacao.Valor!, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(criacao.Valor!.Id);
    }

    public async Task<Result> AtualizarCabecalho(
        string empresaId, string contaId, ContaCabecalhoEntradaDto dados, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorId(empresaId, contaId, ct);
        if (conta is null)
            return Result.Falha("Conta a receber não encontrada.");

        conta.AtualizarCabecalho(dados.Descricao, dados.DocumentoOrigem, dados.NumeroDocumento, dados.CategoriaFinanceira, dados.Observacoes);
        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> AlterarParcela(
        string empresaId, string parcelaId, ParcelaEdicaoEntradaDto dados, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorParcela(empresaId, parcelaId, ct);
        if (conta is null)
            return Result.Falha("Parcela não encontrada.");

        var resultado = conta.AlterarParcela(parcelaId, dados.Valor, dados.Vencimento, dados.DataPrevistaRecebimento, dados.PercentualJurosOverride, dados.Observacoes);
        if (resultado.Falhou)
            return resultado;

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> CancelarConta(string empresaId, string contaId, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorId(empresaId, contaId, ct);
        if (conta is null)
            return Result.Falha("Conta a receber não encontrada.");

        var resultado = conta.Cancelar();
        if (resultado.Falhou)
            return resultado;

        await uow.Salvar(ct);
        return Result.Ok();
    }

    // ─────────────────────────────── Recebimentos ───────────────────────────────

    public async Task<Result<string>> RegistrarRecebimento(
        string empresaId, string? usuarioId, string parcelaId, RecebimentoEntradaDto dados, CancellationToken ct = default)
    {
        var forma = await formas.ObterPorId(empresaId, dados.FormaPagamentoId, ct);
        if (forma is null)
            return Result<string>.Falha("Forma de pagamento não encontrada.");
        if (!forma.Ativo)
            return Result<string>.Falha("Forma de pagamento inativa.");

        var conta = await contas.ObterPorParcela(empresaId, parcelaId, ct);
        if (conta is null)
            return Result<string>.Falha("Parcela não encontrada.");

        var dominio = new DadosRecebimento(
            dados.Data, dados.ValorRecebido, dados.FormaPagamentoId,
            dados.Desconto, dados.Juros, dados.Multa, dados.Acrescimos, dados.Observacoes, usuarioId);

        var resultado = conta.RegistrarRecebimento(parcelaId, dominio);
        if (resultado.Falhou)
            return Result<string>.Falha(resultado.Erro!);

        await uow.Salvar(ct);
        return Result<string>.Ok(resultado.Valor!.Id);
    }

    public async Task<Result> EstornarRecebimento(
        string empresaId, string parcelaId, string recebimentoId, string? motivo, CancellationToken ct = default)
    {
        var conta = await contas.ObterPorParcela(empresaId, parcelaId, ct);
        if (conta is null)
            return Result.Falha("Parcela não encontrada.");

        var resultado = conta.EstornarRecebimento(parcelaId, recebimentoId, motivo);
        if (resultado.Falhou)
            return Result.Falha(resultado.Erro!);

        await uow.Salvar(ct);
        return Result.Ok();
    }

    // ─────────────────────────────── Formas de pagamento ───────────────────────────────

    public async Task<Result<string>> CriarFormaPagamento(string empresaId, FormaPagamentoEntradaDto dados, CancellationToken ct = default)
    {
        var criacao = FormaPagamento.Criar(empresaId, dados.Nome);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        if (await formas.ObterPorNome(empresaId, criacao.Valor!.Nome, ct) is not null)
            return Result<string>.Falha("Já existe uma forma de pagamento com este nome.");

        await formas.Adicionar(criacao.Valor!, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(criacao.Valor!.Id);
    }

    public async Task<Result> AtualizarFormaPagamento(string empresaId, string formaId, FormaPagamentoEntradaDto dados, CancellationToken ct = default)
    {
        var forma = await formas.ObterPorId(empresaId, formaId, ct);
        if (forma is null)
            return Result.Falha("Forma de pagamento não encontrada.");

        var atualizacao = forma.Atualizar(dados.Nome);
        if (atualizacao.Falhou)
            return atualizacao;

        var outra = await formas.ObterPorNome(empresaId, forma.Nome, ct);
        if (outra is not null && outra.Id != formaId)
            return Result.Falha("Já existe uma forma de pagamento com este nome.");

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> AlterarSituacaoFormaPagamento(string empresaId, string formaId, bool ativo, CancellationToken ct = default)
    {
        var forma = await formas.ObterPorId(empresaId, formaId, ct);
        if (forma is null)
            return Result.Falha("Forma de pagamento não encontrada.");

        if (ativo) forma.Ativar();
        else forma.Inativar();

        await uow.Salvar(ct);
        return Result.Ok();
    }

    // ─────────────────────────────── Parâmetros ───────────────────────────────

    public async Task<Result> AtualizarParametros(string empresaId, ParametrosEntradaDto dados, CancellationToken ct = default)
    {
        var atual = await parametros.Obter(empresaId, ct);
        if (atual is null)
        {
            var criacao = ParametrosFinanceiros.Criar(empresaId, dados.JurosMoraMensalPercent, dados.MultaPercent);
            if (criacao.Falhou)
                return Result.Falha(criacao.Erro!);
            await parametros.Adicionar(criacao.Valor!, ct);
        }
        else
        {
            var atualizacao = atual.Atualizar(dados.JurosMoraMensalPercent, dados.MultaPercent);
            if (atualizacao.Falhou)
                return atualizacao;
        }

        await uow.Salvar(ct);
        return Result.Ok();
    }
}
