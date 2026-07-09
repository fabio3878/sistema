namespace Acesso.Contratos;

/// <summary>
/// API PÚBLICA de consulta do módulo Acesso. Outros módulos consultam usuários/perfis por AQUI —
/// nunca acessando o Dominio/Infra do Acesso direto.
/// </summary>
public interface IAcessoConsulta
{
    Task<UsuarioDto?> ObterUsuario(string empresaId, string usuarioId, CancellationToken ct = default);
    Task<PerfilDto?> ObterPerfil(string empresaId, string perfilId, CancellationToken ct = default);
}
