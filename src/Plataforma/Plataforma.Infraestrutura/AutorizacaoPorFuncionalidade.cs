using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>Requisito de autorização: exige uma funcionalidade (código) do usuário corrente.</summary>
public sealed class FuncionalidadeRequirement(string codigo) : IAuthorizationRequirement
{
    public string Codigo { get; } = codigo;
}

/// <summary>Resolve o requisito consultando <see cref="IContextoUsuario.Pode"/> (claims do token).</summary>
public sealed class FuncionalidadeHandler(IContextoUsuario contexto)
    : AuthorizationHandler<FuncionalidadeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, FuncionalidadeRequirement requirement)
    {
        if (contexto.Pode(requirement.Codigo))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Materializa políticas <c>func:&lt;codigo&gt;</c> sob demanda (evita registrar uma política por
/// funcionalidade do catálogo). Endpoints usam <c>.RequireAuthorization("func:cad.cliente.criar")</c>.
/// </summary>
public sealed class FuncionalidadePolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PoliticaAcesso.PrefixoFuncionalidade, StringComparison.Ordinal))
        {
            var codigo = policyName[PoliticaAcesso.PrefixoFuncionalidade.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new FuncionalidadeRequirement(codigo))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
