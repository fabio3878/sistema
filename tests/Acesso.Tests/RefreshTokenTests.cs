using Acesso.Dominio;

namespace Acesso.Tests;

public class RefreshTokenTests
{
    [Fact]
    public void Emitir_guarda_hash_e_devolve_bruto_diferente()
    {
        var (token, bruto) = RefreshToken.Emitir("EMPRESA_DEV", "USR1", "STAMP1", TimeSpan.FromHours(12));

        Assert.NotEqual(bruto, token.TokenHash);
        Assert.Equal(RefreshToken.Hash(bruto), token.TokenHash); // hash determinístico p/ lookup
        Assert.True(token.EstaAtivo(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Revogar_desativa_o_token()
    {
        var (token, _) = RefreshToken.Emitir("EMPRESA_DEV", "USR1", "STAMP1", TimeSpan.FromHours(12));
        token.Revogar("teste");
        Assert.False(token.EstaAtivo(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expirado_nao_esta_ativo()
    {
        var (token, _) = RefreshToken.Emitir("EMPRESA_DEV", "USR1", "STAMP1", TimeSpan.FromHours(-1));
        Assert.False(token.EstaAtivo(DateTimeOffset.UtcNow));
    }
}
