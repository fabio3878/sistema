using Acesso.Dominio;

namespace Acesso.Tests;

public class PerfilTests
{
    [Fact]
    public void Conceder_e_Revogar_ajustam_a_permissao_efetiva()
    {
        var p = Perfil.Criar("EMPRESA_DEV", "Caixa").Valor!;

        Assert.False(p.Concede("cad.cliente.criar"));

        p.Conceder("cad.cliente.criar");
        Assert.True(p.Concede("cad.cliente.criar"));

        p.Conceder("cad.cliente.criar"); // idempotente
        Assert.Single(p.Funcionalidades);

        p.Revogar("cad.cliente.criar");
        Assert.False(p.Concede("cad.cliente.criar")); // soft delete tira do efetivo
    }

    [Fact]
    public void ConcedeTodas_concede_qualquer_funcionalidade_sem_listar()
    {
        var p = Perfil.Criar("EMPRESA_DEV", "Administrador", concedeTodas: true, protegido: true).Valor!;

        Assert.True(p.Concede("qualquer.coisa.nova"));
        Assert.True(p.Concede("acs.usuario.gerenciar"));
        Assert.Empty(p.Funcionalidades); // não precisa de linhas de concessão
    }

    [Fact]
    public void Criar_sem_nome_falha()
    {
        var r = Perfil.Criar("EMPRESA_DEV", "");
        Assert.True(r.Falhou);
    }
}
