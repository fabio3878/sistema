using System.Security.Cryptography;
using Acesso.Dominio;

namespace Acesso.Infraestrutura;

/// <summary>
/// Implementação de <see cref="IHashSenha"/> com PBKDF2-HMAC-SHA256 da BCL (sem dependência externa).
/// Formato armazenado (PHC-like, uma coluna): <c>pbkdf2-sha256$&lt;iteracoes&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c>.
/// A verificação é comparação em tempo constante. O algoritmo pode ser trocado depois sem tocar no domínio.
/// </summary>
public sealed class Pbkdf2HashSenha : IHashSenha
{
    private const string Prefixo = "pbkdf2-sha256";
    private const int Iteracoes = 210_000;   // OWASP 2023 p/ PBKDF2-SHA256
    private const int TamanhoSalt = 16;       // 128 bits
    private const int TamanhoHash = 32;       // 256 bits
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public string Hash(string senha)
    {
        ArgumentNullException.ThrowIfNull(senha);

        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, Algoritmo, TamanhoHash);

        return $"{Prefixo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verificar(string senha, string hashArmazenado)
    {
        if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hashArmazenado))
            return false;

        var partes = hashArmazenado.Split('$');
        if (partes.Length != 4 || partes[0] != Prefixo)
            return false;

        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
            return false;

        byte[] salt, esperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, Algoritmo, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
