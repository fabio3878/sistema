using Acesso.Contratos;
using Acesso.Dominio;
using BuildingBlocks;

namespace Acesso.Aplicacao;

/// <summary>
/// Casos de uso de autenticação: login, refresh (com rotação), logout e troca de senha. Emite JWT
/// curto + refresh token revogável. Mensagens de falha são genéricas (sem enumeração de usuário).
/// </summary>
public sealed class AutenticacaoAppService(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IRefreshTokenRepositorio refreshTokens,
    IHashSenha hashSenha,
    IServicoToken servicoToken,
    IUnidadeDeTrabalho uow)
{
    private const string FalhaCredenciais = "Credenciais inválidas.";

    public async Task<Result<TokenResposta>> Login(string empresaId, LoginRequest req, CancellationToken ct = default)
    {
        var login = (req.Login ?? string.Empty).Trim().ToLowerInvariant();
        var usuario = await usuarios.ObterPorLogin(empresaId, login, ct);

        // Mesma falha para inexistente / inativo / senha errada — não vaza qual foi.
        if (usuario is null || !usuario.Ativo || !hashSenha.Verificar(req.Senha ?? "", usuario.SenhaHash))
            return Result<TokenResposta>.Falha(FalhaCredenciais);

        var permissoes = await MontarPermissoes(empresaId, usuario, ct);
        usuario.RegistrarLogin();

        var resposta = await EmitirPar(usuario, permissoes, ct);
        await uow.Salvar(ct);
        return Result<TokenResposta>.Ok(resposta);
    }

    public async Task<Result<TokenResposta>> Refresh(string empresaId, string refreshTokenBruto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenBruto))
            return Result<TokenResposta>.Falha(FalhaCredenciais);

        var hash = RefreshToken.Hash(refreshTokenBruto);
        var token = await refreshTokens.ObterPorHash(empresaId, hash, ct);
        if (token is null)
            return Result<TokenResposta>.Falha(FalhaCredenciais);

        // Reuso de um token já revogado ⇒ possível roubo: revoga tudo do usuário.
        if (token.RevogadoEm is not null)
        {
            await refreshTokens.RevogarTodosDoUsuario(empresaId, token.UsuarioId, "Reuso de refresh token revogado", ct);
            await uow.Salvar(ct);
            return Result<TokenResposta>.Falha(FalhaCredenciais);
        }

        var usuario = await usuarios.ObterPorId(empresaId, token.UsuarioId, ct);
        if (usuario is null || !usuario.Ativo
            || !token.EstaAtivo(DateTimeOffset.UtcNow)
            || token.StampSeguranca != usuario.StampSeguranca)
            return Result<TokenResposta>.Falha(FalhaCredenciais);

        var permissoes = await MontarPermissoes(empresaId, usuario, ct);
        var resposta = await EmitirPar(usuario, permissoes, ct, tokenAntigo: token);
        await uow.Salvar(ct);
        return Result<TokenResposta>.Ok(resposta);
    }

    public async Task<Result> Logout(string empresaId, string refreshTokenBruto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenBruto))
            return Result.Ok(); // idempotente

        var token = await refreshTokens.ObterPorHash(empresaId, RefreshToken.Hash(refreshTokenBruto), ct);
        token?.Revogar("Logout");
        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> TrocarSenha(
        string empresaId, string usuarioId, TrocarSenhaRequest req, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorId(empresaId, usuarioId, ct);
        if (usuario is null)
            return Result.Falha("Usuário não encontrado.");

        if (!hashSenha.Verificar(req.SenhaAtual ?? "", usuario.SenhaHash))
            return Result.Falha("Senha atual incorreta.");

        if (string.IsNullOrWhiteSpace(req.SenhaNova) || req.SenhaNova.Length < 6)
            return Result.Falha("A nova senha deve ter ao menos 6 caracteres.");

        usuario.AlterarSenha(hashSenha.Hash(req.SenhaNova));       // rotaciona o StampSeguranca
        await refreshTokens.RevogarTodosDoUsuario(empresaId, usuarioId, "Troca de senha", ct);
        await uow.Salvar(ct);
        return Result.Ok();
    }

    // --- helpers ---

    private async Task<ConjuntoPermissoes> MontarPermissoes(string empresaId, Usuario usuario, CancellationToken ct)
    {
        var perfilIds = usuario.Perfis.Where(p => !p.Excluido).Select(p => p.PerfilId).ToArray();
        if (perfilIds.Length == 0)
            return new ConjuntoPermissoes(false, new HashSet<string>());

        var perfisDoUsuario = await perfis.ObterPorIds(empresaId, perfilIds, ct);
        var ativos = perfisDoUsuario.Where(p => p.Ativo).ToArray();

        if (ativos.Any(p => p.ConcedeTodas))
            return new ConjuntoPermissoes(true, new HashSet<string>());

        var codigos = ativos
            .SelectMany(p => p.Funcionalidades)
            .Where(f => !f.Excluido)
            .Select(f => f.FuncionalidadeCodigo)
            .ToHashSet();

        return new ConjuntoPermissoes(false, codigos);
    }

    private async Task<TokenResposta> EmitirPar(
        Usuario usuario, ConjuntoPermissoes permissoes, CancellationToken ct, RefreshToken? tokenAntigo = null)
    {
        var access = servicoToken.Emitir(new DadosToken(
            usuario.Id, usuario.EmpresaId, usuario.Login, usuario.StampSeguranca, permissoes));

        var (novoRefresh, bruto) = RefreshToken.Emitir(
            usuario.EmpresaId, usuario.Id, usuario.StampSeguranca, servicoToken.DuracaoRefresh);

        await refreshTokens.Adicionar(novoRefresh, ct);
        tokenAntigo?.Revogar("Rotacionado", novoRefresh.Id);

        return new TokenResposta(access.Token, bruto, access.ExpiraEm, usuario.DeveTrocarSenha);
    }
}
