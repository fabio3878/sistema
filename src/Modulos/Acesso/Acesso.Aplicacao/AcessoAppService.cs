using Acesso.Dominio;
using BuildingBlocks;

namespace Acesso.Aplicacao;

/// <summary>
/// Casos de uso de escrita do módulo Acesso. Valida no domínio, persiste via portas e confirma
/// na unidade de trabalho. Não conhece EF Core.
/// </summary>
public sealed class AcessoAppService(
    IUsuarioRepositorio usuarios,
    IPerfilRepositorio perfis,
    IHashSenha hashSenha,
    IUnidadeDeTrabalho uow)
{
    public async Task<Result<string>> CriarUsuario(
        string empresaId, DadosUsuario dados, IReadOnlyList<string>? perfilIds = null,
        CancellationToken ct = default)
    {
        var criacao = Usuario.Criar(empresaId, dados, hashSenha);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var usuario = criacao.Valor!;

        var jaExiste = await usuarios.ObterPorLogin(empresaId, usuario.LoginNormalizado, ct);
        if (jaExiste is not null)
            return Result<string>.Falha("Já existe um usuário com esse login.");

        foreach (var perfilId in perfilIds ?? [])
        {
            var atribuir = usuario.AtribuirPerfil(perfilId);
            if (atribuir.Falhou)
                return Result<string>.Falha(atribuir.Erro!);
        }

        await usuarios.Adicionar(usuario, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(usuario.Id);
    }

    public async Task<Result<string>> CriarPerfil(
        string empresaId, string nome, string? descricao = null, CancellationToken ct = default)
    {
        var criacao = Perfil.Criar(empresaId, nome, descricao);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var perfil = criacao.Valor!;

        var jaExiste = await perfis.ObterPorNome(empresaId, perfil.Nome, ct);
        if (jaExiste is not null)
            return Result<string>.Falha("Já existe um perfil com esse nome.");

        await perfis.Adicionar(perfil, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(perfil.Id);
    }

    public async Task<Result> AtribuirPerfil(
        string empresaId, string usuarioId, string perfilId, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObterPorId(empresaId, usuarioId, ct);
        if (usuario is null)
            return Result.Falha("Usuário não encontrado.");

        var perfil = await perfis.ObterPorId(empresaId, perfilId, ct);
        if (perfil is null)
            return Result.Falha("Perfil não encontrado.");

        var atribuir = usuario.AtribuirPerfil(perfilId);
        if (atribuir.Falhou)
            return atribuir;

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> ConcederFuncionalidade(
        string empresaId, string perfilId, string funcionalidadeCodigo, CancellationToken ct = default)
    {
        var perfil = await perfis.ObterPorId(empresaId, perfilId, ct);
        if (perfil is null)
            return Result.Falha("Perfil não encontrado.");

        var conceder = perfil.Conceder(funcionalidadeCodigo);
        if (conceder.Falhou)
            return conceder;

        await uow.Salvar(ct);
        return Result.Ok();
    }
}
