namespace Acesso.Infraestrutura;

/// <summary>
/// Configuração do JWT (seção <c>Acesso:Jwt</c>). Tempos são configuráveis; a chave de assinatura
/// NUNCA é versionada — vem de user-secrets/variável de ambiente.
/// </summary>
public sealed class OpcoesJwt
{
    public const string Secao = "Acesso:Jwt";

    /// <summary>Chave HMAC-SHA256 (≥ 32 bytes). Obrigatória em runtime; só em secret/env.</summary>
    public string ChaveAssinatura { get; set; } = "";
    public string Emissor { get; set; } = "AgenteLocal";
    public string Audiencia { get; set; } = "AutomacaoComercial";
    public int MinutosAccessToken { get; set; } = 15;
    public int HorasRefreshToken { get; set; } = 12;
}
