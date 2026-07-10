using BuildingBlocks;

namespace Cadastros.Contratos;

/// <summary>
/// Catálogo, em código, das funcionalidades do módulo Cadastros (fonte da verdade). Convenção:
/// <c>&lt;modulo&gt;.&lt;recurso&gt;.&lt;acao&gt;</c>. Reconciliado para acs_funcionalidades no startup.
/// </summary>
public static class FuncionalidadesCadastro
{
    public const string ModuloCodigo = "cad";
    public const string ModuloNome = "Cadastros";

    public const string ListarCliente = "cad.cliente.listar";
    public const string CriarCliente = "cad.cliente.criar";
    public const string EditarCliente = "cad.cliente.editar";
    public const string CriarProduto = "cad.produto.criar";
    public const string EditarProduto = "cad.produto.editar";

    public static IEnumerable<FuncionalidadeManifesto> Manifesto() =>
    [
        new(ListarCliente, ModuloCodigo, ModuloNome, "Listar clientes", "Consultar a lista de clientes."),
        new(CriarCliente, ModuloCodigo, ModuloNome, "Criar cliente", "Cadastrar novos clientes."),
        new(EditarCliente, ModuloCodigo, ModuloNome, "Editar cliente", "Alterar dados de clientes."),
        new(CriarProduto, ModuloCodigo, ModuloNome, "Criar produto", "Cadastrar novos produtos."),
        new(EditarProduto, ModuloCodigo, ModuloNome, "Editar produto", "Alterar dados de produtos."),
    ];
}
