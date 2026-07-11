using BuildingBlocks;
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

        // Unidades de medida — referência global (não-tenant): basta estar autenticado (combobox do produto).
        grupo.MapGet("/unidades", async (ICadastrosConsulta consulta, CancellationToken ct) =>
            Results.Ok(await consulta.ListarUnidades(ct))).RequireAuthorization();

        // Produtos — gating pelo módulo Estoque (est.produto.*), embora o cadastro resida em Cadastros.
        grupo.MapGet("/produtos", async (
            ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? busca, string? situacao) =>
        {
            var filtro = new FiltroProdutos(busca, SituacaoParaAtivo(situacao));
            return Results.Ok(await consulta.ListarProdutos(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.ListarProduto));

        grupo.MapGet("/produtos/{id}", async (
            string id, ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var produto = await consulta.ObterProduto(empresa.EmpresaId, id, ct);
            return produto is null ? Results.NotFound() : Results.Ok(produto);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.ListarProduto));

        grupo.MapPost("/produtos", async (
            ProdutoEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.CriarProduto(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.CriarProduto));

        grupo.MapPut("/produtos/{id}", async (
            string id, ProdutoEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarProduto(empresa.EmpresaId, id, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.EditarProduto));

        grupo.MapPost("/produtos/{id}/ativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoProduto(empresa.EmpresaId, id, ativo: true, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.EditarProduto));

        grupo.MapPost("/produtos/{id}/inativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoProduto(empresa.EmpresaId, id, ativo: false, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesEstoque.EditarProduto));

        // Serviços — cadastro próprio, gating pelo módulo Cadastros (cad.servico.*).
        grupo.MapGet("/servicos", async (
            ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? busca, string? situacao) =>
        {
            var filtro = new FiltroServicos(busca, SituacaoParaAtivo(situacao));
            return Results.Ok(await consulta.ListarServicos(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.ListarServico));

        grupo.MapGet("/servicos/{id}", async (
            string id, ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var servico = await consulta.ObterServico(empresa.EmpresaId, id, ct);
            return servico is null ? Results.NotFound() : Results.Ok(servico);
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.ListarServico));

        grupo.MapPost("/servicos", async (
            ServicoEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.CriarServico(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.Ok(new { id = r.Valor }) : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.CriarServico));

        grupo.MapPut("/servicos/{id}", async (
            string id, ServicoEntradaDto req, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AtualizarServico(empresa.EmpresaId, id, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarServico));

        grupo.MapPost("/servicos/{id}/ativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoServico(empresa.EmpresaId, id, ativo: true, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarServico));

        grupo.MapPost("/servicos/{id}/inativar", async (
            string id, CadastrosAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.AlterarSituacaoServico(empresa.EmpresaId, id, ativo: false, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.EditarServico));

        // Auditoria — trilha "quem alterou o quê" dos cadastros (paginada), gating cad.auditoria.ver.
        grupo.MapGet("/auditoria", async (
            ICadastrosConsulta consulta, IContextoEmpresa empresa, CancellationToken ct,
            string? entidade, string? registroId, string? usuario,
            OperacaoAuditoria? operacao, DateTimeOffset? de, DateTimeOffset? ate,
            int? pagina, int? tamanho) =>
        {
            var filtro = new FiltroAuditoria(entidade, registroId, usuario, operacao, de, ate,
                pagina ?? 1, tamanho ?? 20);
            return Results.Ok(await consulta.ListarAuditoria(empresa.EmpresaId, filtro, ct));
        }).RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesCadastro.VerAuditoria));

        return app;
    }

    /// <summary>Converte o filtro de situação da query ("ativo"/"inativo") em bool? (nulo = todos).</summary>
    private static bool? SituacaoParaAtivo(string? situacao) => situacao?.ToLowerInvariant() switch
    {
        "ativo" => true,
        "inativo" => false,
        _ => null,
    };
}
