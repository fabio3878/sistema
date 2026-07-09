using System.Security.Claims;
using System.Text;
using Acesso.Aplicacao;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Plataforma.Infraestrutura;

namespace Acesso.Infraestrutura;

/// <summary>
/// Emissão de access token JWT assinado em HS256 (uma máquina emite e valida). Claims batem com o
/// que <see cref="ContextoUsuarioHttp"/> lê. Permissões: <c>perm_all=true</c> para super-perfil,
/// senão uma claim <c>func</c> por código.
/// </summary>
public sealed class ServicoTokenJwt(IOptions<OpcoesJwt> opcoes) : IServicoToken
{
    private readonly OpcoesJwt _o = opcoes.Value;

    public TimeSpan DuracaoRefresh => TimeSpan.FromHours(_o.HorasRefreshToken);

    public TokenAcesso Emitir(DadosToken dados)
    {
        var agora = DateTimeOffset.UtcNow;
        var expira = agora.AddMinutes(_o.MinutosAccessToken);

        var claims = new List<Claim>
        {
            new(ContextoUsuarioHttp.ClaimSub, dados.UsuarioId),
            new(ContextoUsuarioHttp.ClaimEmpresa, dados.EmpresaId),
            new(ContextoUsuarioHttp.ClaimLogin, dados.Login),
            new(ContextoUsuarioHttp.ClaimStamp, dados.StampSeguranca),
        };

        if (dados.Permissoes.ConcedeTodas)
            claims.Add(new Claim(ContextoUsuarioHttp.ClaimPermAll, "true"));
        else
            claims.AddRange(dados.Permissoes.Codigos.Select(c => new Claim(ContextoUsuarioHttp.ClaimFunc, c)));

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.ChaveAssinatura));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _o.Emissor,
            Audience = _o.Audiencia,
            IssuedAt = agora.UtcDateTime,
            NotBefore = agora.UtcDateTime,
            Expires = expira.UtcDateTime,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(descriptor);
        return new TokenAcesso(token, expira);
    }
}
