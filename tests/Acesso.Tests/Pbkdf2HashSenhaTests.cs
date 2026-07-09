using Acesso.Dominio;
using Acesso.Infraestrutura;

namespace Acesso.Tests;

public class Pbkdf2HashSenhaTests
{
    private static readonly IHashSenha Hash = new Pbkdf2HashSenha();

    [Fact]
    public void Hash_depois_Verificar_confere_a_senha_correta()
    {
        var hash = Hash.Hash("s3nh@Forte");
        Assert.True(Hash.Verificar("s3nh@Forte", hash));
    }

    [Fact]
    public void Verificar_rejeita_senha_errada()
    {
        var hash = Hash.Hash("s3nh@Forte");
        Assert.False(Hash.Verificar("outraSenha", hash));
    }

    [Fact]
    public void Hash_do_mesmo_texto_gera_saidas_diferentes_pelo_salt()
    {
        Assert.NotEqual(Hash.Hash("igual"), Hash.Hash("igual"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("formato-invalido")]
    [InlineData("pbkdf2-sha256$abc$xx$yy")]
    public void Verificar_com_hash_malformado_retorna_false(string hashArmazenado)
    {
        Assert.False(Hash.Verificar("qualquer", hashArmazenado));
    }
}
