using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Acesso.Infraestrutura;

/// <summary>Wiring de autenticação JWT do módulo Acesso (chamado pelo host).</summary>
public static class AutenticacaoServiceCollectionExtensions
{
    public static IServiceCollection AdicionarAutenticacaoAcesso(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpcoesJwt>(config.GetSection(OpcoesJwt.Secao));
        var opcoes = config.GetSection(OpcoesJwt.Secao).Get<OpcoesJwt>() ?? new OpcoesJwt();

        if (string.IsNullOrWhiteSpace(opcoes.ChaveAssinatura) || Encoding.UTF8.GetByteCount(opcoes.ChaveAssinatura) < 32)
            throw new InvalidOperationException(
                "Acesso:Jwt:ChaveAssinatura ausente ou curta (< 32 bytes). Configure via user-secrets/ambiente.");

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcoes.ChaveAssinatura));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false; // preserva os nomes de claim (sub, func, perm_all...)
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opcoes.Emissor,
                    ValidateAudience = true,
                    ValidAudience = opcoes.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = chave,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        return services;
    }
}
