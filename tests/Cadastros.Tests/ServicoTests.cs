using Cadastros.Dominio;

namespace Cadastros.Tests;

public class ServicoTests
{
    private static DadosServico Dados(
        string descricao = "Instalação elétrica", decimal preco = 150m,
        string unidade = "HR", string? codigoInterno = "SERV-01") =>
        new(descricao, preco, unidade, codigoInterno);

    [Fact]
    public void Criar_com_dados_validos_gera_servico()
    {
        var r = Servico.Criar("EMPRESA_DEV", Dados());

        Assert.True(r.Sucesso);
        var s = r.Valor!;
        Assert.Equal("SERV-01", s.CodigoInterno);
        Assert.Equal("Instalação elétrica", s.Descricao);
        Assert.Equal("HR", s.Unidade);
        Assert.Equal(150m, s.PrecoVenda);
        Assert.True(s.Ativo);
    }

    [Fact]
    public void Criar_sem_codigo_interno_e_valido()
    {
        var r = Servico.Criar("EMPRESA_DEV", Dados(codigoInterno: null));
        Assert.True(r.Sucesso);
        Assert.Null(r.Valor!.CodigoInterno);
    }

    [Fact]
    public void Criar_sem_unidade_retorna_falha()
    {
        var r = Servico.Criar("EMPRESA_DEV", Dados(unidade: ""));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_sem_descricao_retorna_falha()
    {
        var r = Servico.Criar("EMPRESA_DEV", Dados(descricao: " "));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_preco_negativo_retorna_falha()
    {
        var r = Servico.Criar("EMPRESA_DEV", Dados(preco: -1m));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Inativar_e_ativar_alternam_situacao()
    {
        var s = Servico.Criar("EMPRESA_DEV", Dados()).Valor!;

        s.Inativar();
        Assert.False(s.Ativo);

        s.Ativar();
        Assert.True(s.Ativo);
    }
}
