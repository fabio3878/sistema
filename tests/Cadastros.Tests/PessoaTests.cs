using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Tests;

public class PessoaTests
{
    [Fact]
    public void Criar_com_dados_validos_gera_pessoa_com_ulid_e_papel()
    {
        var r = Pessoa.Criar("EMPRESA_DEV", "João da Silva", "529.982.247-25", PapelPessoa.Cliente);

        Assert.True(r.Sucesso);
        var pessoa = r.Valor!;
        Assert.Equal(26, pessoa.Id.Length);              // PK = ULID (seção 4.1)
        Assert.Equal("52998224725", pessoa.Documento);   // só dígitos
        Assert.True(pessoa.TemPapel(PapelPessoa.Cliente));
        Assert.False(pessoa.Excluido);
    }

    [Theory]
    [InlineData("", "12345678901", "Nome vazio falha")]
    [InlineData("Fulano", "123", "Documento curto falha")]
    public void Criar_com_dados_invalidos_retorna_falha(string nome, string doc, string _)
    {
        var r = Pessoa.Criar("EMPRESA_DEV", nome, doc, PapelPessoa.Cliente);
        Assert.True(r.Falhou);
        Assert.NotNull(r.Erro);
    }

    [Fact]
    public void Criar_sem_papel_retorna_falha()
    {
        var r = Pessoa.Criar("EMPRESA_DEV", "Fulano", "52998224725", PapelPessoa.Nenhum);
        Assert.True(r.Falhou);
    }

    [Fact]
    public void AdicionarPapel_acumula_papeis_e_sobe_versao()
    {
        var pessoa = Pessoa.Criar("EMPRESA_DEV", "Fulano", "52998224725", PapelPessoa.Cliente).Valor!;
        var versaoAntes = pessoa.Versao;

        pessoa.AdicionarPapel(PapelPessoa.Fornecedor);

        Assert.True(pessoa.TemPapel(PapelPessoa.Cliente));
        Assert.True(pessoa.TemPapel(PapelPessoa.Fornecedor));
        Assert.Equal(versaoAntes + 1, pessoa.Versao);
    }
}
