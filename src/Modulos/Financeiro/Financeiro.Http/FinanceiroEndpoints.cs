using BuildingBlocks;
using Financeiro.Aplicacao;
using Financeiro.Contratos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Plataforma.Dominio;

namespace Financeiro.Http;

/// <summary>
/// Endpoints HTTP do módulo Financeiro (minimal API). Todos exigem autenticação + a funcionalidade
/// correspondente (<c>fin.*</c>). O tenant vem SEMPRE do <see cref="IContextoEmpresa"/>; o usuário
/// que registra recebimento/lança conta vem do <see cref="IContextoUsuario"/>. Falha de domínio
/// (<c>Result</c>) vira 400 com <c>{ erro }</c>.
/// </summary>
public static class FinanceiroEndpoints
{
    public static IEndpointRouteBuilder MapFinanceiroEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/fin");

        // ─────────────────────────────── Contas a Receber ───────────────────────────────

        grupo.MapGet("/contas-receber", async (
            IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? clienteId, string? busca, SituacaoConta? situacao,
            DateOnly? vencimentoDe, DateOnly? vencimentoAte, DateOnly? emissaoDe, DateOnly? emissaoAte,
            int? pagina, int? tamanho) =>
        {
            var filtro = new FiltroContasReceber(clienteId, busca, situacao,
                vencimentoDe, vencimentoAte, emissaoDe, emissaoAte, pagina ?? 1, tamanho ?? 20);
            return Results.Ok(await consulta.ListarContas(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.ListarContaReceber));

        grupo.MapGet("/contas-receber/{id}", async (
            string id, IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var conta = await consulta.ObterConta(empresa.EmpresaId, id, ct);
            return conta is null ? Results.NotFound() : Results.Ok(conta);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.ListarContaReceber));

        grupo.MapPost("/contas-receber", async (
            ContaEntradaDto req, FinanceiroAppService svc,
            IContextoEmpresa empresa, IContextoUsuario usuario, CancellationToken ct) =>
        {
            var r = await svc.CriarConta(empresa.EmpresaId, usuario.UsuarioId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.CriarContaReceber));

        grupo.MapPut("/contas-receber/{id}", async (
            string id, ContaCabecalhoEntradaDto req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarCabecalho(empresa.EmpresaId, id, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarContaReceber));

        grupo.MapPost("/contas-receber/{id}/cancelar", async (
            string id, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.CancelarConta(empresa.EmpresaId, id, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.CancelarContaReceber));

        grupo.MapPut("/contas-receber/parcelas/{parcelaId}", async (
            string parcelaId, ParcelaEdicaoEntradaDto req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarParcela(empresa.EmpresaId, parcelaId, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarContaReceber));

        grupo.MapGet("/contas-receber/{id}/renegociacao/sugestao", async (
            string id, IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string[]? parcelaIds, bool? incluirEncargos) =>
        {
            var sugestao = await consulta.SugerirRenegociacao(empresa.EmpresaId, id, parcelaIds ?? [], incluirEncargos ?? false, ct);
            return sugestao is null ? Results.NotFound() : Results.Ok(sugestao);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.RenegociarContaReceber));

        grupo.MapPost("/contas-receber/{id}/renegociar", async (
            string id, RenegociacaoEntradaDto req, FinanceiroAppService svc,
            IContextoEmpresa empresa, IContextoUsuario usuario, CancellationToken ct) =>
        {
            var r = await svc.Renegociar(empresa.EmpresaId, usuario.UsuarioId, id, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.RenegociarContaReceber));

        // ─────────────────────────────── Recebimentos ───────────────────────────────

        grupo.MapGet("/contas-receber/parcelas/{parcelaId}/sugestao", async (
            string parcelaId, IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var sugestao = await consulta.SugerirRecebimento(empresa.EmpresaId, parcelaId, ct);
            return sugestao is null ? Results.NotFound() : Results.Ok(sugestao);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.RegistrarRecebimento));

        grupo.MapPost("/contas-receber/parcelas/{parcelaId}/recebimentos", async (
            string parcelaId, RecebimentoEntradaDto req, FinanceiroAppService svc,
            IContextoEmpresa empresa, IContextoUsuario usuario, CancellationToken ct) =>
        {
            var r = await svc.RegistrarRecebimento(empresa.EmpresaId, usuario.UsuarioId, parcelaId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.RegistrarRecebimento));

        grupo.MapPost("/contas-receber/parcelas/{parcelaId}/recebimentos/{recebimentoId}/estornar", async (
            string parcelaId, string recebimentoId, EstornoEntradaDto? req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.EstornarRecebimento(empresa.EmpresaId, parcelaId, recebimentoId, req?.Motivo, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EstornarRecebimento));

        // ─────────────────────────────── Formas de pagamento ───────────────────────────────

        grupo.MapGet("/formas-pagamento", async (
            IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct, string? busca, string? situacao) =>
        {
            bool? ativo = situacao?.ToLowerInvariant() switch { "ativo" => true, "inativo" => false, _ => null };
            var filtro = new FiltroFormasPagamento(busca, ativo);
            return Results.Ok(await consulta.ListarFormasPagamento(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.ListarFormaPagamento));

        grupo.MapGet("/formas-pagamento/{id}", async (
            string id, IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var forma = await consulta.ObterFormaPagamento(empresa.EmpresaId, id, ct);
            return forma is null ? Results.NotFound() : Results.Ok(forma);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.ListarFormaPagamento));

        grupo.MapPost("/formas-pagamento", async (
            FormaPagamentoEntradaDto req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.CriarFormaPagamento(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.CriarFormaPagamento));

        grupo.MapPut("/formas-pagamento/{id}", async (
            string id, FormaPagamentoEntradaDto req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarFormaPagamento(empresa.EmpresaId, id, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarFormaPagamento));

        grupo.MapPost("/formas-pagamento/{id}/ativar", async (
            string id, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoFormaPagamento(empresa.EmpresaId, id, ativo: true, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarFormaPagamento));

        grupo.MapPost("/formas-pagamento/{id}/inativar", async (
            string id, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoFormaPagamento(empresa.EmpresaId, id, ativo: false, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarFormaPagamento));

        // ─────────────────────────────── Parâmetros ───────────────────────────────

        grupo.MapGet("/parametros", async (
            IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
            Results.Ok(await consulta.ObterParametros(empresa.EmpresaId, ct)))
            .RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.VerParametros));

        grupo.MapPut("/parametros", async (
            ParametrosEntradaDto req, FinanceiroAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarParametros(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.EditarParametros));

        // ─────────────────────────────── Auditoria ───────────────────────────────

        grupo.MapGet("/auditoria", async (
            IFinanceiroConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? entidade, string? registroId, string? usuario,
            OperacaoAuditoria? operacao, DateTimeOffset? de, DateTimeOffset? ate,
            int? pagina, int? tamanho) =>
        {
            var filtro = new FiltroAuditoria(entidade, registroId, usuario, operacao, de, ate,
                pagina ?? 1, tamanho ?? 20);
            return Results.Ok(await consulta.ListarAuditoria(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesFinanceiro.VerAuditoria));

        return app;
    }
}
