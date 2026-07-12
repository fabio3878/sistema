namespace Financeiro.Contratos;

/// <summary>
/// De onde nasceu a Conta a Receber. <c>Manual</c> = lançada à mão (v1). <c>Venda</c> fica
/// reservado para quando o módulo de Vendas gerar títulos automaticamente (evento VendaFechada).
/// </summary>
public enum TipoOrigemConta
{
    Manual = 0,
    Venda = 1,
}

/// <summary>
/// Situação de uma parcela — <b>derivada</b> (nunca setada à mão), calculada na leitura a partir
/// do saldo + vencimento + marcadores. <c>Vencida</c> depende da data de hoje.
/// </summary>
public enum StatusParcela
{
    Aberta = 0,
    RecebidaParcial = 1,
    Recebida = 2,
    Vencida = 3,
    Cancelada = 4,
    Renegociada = 5,
}

/// <summary>
/// Situação geral de uma Conta a Receber — <b>derivada</b> da situação de suas parcelas
/// (nunca alterada manualmente).
/// </summary>
public enum SituacaoConta
{
    EmAberto = 0,
    ParcialmenteRecebida = 1,
    Quitada = 2,
    PossuiParcelasVencidas = 3,
    Cancelada = 4,
}
