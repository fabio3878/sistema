using Cadastros.Dominio;

namespace Cadastros.Tests;

public class ProdutoTests
{
    [Fact]
    public void Criar_com_dados_validos_gera_produto()
    {
        var r = Produto.Criar("EMPRESA_DEV", "SKU-001", "Pão francês", "19059090", 0.75m, "7891234567890");

        Assert.True(r.Sucesso);
        var p = r.Valor!;
        Assert.Equal("SKU-001", p.Sku);
        Assert.Equal("19059090", p.Ncm);
        Assert.Equal(0.75m, p.PrecoVenda);
    }

    [Fact]
    public void Criar_com_ncm_invalido_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", "SKU-001", "Item", "123", 1m);
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_preco_negativo_retorna_falha()
    {
        var r = Produto.Criar("EMPRESA_DEV", "SKU-001", "Item", "19059090", -1m);
        Assert.True(r.Falhou);
    }

    [Fact]
    public void AlterarPreco_atualiza_valor_e_versao()
    {
        var p = Produto.Criar("EMPRESA_DEV", "SKU-001", "Item", "19059090", 10m).Valor!;
        var versaoAntes = p.Versao;

        p.AlterarPreco(12.5m);

        Assert.Equal(12.5m, p.PrecoVenda);
        Assert.Equal(versaoAntes + 1, p.Versao);
    }
}
