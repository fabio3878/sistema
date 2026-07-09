using BuildingBlocks;

namespace Acesso.Contratos;

/// <summary>
/// Catálogo, em código, das funcionalidades do próprio módulo Acesso. Convenção de código:
/// <c>&lt;modulo&gt;.&lt;recurso&gt;.&lt;acao&gt;</c>. Estas constantes são a fonte da verdade referenciada
/// tanto pela verificação de permissão quanto pelo manifesto reconciliado no startup.
/// </summary>
public static class FuncionalidadesAcesso
{
    public const string ModuloCodigo = "acs";
    public const string ModuloNome = "Acesso";

    public const string GerenciarUsuarios = "acs.usuario.gerenciar";
    public const string GerenciarPerfis = "acs.perfil.gerenciar";

    /// <summary>Manifesto do módulo (consumido pelo <see cref="IModulo.Funcionalidades"/>).</summary>
    public static IEnumerable<FuncionalidadeManifesto> Manifesto() =>
    [
        new(GerenciarUsuarios, ModuloCodigo, ModuloNome, "Gerenciar usuários",
            "Criar, editar e inativar usuários do sistema."),
        new(GerenciarPerfis, ModuloCodigo, ModuloNome, "Gerenciar perfis",
            "Criar perfis e conceder/revogar funcionalidades."),
    ];
}
