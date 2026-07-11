using System.Security.Cryptography;
using BuildingBlocks;

namespace Acesso.Dominio;

/// <summary>
/// Refresh token (dado de tenant, herda <see cref="EntidadeBase"/>). O access token JWT é curto e
/// stateless; a renovação/revogação passa por AQUI. O valor bruto (256 bits) só existe no momento da
/// emissão e é entregue ao cliente; no banco guarda-se só o SHA-256 (alta entropia ⇒ hash simples
/// basta, não precisa de KDF). Vinculado ao <see cref="StampSeguranca"/> do usuário no momento da emissão.
/// </summary>
public sealed class RefreshToken : EntidadeBase
{
    public string UsuarioId { get; private set; } = default!;

    /// <summary>Snapshot do StampSeguranca do usuário na emissão; se o stamp mudar (troca de senha /
    /// inativação), este token deixa de ser aceito.</summary>
    public string StampSeguranca { get; private set; } = default!;

    [NaoAuditar] // segredo: fora da trilha de auditoria
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? RevogadoEm { get; private set; }

    /// <summary>Id do token que substituiu este na rotação (trilha de rotação/detecção de reuso).</summary>
    public string? SubstituidoPorId { get; private set; }
    public string? MotivoRevogacao { get; private set; }

    // Construtor para o EF Core materializar.
    private RefreshToken() { }

    /// <summary>Emite um novo refresh token. Retorna a entidade (com o hash) e o valor BRUTO
    /// (mostrado só uma vez, entregue ao cliente).</summary>
    public static (RefreshToken token, string bruto) Emitir(
        string empresaId, string usuarioId, string stampSeguranca, TimeSpan vida)
    {
        var bruto = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var token = new RefreshToken
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            StampSeguranca = stampSeguranca,
            TokenHash = Hash(bruto),
            ExpiraEm = DateTimeOffset.UtcNow.Add(vida),
        };
        return (token, bruto);
    }

    /// <summary>Hash determinístico (SHA-256, Base64) para lookup do token bruto.</summary>
    public static string Hash(string bruto) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(bruto)));

    public bool EstaAtivo(DateTimeOffset agora) =>
        !Excluido && RevogadoEm is null && ExpiraEm > agora;

    public void Revogar(string motivo, string? substituidoPorId = null)
    {
        if (RevogadoEm is not null) return;
        RevogadoEm = DateTimeOffset.UtcNow;
        MotivoRevogacao = motivo;
        SubstituidoPorId = substituidoPorId;
        MarcarAtualizado();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
