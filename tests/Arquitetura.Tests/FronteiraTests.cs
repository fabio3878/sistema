using Acesso.Contratos;
using Acesso.Dominio;
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
        var resultado = Types.InAssembly(typeof(Cliente).Assembly)
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
        var resultado = Types.InAssembly(typeof(ClienteDto).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Cadastros.Dominio",
                "Cadastros.Aplicacao",
                "Cadastros.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Acesso_Dominio_nao_depende_de_infraestrutura_nem_de_aplicacao()
    {
        var resultado = Types.InAssembly(typeof(Usuario).Assembly)
            .That().ResideInNamespace("Acesso.Dominio")
            .ShouldNot().HaveDependencyOnAny(
                "Acesso.Aplicacao",
                "Acesso.Infraestrutura",
                "Plataforma.Aplicacao",
                "Plataforma.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Acesso_Contratos_e_folha_depende_so_de_buildingblocks()
    {
        var resultado = Types.InAssembly(typeof(UsuarioDto).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Acesso.Dominio",
                "Acesso.Aplicacao",
                "Acesso.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Acesso_Http_nao_depende_de_infra_nem_de_outros_modulos()
    {
        var resultado = Types.InAssembly(typeof(Acesso.Http.AcessoEndpoints).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Acesso.Infraestrutura",
                "Cadastros.Dominio",
                "Cadastros.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Cadastros_Http_nao_depende_de_infra_nem_de_outros_modulos()
    {
        var resultado = Types.InAssembly(typeof(Cadastros.Http.CadastrosEndpoints).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Cadastros.Infraestrutura",
                "Acesso.Dominio",
                "Acesso.Infraestrutura",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    [Fact]
    public void Plataforma_Dominio_nao_depende_de_aspnet_nem_de_ef()
    {
        var resultado = Types.InAssembly(typeof(Plataforma.Dominio.IContextoUsuario).Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(resultado.IsSuccessful, Falhas(resultado));
    }

    private static string Falhas(TestResult r) =>
        "Tipos que violam a fronteira: " + string.Join(", ", r.FailingTypeNames ?? []);
}
