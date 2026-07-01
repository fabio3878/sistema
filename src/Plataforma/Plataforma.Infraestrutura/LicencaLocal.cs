using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Licença padrão de desenvolvimento: liga todos os módulos, frente PDV Varejo,
/// modo local-only. Em produção isto vira leitura de um arquivo/serviço de licença.
/// </summary>
public sealed class LicencaLocal : ILicenca
{
    public bool ModuloAtivo(string modulo) => true;

    public string FrenteAtiva => "PdvVarejo";

    public SyncMode Sync => SyncMode.LocalOnly;
}
