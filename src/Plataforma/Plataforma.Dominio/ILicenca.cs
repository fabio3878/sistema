namespace Plataforma.Dominio;

/// <summary>
/// Licença do cliente (seção 8): liga módulos e UMA frente. É assim que 1 produto
/// vira N verticais — mesmo binário, comportamento por configuração.
/// </summary>
public interface ILicenca
{
    /// <summary>Um módulo está habilitado? Ex.: "Estoque", "Financeiro", "Fiscal".</summary>
    bool ModuloAtivo(string modulo);

    /// <summary>Frente ativa. Ex.: "PdvVarejo", "Restaurante", "OrdemServico".</summary>
    string FrenteAtiva { get; }

    /// <summary>Modo de sincronização deste cliente.</summary>
    SyncMode Sync { get; }
}
