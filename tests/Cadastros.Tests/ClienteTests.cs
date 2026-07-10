using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Tests;

public class ClienteTests
{
    [Fact]
    public void Criar_com_cpf_valido_gera_cliente_pessoa_fisica_com_ulid()
    {
        var r = Cliente.Criar("EMPRESA_DEV", new DadosCliente("João da Silva", "529.982.247-25"));

        Assert.True(r.Sucesso);
        var cliente = r.Valor!;
        Assert.Equal(26, cliente.Id.Length);            // PK = ULID (seção 4.1)
        Assert.Equal("52998224725", cliente.Documento); // só dígitos
        Assert.Equal(TipoPessoa.Fisica, cliente.TipoPessoa);
        Assert.True(cliente.Ativo);                      // nasce ativo
        Assert.False(cliente.Excluido);
    }

    [Fact]
    public void Criar_com_cnpj_valido_gera_pessoa_juridica()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Mercado Ltda", "11.222.333/0001-81", TipoPessoa.Juridica));

        Assert.True(r.Sucesso);
        Assert.Equal(TipoPessoa.Juridica, r.Valor!.TipoPessoa);
    }

    [Theory]
    [InlineData("", "52998224725", "Nome vazio falha")]
    [InlineData("Fulano", "123", "Documento curto falha")]
    [InlineData("Fulano", "12345678901", "CPF com dígito verificador inválido")]
    public void Criar_com_dados_invalidos_retorna_falha(string nome, string doc, string _)
    {
        var r = Cliente.Criar("EMPRESA_DEV", new DadosCliente(nome, doc));
        Assert.True(r.Falhou);
        Assert.NotNull(r.Erro);
    }

    [Fact]
    public void Criar_pf_com_cnpj_falha()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Fulano", "11222333000181", TipoPessoa.Fisica));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_pj_com_cpf_falha()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Mercado Ltda", "52998224725", TipoPessoa.Juridica));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_com_cnpj_invalido_falha()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Mercado Ltda", "11222333000100", TipoPessoa.Juridica));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_contribuinte_sem_inscricao_estadual_falha()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Mercado Ltda", "11222333000181", TipoPessoa.Juridica, IndicadorIe.Contribuinte));
        Assert.True(r.Falhou);
    }

    [Fact]
    public void Criar_contribuinte_com_inscricao_estadual_ok()
    {
        var r = Cliente.Criar("EMPRESA_DEV",
            new DadosCliente("Mercado Ltda", "11222333000181", TipoPessoa.Juridica, IndicadorIe.Contribuinte,
                InscricaoEstadual: "123456789"));
        Assert.True(r.Sucesso);
    }

    [Fact]
    public void Inativar_marca_inativo_e_sobe_versao()
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;
        var versaoAntes = cliente.Versao;

        cliente.Inativar();

        Assert.False(cliente.Ativo);
        Assert.Equal(versaoAntes + 1, cliente.Versao);
    }

    [Fact]
    public void AdicionarEndereco_valido_entra_no_agregado()
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;

        var r = cliente.AdicionarEndereco(new DadosEndereco(
            TipoEndereco.Principal, "01001-000", "Praça da Sé", "100", "Sé", "São Paulo", "SP", "3550308"));

        Assert.True(r.Sucesso);
        Assert.Single(cliente.Enderecos);
        Assert.Equal("01001000", cliente.Enderecos.First().Cep); // só dígitos
    }

    [Theory]
    [InlineData("123", "SP", "3550308", "CEP curto")]
    [InlineData("01001000", "S", "3550308", "UF inválida")]
    [InlineData("01001000", "SP", "12", "IBGE inválido")]
    public void AdicionarEndereco_invalido_falha(string cep, string uf, string ibge, string _)
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;

        var r = cliente.AdicionarEndereco(new DadosEndereco(
            TipoEndereco.Principal, cep, "Rua X", "1", "Centro", "São Paulo", uf, ibge));

        Assert.True(r.Falhou);
        Assert.Empty(cliente.Enderecos);
    }

    [Fact]
    public void Atualizar_altera_dados_e_sobe_versao()
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;
        var versaoAntes = cliente.Versao;

        var r = cliente.Atualizar(new DadosCliente("Fulano Renomeado", "52998224725", Email: "f@ex.com"));

        Assert.True(r.Sucesso);
        Assert.Equal("Fulano Renomeado", cliente.Nome);
        Assert.Equal("f@ex.com", cliente.Email);
        Assert.Equal(versaoAntes + 1, cliente.Versao);
    }

    [Fact]
    public void Atualizar_com_dados_invalidos_nao_muta()
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;

        var r = cliente.Atualizar(new DadosCliente("", "52998224725"));

        Assert.True(r.Falhou);
        Assert.Equal("Fulano", cliente.Nome); // inalterado
    }

    [Fact]
    public void SincronizarEnderecos_edita_mantido_e_soft_delete_ausente()
    {
        var cliente = Cliente.Criar("EMPRESA_DEV", new DadosCliente("Fulano", "52998224725")).Valor!;
        cliente.AdicionarEndereco(new DadosEndereco(
            TipoEndereco.Principal, "01001000", "Praça da Sé", "100", "Sé", "São Paulo", "SP", "3550308"));
        cliente.AdicionarEndereco(new DadosEndereco(
            TipoEndereco.Entrega, "02002000", "Rua B", "200", "Bairro B", "São Paulo", "SP", "3550308"));
        var principal = cliente.Enderecos.First(e => e.Tipo == TipoEndereco.Principal);

        // Mantém e edita o "Principal"; omite o "Entrega" (deve virar soft delete); adiciona um novo.
        var r = cliente.SincronizarEnderecos(
        [
            new EnderecoSync(principal.Id, new DadosEndereco(
                TipoEndereco.Principal, "01001000", "Praça da Sé", "999", "Sé", "São Paulo", "SP", "3550308")),
            new EnderecoSync(null, new DadosEndereco(
                TipoEndereco.Cobranca, "03003000", "Rua C", "300", "Bairro C", "São Paulo", "SP", "3550308")),
        ]);

        Assert.True(r.Sucesso);
        // Coleção guarda o tombstone (Excluido) — nunca remoção física.
        var ativos = cliente.Enderecos.Where(e => !e.Excluido).ToList();
        Assert.Equal(2, ativos.Count);
        Assert.Contains(ativos, e => e.Tipo == TipoEndereco.Cobranca);
        Assert.Equal("999", ativos.First(e => e.Id == principal.Id).Numero); // editado
        Assert.Contains(cliente.Enderecos, e => e.Tipo == TipoEndereco.Entrega && e.Excluido); // soft delete
    }
}
