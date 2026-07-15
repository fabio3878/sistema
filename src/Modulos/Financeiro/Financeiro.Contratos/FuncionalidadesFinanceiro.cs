using BuildingBlocks;

namespace Financeiro.Contratos;

/// <summary>
/// Catálogo, em código, das funcionalidades do módulo Financeiro (fonte da verdade). Convenção:
/// <c>&lt;modulo&gt;.&lt;recurso&gt;.&lt;acao&gt;</c>. Reconciliado para acs_funcionalidades no startup.
/// </summary>
public static class FuncionalidadesFinanceiro
{
    public const string ModuloCodigo = "fin";
    public const string ModuloNome = "Financeiro";

    public const string ListarContaReceber = "fin.contareceber.listar";
    public const string CriarContaReceber = "fin.contareceber.criar";
    public const string EditarContaReceber = "fin.contareceber.editar";
    public const string CancelarContaReceber = "fin.contareceber.cancelar";
    public const string RenegociarContaReceber = "fin.contareceber.renegociar";

    public const string RegistrarRecebimento = "fin.recebimento.registrar";
    public const string EstornarRecebimento = "fin.recebimento.estornar";

    public const string ListarFormaPagamento = "fin.formapagamento.listar";
    public const string CriarFormaPagamento = "fin.formapagamento.criar";
    public const string EditarFormaPagamento = "fin.formapagamento.editar";

    public const string VerParametros = "fin.parametros.ver";
    public const string EditarParametros = "fin.parametros.editar";

    /// <summary>Ver a trilha de auditoria do financeiro (contas, parcelas, recebimentos).</summary>
    public const string VerAuditoria = "fin.auditoria.ver";

    public static IEnumerable<FuncionalidadeManifesto> Manifesto() =>
    [
        new(ListarContaReceber, ModuloCodigo, ModuloNome, "Listar contas a receber", "Consultar contas a receber e suas parcelas."),
        new(CriarContaReceber, ModuloCodigo, ModuloNome, "Criar conta a receber", "Lançar novas contas a receber."),
        new(EditarContaReceber, ModuloCodigo, ModuloNome, "Editar conta a receber", "Alterar dados de contas a receber e parcelas."),
        new(CancelarContaReceber, ModuloCodigo, ModuloNome, "Cancelar conta a receber", "Cancelar contas a receber em aberto."),
        new(RenegociarContaReceber, ModuloCodigo, ModuloNome, "Renegociar conta a receber", "Renegociar parcelas em aberto gerando um novo plano."),
        new(RegistrarRecebimento, ModuloCodigo, ModuloNome, "Registrar recebimento", "Dar baixa (total ou parcial) em uma parcela."),
        new(EstornarRecebimento, ModuloCodigo, ModuloNome, "Estornar recebimento", "Estornar um recebimento já lançado."),
        new(ListarFormaPagamento, ModuloCodigo, ModuloNome, "Listar formas de pagamento", "Consultar as formas de pagamento."),
        new(CriarFormaPagamento, ModuloCodigo, ModuloNome, "Criar forma de pagamento", "Cadastrar novas formas de pagamento."),
        new(EditarFormaPagamento, ModuloCodigo, ModuloNome, "Editar forma de pagamento", "Alterar formas de pagamento."),
        new(VerParametros, ModuloCodigo, ModuloNome, "Ver parâmetros financeiros", "Consultar juros de mora e multa padrão."),
        new(EditarParametros, ModuloCodigo, ModuloNome, "Editar parâmetros financeiros", "Alterar juros de mora e multa padrão."),
        new(VerAuditoria, ModuloCodigo, ModuloNome, "Ver auditoria", "Consultar a trilha de alterações do financeiro."),
    ];
}
