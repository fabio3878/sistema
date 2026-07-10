using Cadastros.Aplicacao;
using Cadastros.Contratos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Plataforma.Dominio;

namespace Cadastros.Http;

/// <summary>
/// Endpoints HTTP do módulo Cadastros (minimal API). Todos exigem autenticação + a funcionalidade
/// correspondente (<c>cad.cliente.*</c>). O tenant vem SEMPRE do <see cref="IContextoEmpresa"/>
/// (claim do token), nunca do corpo da requisição. Falha de domínio (<c>Result</c>) vira 400 com
/// <c>{ erro }</c> — que o front lê como mensagem.
/// </summary>
public static class CadastrosEndpoints
{
    public static IEndpointRouteBuilder MapCadastrosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/cad");

        grupo.MapGet("/clientes", async (
            ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? busca, string? cidade, string? bairro, string? situacao, int? mes) =>
        {
            bool? ativo = situacao?.ToLowerInvariant() switch
            {
                "ativo" => true,
                "inativo" => false,
                _ => null,
            };
            var filtro = new FiltroClientes(busca, cidade, bairro, ativo, mes);
            return Results.Ok(await consulta.ListarClientes(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.ListarCliente));

        // Localidades IBGE — dado de referência global (não-tenant): basta estar autenticado.
        grupo.MapGet("/localidades/estados", async (ICadastrosConsulta consulta, CancellationToken ct) =>
            Results.Ok(await consulta.ListarEstados(ct))).RequireAuthorization();

        grupo.MapGet("/localidades/municipios", async (
            ICadastrosConsulta consulta, string uf, CancellationToken ct) =>
            Results.Ok(await consulta.ListarMunicipios(uf, ct))).RequireAuthorization();

        grupo.MapGet("/clientes/{id}", async (
            string id, ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var cliente = await consulta.ObterCliente(empresa.EmpresaId, id, ct);
            return cliente is null ? Results.NotFound() : Results.Ok(cliente);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.ListarCliente));

        grupo.MapPost("/clientes", async (
            ClienteEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.CriarCliente(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.CriarCliente));

        grupo.MapPut("/clientes/{id}", async (
            string id, ClienteEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarCliente(empresa.EmpresaId, id, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarCliente));

        grupo.MapPost("/clientes/{id}/ativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoCliente(empresa.EmpresaId, id, ativo: true, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarCliente));

        grupo.MapPost("/clientes/{id}/inativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoCliente(empresa.EmpresaId, id, ativo: false, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarCliente));

        return app;
    }
}
