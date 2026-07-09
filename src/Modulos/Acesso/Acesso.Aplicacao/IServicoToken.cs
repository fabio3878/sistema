namespace Acesso.Aplicacao;

/// <summary>Conjunto de permissões efetivas de um usuário (união dos perfis, ou super-perfil).</summary>
public sealed record ConjuntoPermissoes(bool ConcedeTodas, IReadOnlySet<string> Codigos);

/// <summary>Dados necessários para emitir o access token (viram claims).</summary>
public sealed record DadosToken(
    string UsuarioId,
    string EmpresaId,
    string Login,
    string StampSeguranca,
    ConjuntoPermissoes Permissoes);

/// <summary>Access token emitido + seu vencimento.</summary>
public sealed record TokenAcesso(string Token, DateTimeOffset ExpiraEm);

/// <summary>
/// Porta de emissão de access token (a impl JWT fica na Infraestrutura, para o Dominio/Aplicacao
/// não conhecerem detalhes de assinatura). Também expõe a duração do refresh (configurável).
/// </summary>
public interface IServicoToken
{
    TokenAcesso Emitir(DadosToken dados);

    /// <summary>Duração do refresh token (lida de config; usada para calcular o vencimento).</summary>
    TimeSpan DuracaoRefresh { get; }
}
