using System.Text;
using Acesso.Aplicacao;
using Acesso.Infraestrutura;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Acesso.Tests;

public class ServicoTokenJwtTests
{
    private const string Chave = "chave-de-teste-bem-comprida-com-mais-de-32-bytes-1234567890";

    private static ServicoTokenJwt Criar() => new(Options.Create(new OpcoesJwt
    {
        ChaveAssinatura = Chave,
        Emissor = "AgenteLocal",
        Audiencia = "AutomacaoComercial",
        MinutosAccessToken = 15,
        HorasRefreshToken = 12,
    }));

    [Fact]
    public void Emitir_com_ConcedeTodas_poe_claim_perm_all()
    {
        var svc = Criar();
        var t = svc.Emitir(new DadosToken("USR1", "EMPRESA_DEV", "admin", "STAMP1",
            new ConjuntoPermissoes(true, new HashSet<string>())));

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(t.Token);
        Assert.Equal("USR1", jwt.GetClaim("sub").Value);
        Assert.Equal("EMPRESA_DEV", jwt.GetClaim("empresa").Value);
        Assert.Equal("true", jwt.GetClaim("perm_all").Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "func");
    }

    [Fact]
    public void Emitir_com_funcionalidades_poe_uma_claim_func_por_codigo()
    {
        var svc = Criar();
        var codigos = new HashSet<string> { "cad.cliente.criar", "cad.produto.editar" };
        var t = svc.Emitir(new DadosToken("USR1", "EMPRESA_DEV", "op", "STAMP1",
            new ConjuntoPermissoes(false, codigos)));

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(t.Token);
        var func = jwt.Claims.Where(c => c.Type == "func").Select(c => c.Value).ToHashSet();
        Assert.Equal(codigos, func);
    }

    [Fact]
    public async Task Token_emitido_valida_com_a_chave_correta_e_falha_com_a_errada()
    {
        var svc = Criar();
        var t = svc.Emitir(new DadosToken("USR1", "EMPRESA_DEV", "admin", "STAMP1",
            new ConjuntoPermissoes(true, new HashSet<string>())));

        var handler = new JsonWebTokenHandler();
        var okParams = new TokenValidationParameters
        {
            ValidIssuer = "AgenteLocal",
            ValidAudience = "AutomacaoComercial",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave)),
        };
        Assert.True((await handler.ValidateTokenAsync(t.Token, okParams)).IsValid);

        var badParams = new TokenValidationParameters
        {
            ValidIssuer = "AgenteLocal",
            ValidAudience = "AutomacaoComercial",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave + "-outra-chave-diferente")),
        };
        Assert.False((await handler.ValidateTokenAsync(t.Token, badParams)).IsValid);
    }
}
