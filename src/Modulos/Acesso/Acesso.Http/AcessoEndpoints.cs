using Acesso.Aplicacao;
using Acesso.Contratos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Plataforma.Dominio;

namespace Acesso.Http;

/// <summary>
/// Endpoints HTTP de autenticação/acesso (minimal API). Login/refresh são anônimos; logout e
/// troca de senha exigem autenticação; a listagem de usuários exige a funcionalidade correspondente.
/// O tenant vem do <see cref="IContextoEmpresa"/> (claim do token ou tenant do servidor).
/// </summary>
public static class AcessoEndpoints
{
    public static IEndpointRouteBuilder MapAcessoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/acesso");

        grupo.MapPost("/login", async (
            LoginRequest req, AutenticacaoAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.Login(empresa.EmpresaId, req, ct);
            return r.Sucesso ? Results.Ok(r.Valor) : Results.Problem(r.Erro, statusCode: StatusCodes.Status401Unauthorized);
        }).AllowAnonymous();

        grupo.MapPost("/refresh", async (
            RefreshRequest req, AutenticacaoAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            var r = await svc.Refresh(empresa.EmpresaId, req.RefreshToken, ct);
            return r.Sucesso ? Results.Ok(r.Valor) : Results.Problem(r.Erro, statusCode: StatusCodes.Status401Unauthorized);
        }).AllowAnonymous();

        grupo.MapPost("/logout", async (
            LogoutRequest req, AutenticacaoAppService svc, IContextoEmpresa empresa, CancellationToken ct) =>
        {
            await svc.Logout(empresa.EmpresaId, req.RefreshToken, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        grupo.MapPost("/trocar-senha", async (
            TrocarSenhaRequest req, AutenticacaoAppService svc,
            IContextoEmpresa empresa, IContextoUsuario usuario, CancellationToken ct) =>
        {
            if (!usuario.Autenticado || usuario.UsuarioId is null)
                return Results.Unauthorized();
            var r = await svc.TrocarSenha(empresa.EmpresaId, usuario.UsuarioId, req, ct);
            return r.Sucesso ? Results.NoContent() : Results.BadRequest(new { erro = r.Erro });
        }).RequireAuthorization();

        // Quem sou eu — endpoint protegido simples (qualquer autenticado).
        grupo.MapGet("/eu", (IContextoUsuario usuario) => Results.Ok(new
        {
            usuario.UsuarioId,
            usuario.Login,
            usuario.ConcedeTodas,
            Funcionalidades = usuario.Funcionalidades,
        })).RequireAuthorization();

        // Exemplo de endpoint guardado por funcionalidade (padrão para os módulos seguirem).
        grupo.MapGet("/usuarios", async (IAcessoConsulta consulta, IContextoEmpresa empresa, CancellationToken ct) =>
            Results.Ok(await consulta.ListarUsuarios(empresa.EmpresaId, ct)))
            .RequireAuthorization(PoliticaAcesso.Funcionalidade(FuncionalidadesAcesso.GerenciarUsuarios));

        return app;
    }
}
