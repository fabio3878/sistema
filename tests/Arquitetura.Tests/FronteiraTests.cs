using Cadastros.Contratos;
using Cadastros.Dominio;
using NetArchTest.Rules;

namespace Arquitetura.Tests;

/// <summary>
/// Impõe as fronteiras da seção 3 automaticamente: se alguém violar a regra de
/// referência entre camadas, o build de teste falha. É o guarda-corpo que deixa
/// mexer num módulo sem quebrar os outros por acidente.
/// </summary>
public class FronteiraTests
{
    [Fact]
    public void Dominio_nao_depende_de_infraestrutura_nem_de_aplicacao()
    {
        var resultado = Types.InAssembly(typeof(Pessoa).Assembly)
            .That().ResideInNamespace("Cadastros.Dominio")
            .ShouldNot().HaveDependencyOnAny(
                "Cadastros.Aplicacao",
                "Cadastros.Infraestrutura",
                "Plataforma.Aplicacao",
                "Plataforma.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Contratos_e_folha_depende_so_de_buildingblocks()
    {
        var resultado = Types.InAssembly(typeof(PessoaDto).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Cadastros.Dominio",
                "Cadastros.Aplicacao",
                "Cadastros.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    private static string Falhas(TestResult r) =>
        "Tipos que violam a fronteira: " + string.Join(", ", r.FailingTypeNames ?? []);
}
