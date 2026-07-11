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

    public const string ListarServico = "cad.servico.listar";
    public const string CriarServico = "cad.servico.criar";
    public const string EditarServico = "cad.servico.editar";

    /// <summary>Ver a trilha de auditoria dos cadastros (clientes, produtos, serviços).</summary>
    public const string VerAuditoria = "cad.auditoria.ver";

    public static IEnumerable<FuncionalidadeManifesto> Manifesto() =>
    [
        new(ListarCliente, ModuloCodigo, ModuloNome, "Listar clientes", "Consultar a lista de clientes."),
        new(CriarCliente, ModuloCodigo, ModuloNome, "Criar cliente", "Cadastrar novos clientes."),
        new(EditarCliente, ModuloCodigo, ModuloNome, "Editar cliente", "Alterar dados de clientes."),
        new(ListarServico, ModuloCodigo, ModuloNome, "Listar serviços", "Consultar a lista de serviços."),
        new(CriarServico, ModuloCodigo, ModuloNome, "Criar serviço", "Cadastrar novos serviços."),
        new(EditarServico, ModuloCodigo, ModuloNome, "Editar serviço", "Alterar dados de serviços."),
        new(VerAuditoria, ModuloCodigo, ModuloNome, "Ver auditoria", "Consultar a trilha de alterações dos cadastros."),
    ];
}

/// <summary>
/// Funcionalidades cujo GATING é do módulo Estoque (<c>est</c>), embora o cadastro de Produto
/// resida fisicamente em Cadastros (master data). Menu/tela/endpoints de Produto são liberados
/// pela licença + permissão do Estoque — é o módulo que consome o produto no dia a dia.
/// </summary>
public static class FuncionalidadesEstoque
{
    public const string ModuloCodigo = "est";
    public const string ModuloNome = "Estoque";

    public const string ListarProduto = "est.produto.listar";
    public const string CriarProduto = "est.produto.criar";
    public const string EditarProduto = "est.produto.editar";

    public static IEnumerable<FuncionalidadeManifesto> Manifesto() =>
    [
        new(ListarProduto, ModuloCodigo, ModuloNome, "Listar produtos", "Consultar a lista de produtos."),
        new(CriarProduto, ModuloCodigo, ModuloNome, "Criar produto", "Cadastrar novos produtos."),
        new(EditarProduto, ModuloCodigo, ModuloNome, "Editar produto", "Alterar dados de produtos."),
    ];
}
