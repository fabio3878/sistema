using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Tests;

public class ProdutoTests
{
    private static DadosProduto Dados(
        string descricao = "Pão francês", string ncm = "19059090", decimal preco = 0.75m,
        string unidade = "KG", string? cest = null, string? codigoInterno = "SKU-001") =>
        new(descricao, ncm, preco, unidade, OrigemMercadoria.Nacional, codigoInterno, "7891234567890", cest);

    [Fact]
    public void Criar_com_dados_validos_gera_produto()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados());

        Assert.True(r.Sucesso);
        var p = r.Valor!;
        Assert.Equal("SKU-001", p.CodigoInterno);
        Assert.Equal("19059090", p.Ncm);
        Assert.Equal("KG", p.Unidade);
        Assert.Equal(0.75m, p.PrecoVenda);
        Assert.True(p.Ativo);
    }

    [Fact]
    public void Criar_sem_codigo_interno_e_valido()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(codigoInterno: null));
        Assert.True(r.Sucesso);
        Assert.Null(r.Valor!.CodigoInterno);
    }

    [Fact]
    public void Criar_com_ncm_invalido_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(ncm: "123"));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_preco_negativo_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(preco: -1m));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_sem_unidade_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(unidade: ""));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_cest_invalido_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(cest: "123"));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_cest_valido_normaliza_digitos()
    {
        var r = Produto.Criar("EMPRESA_DEV", Dados(cest: "01.038.00"));
        Assert.True(r.Sucesso);
        Assert.Equal("0103800", r.Valor!.Cest);
    }

    [Fact]
    public void AlterarPreco_atualiza_valor_e_versao()
    {
        var p = Produto.Criar("EMPRESA_DEV", Dados(preco: 10m)).Valor!;
        var versaoAntes = p.Versao;

        p.AlterarPreco(12.5m);

        Assert.Equal(12.5m, p.PrecoVenda);
        Assert.Equal(versaoAntes + 1, p.Versao);
    }

    [Fact]
    public void Inativar_e_ativar_alternam_situacao()
    {
        var p = Produto.Criar("EMPRESA_DEV", Dados()).Valor!;

        p.Inativar();
        Assert.False(p.Ativo);

        p.Ativar();
        Assert.True(p.Ativo);
    }

    [Fact]
    public void Atualizar_troca_dados_e_incrementa_versao()
    {
        var p = Produto.Criar("EMPRESA_DEV", Dados()).Valor!;
        var versaoAntes = p.Versao;

        var r = p.Atualizar(Dados(descricao: "Pão de forma"));

        Assert.True(r.Sucesso);
        Assert.Equal("Pão de forma", p.Descricao);
        Assert.Equal(versaoAntes + 1, p.Versao);
    }
}
