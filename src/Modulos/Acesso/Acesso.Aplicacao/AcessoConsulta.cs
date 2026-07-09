using Acesso.Contratos;
using Acesso.Dominio;

namespace Acesso.Aplicacao;

/// <summary>
/// Implementa a API pública de consulta (<see cref="IAcessoConsulta"/>) mapeando entidades de
/// domínio para DTOs. Nunca expõe o hash de senha. Depende só das portas do Dominio — nada de EF.
/// </summary>
public sealed class AcessoConsulta(IUsuarioRepositorio usuarios, IPerfilRepositorio perfis)
    : IAcessoConsulta
{
    public async Task<UsuarioDto?> ObterUsuario(string empresaId, string usuarioId, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorId(empresaId, usuarioId, ct);
        if (u is null) return null;

        var perfilIds = u.Perfis.Where(p => !p.Excluido).Select(p => p.PerfilId).ToArray();

        return new UsuarioDto(
            u.Id, u.EmpresaId, u.Login, u.NomeExibicao, u.Email, u.Ativo,
            u.DeveTrocarSenha, u.UltimoLoginEm, perfilIds);
    }

    public async Task<PerfilDto?> ObterPerfil(string empresaId, string perfilId, CancellationToken ct = default)
    {
        var p = await perfis.ObterPorId(empresaId, perfilId, ct);
        if (p is null) return null;

        var funcionalidades = p.Funcionalidades
            .Where(f => !f.Excluido)
            .Select(f => f.FuncionalidadeCodigo)
            .ToArray();

        return new PerfilDto(
            p.Id, p.EmpresaId, p.Nome, p.Descricao, p.Ativo, p.Protegido, p.ConcedeTodas, funcionalidades);
    }
}
