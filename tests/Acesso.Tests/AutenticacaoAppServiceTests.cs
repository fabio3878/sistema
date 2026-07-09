using Acesso.Aplicacao;
using Acesso.Contratos;
using Acesso.Dominio;
using Acesso.Infraestrutura;

namespace Acesso.Tests;

public class AutenticacaoAppServiceTests
{
    private static readonly IHashSenha Hash = new Pbkdf2HashSenha();

    private sealed record Cenario(
        AutenticacaoAppService Svc, UsuarioRepoFake Usuarios, PerfilRepoFake Perfis,
        RefreshRepoFake Refresh, Usuario Usuario);

    private static Cenario Montar(bool concedeTodas = true, string senha = "segredo123", bool ativo = true)
    {
        var usuarios = new UsuarioRepoFake();
        var perfis = new PerfilRepoFake();
        var refresh = new RefreshRepoFake();

        var perfil = Perfil.Criar(Fakes.Empresa, "Administrador", concedeTodas: concedeTodas, protegido: true).Valor!;
        if (!concedeTodas) perfil.Conceder("cad.cliente.criar");
        perfis.Adicionar(perfil);

        var usuario = Usuario.Criar(Fakes.Empresa, new DadosUsuario("admin", "Admin", senha), Hash).Valor!;
        usuario.AtribuirPerfil(perfil.Id);
        if (!ativo) usuario.Inativar();
        usuarios.Adicionar(usuario);

        var svc = new AutenticacaoAppService(usuarios, perfis, refresh, Hash, new ServicoTokenFake(), new UowFake());
        return new Cenario(svc, usuarios, perfis, refresh, usuario);
    }

    [Fact]
    public async Task Login_valido_emite_par_de_tokens()
    {
        var c = Montar();
        var r = await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "segredo123"));

        Assert.True(r.Sucesso);
        Assert.False(string.IsNullOrEmpty(r.Valor!.AccessToken));
        Assert.False(string.IsNullOrEmpty(r.Valor.RefreshToken));
        Assert.Single(c.Refresh.Tokens);
    }

    [Theory]
    [InlineData("admin", "senhaerrada")]   // senha errada
    [InlineData("naoexiste", "segredo123")] // login inexistente
    public async Task Login_invalido_retorna_falha_generica(string login, string senha)
    {
        var c = Montar();
        var r = await c.Svc.Login(Fakes.Empresa, new LoginRequest(login, senha));
        Assert.True(r.Falhou);
        Assert.Equal("Credenciais inválidas.", r.Erro);
    }

    [Fact]
    public async Task Login_usuario_inativo_falha_com_mesma_mensagem()
    {
        var c = Montar(ativo: false);
        var r = await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "segredo123"));
        Assert.True(r.Falhou);
        Assert.Equal("Credenciais inválidas.", r.Erro);
    }

    [Fact]
    public async Task Refresh_rotaciona_revogando_o_antigo()
    {
        var c = Montar();
        var login = (await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "segredo123"))).Valor!;

        var r = await c.Svc.Refresh(Fakes.Empresa, login.RefreshToken);

        Assert.True(r.Sucesso);
        Assert.Equal(2, c.Refresh.Tokens.Count);
        var antigo = c.Refresh.Tokens[0];
        Assert.NotNull(antigo.RevogadoEm);                 // rotacionado
        Assert.NotNull(antigo.SubstituidoPorId);
        Assert.NotEqual(login.RefreshToken, r.Valor!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_de_token_ja_revogado_revoga_todos()
    {
        var c = Montar();
        var login = (await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "segredo123"))).Valor!;
        await c.Svc.Refresh(Fakes.Empresa, login.RefreshToken); // usa e revoga o 1º

        // reusar o token já revogado dispara revoga-tudo
        var r = await c.Svc.Refresh(Fakes.Empresa, login.RefreshToken);

        Assert.True(r.Falhou);
        Assert.All(c.Refresh.Tokens, t => Assert.NotNull(t.RevogadoEm));
    }

    [Fact]
    public async Task TrocarSenha_invalida_refresh_antigo_por_mudanca_de_stamp()
    {
        var c = Montar();
        var login = (await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "segredo123"))).Valor!;

        var troca = await c.Svc.TrocarSenha(Fakes.Empresa, c.Usuario.Id,
            new TrocarSenhaRequest("segredo123", "novaSenha456"));
        Assert.True(troca.Sucesso);

        // o refresh emitido antes não vale mais (stamp mudou / tokens revogados)
        var r = await c.Svc.Refresh(Fakes.Empresa, login.RefreshToken);
        Assert.True(r.Falhou);

        // e a senha nova autentica
        var novoLogin = await c.Svc.Login(Fakes.Empresa, new LoginRequest("admin", "novaSenha456"));
        Assert.True(novoLogin.Sucesso);
    }
}
