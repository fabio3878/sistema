using Acesso.Dominio;
using Acesso.Infraestrutura;

namespace Acesso.Tests;

public class UsuarioTests
{
    private static readonly IHashSenha Hash = new Pbkdf2HashSenha();

    [Fact]
    public void Criar_valido_normaliza_login_gera_hash_e_ulid()
    {
        var r = Usuario.Criar("EMPRESA_DEV",
            new DadosUsuario("  Fulano.ADM ", "Fulano", "segredo123"), Hash);

        Assert.True(r.Sucesso);
        var u = r.Valor!;
        Assert.Equal(26, u.Id.Length);                 // PK = ULID
        Assert.Equal("Fulano.ADM", u.Login);           // trim, preserva caixa original
        Assert.Equal("fulano.adm", u.LoginNormalizado); // lookup em minúsculas
        Assert.True(u.Ativo);
        Assert.False(u.Excluido);
        Assert.NotEqual("segredo123", u.SenhaHash);     // nunca guarda em claro
        Assert.False(string.IsNullOrWhiteSpace(u.StampSeguranca));
        Assert.True(u.DeveTrocarSenha);
    }

    [Theory]
    [InlineData("ab", "Nome", "segredo123", "login curto")]
    [InlineData("fulano", "", "segredo123", "nome vazio")]
    [InlineData("fulano", "Nome", "123", "senha curta")]
    [InlineData("fulano", "Nome", "segredo123", "email invalido", "semarroba")]
    public void Criar_invalido_falha(string login, string nome, string senha, string _, string? email = null)
    {
        var r = Usuario.Criar("EMPRESA_DEV", new DadosUsuario(login, nome, senha, Email: email), Hash);
        Assert.True(r.Falhou);
        Assert.NotNull(r.Erro);
    }

    [Fact]
    public void AtribuirPerfil_e_idempotente_e_RemoverPerfil_faz_soft_delete()
    {
        var u = Usuario.Criar("EMPRESA_DEV", new DadosUsuario("fulano", "Fulano", "segredo123"), Hash).Valor!;

        u.AtribuirPerfil("PERFIL_1");
        u.AtribuirPerfil("PERFIL_1"); // repetido não duplica
        Assert.Single(u.Perfis);

        u.RemoverPerfil("PERFIL_1");
        Assert.True(u.Perfis.First().Excluido); // tombstone, não some da lista carregada
    }

    [Fact]
    public void AlterarSenha_troca_hash_limpa_flag_e_rotaciona_stamp()
    {
        var u = Usuario.Criar("EMPRESA_DEV", new DadosUsuario("fulano", "Fulano", "segredo123"), Hash).Valor!;
        var stampAntes = u.StampSeguranca;

        u.AlterarSenha(Hash.Hash("novaSenha456"));

        Assert.False(u.DeveTrocarSenha);
        Assert.NotEqual(stampAntes, u.StampSeguranca);       // invalida tokens antigos
        Assert.True(Hash.Verificar("novaSenha456", u.SenhaHash));
    }

    [Fact]
    public void Inativar_bloqueia_e_muda_stamp_de_seguranca()
    {
        var u = Usuario.Criar("EMPRESA_DEV", new DadosUsuario("fulano", "Fulano", "segredo123"), Hash).Valor!;
        var stampAntes = u.StampSeguranca;

        u.Inativar();

        Assert.False(u.Ativo);
        Assert.NotEqual(stampAntes, u.StampSeguranca);
    }
}
