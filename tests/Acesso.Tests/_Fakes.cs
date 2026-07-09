using Acesso.Aplicacao;
using Acesso.Dominio;

namespace Acesso.Tests;

/// <summary>Repositórios in-memory e serviço de token falso para testar a orquestração de autenticação.</summary>
internal static class Fakes
{
    public const string Empresa = "EMPRESA_DEV";
}

internal sealed class UsuarioRepoFake : IUsuarioRepositorio
{
    private readonly Dictionary<string, Usuario> _porId = [];

    public Task Adicionar(Usuario usuario, CancellationToken ct = default)
    {
        _porId[usuario.Id] = usuario;
        return Task.CompletedTask;
    }

    public Task<Usuario?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        Task.FromResult(_porId.TryGetValue(id, out var u) && u.EmpresaId == empresaId ? u : null);

    public Task<Usuario?> ObterPorLogin(string empresaId, string loginNormalizado, CancellationToken ct = default) =>
        Task.FromResult(_porId.Values.FirstOrDefault(u =>
            u.EmpresaId == empresaId && u.LoginNormalizado == loginNormalizado));

    public Task<bool> ExisteAlgum(string empresaId, CancellationToken ct = default) =>
        Task.FromResult(_porId.Values.Any(u => u.EmpresaId == empresaId));

    public Task<IReadOnlyList<Usuario>> Listar(string empresaId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Usuario>>(_porId.Values.Where(u => u.EmpresaId == empresaId).ToList());
}

internal sealed class PerfilRepoFake : IPerfilRepositorio
{
    private readonly Dictionary<string, Perfil> _porId = [];

    public Task Adicionar(Perfil perfil, CancellationToken ct = default)
    {
        _porId[perfil.Id] = perfil;
        return Task.CompletedTask;
    }

    public Task<Perfil?> ObterPorId(string empresaId, string id, CancellationToken ct = default) =>
        Task.FromResult(_porId.TryGetValue(id, out var p) ? p : null);

    public Task<Perfil?> ObterPorNome(string empresaId, string nome, CancellationToken ct = default) =>
        Task.FromResult(_porId.Values.FirstOrDefault(p => p.EmpresaId == empresaId && p.Nome == nome));

    public Task<IReadOnlyList<Perfil>> ObterPorIds(
        string empresaId, IReadOnlyCollection<string> ids, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Perfil>>(
            _porId.Values.Where(p => p.EmpresaId == empresaId && ids.Contains(p.Id)).ToList());
}

internal sealed class RefreshRepoFake : IRefreshTokenRepositorio
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task Adicionar(RefreshToken token, CancellationToken ct = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> ObterPorHash(string empresaId, string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.EmpresaId == empresaId && t.TokenHash == tokenHash));

    public Task RevogarTodosDoUsuario(string empresaId, string usuarioId, string motivo, CancellationToken ct = default)
    {
        foreach (var t in Tokens.Where(t => t.EmpresaId == empresaId && t.UsuarioId == usuarioId && t.RevogadoEm is null))
            t.Revogar(motivo);
        return Task.CompletedTask;
    }
}

internal sealed class UowFake : IUnidadeDeTrabalho
{
    public int Salvos { get; private set; }
    public Task<int> Salvar(CancellationToken ct = default) => Task.FromResult(++Salvos);
}

/// <summary>Token de acesso falso: devolve uma string previsível (não é JWT real).</summary>
internal sealed class ServicoTokenFake : IServicoToken
{
    public TimeSpan DuracaoRefresh => TimeSpan.FromHours(12);
    public TokenAcesso Emitir(DadosToken dados) =>
        new($"fake.{dados.UsuarioId}.{dados.StampSeguranca}", DateTimeOffset.UtcNow.AddMinutes(15));
}
