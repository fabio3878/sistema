namespace Cadastros.Contratos;

/// <summary>Natureza do documento: pessoa física (CPF) ou jurídica (CNPJ). Derivada do documento.</summary>
public enum TipoPessoa
{
    Fisica,
    Juridica,
}

/// <summary>Finalidade do endereço de um cliente. Um cliente pode ter vários (seção 6.2).</summary>
public enum TipoEndereco
{
    Principal = 1,
    Cobranca = 2,
    Entrega = 3,
}

/// <summary>
/// Indicador da IE do destinatário (campo <c>indIEDest</c> da NF-e). Define se o cliente
/// contribui com ICMS — decide o preenchimento da Inscrição Estadual.
/// </summary>
public enum IndicadorIe
{
    Contribuinte = 1,
    Isento = 2,
    NaoContribuinte = 9,
}

/// <summary>
/// Regime tributário (CRT). Só faz sentido para pessoa jurídica; nulo para consumidor comum.
/// </summary>
public enum RegimeTributario
{
    SimplesNacional = 1,
    SimplesExcessoSublimite = 2,
    Normal = 3,
}
