using Plataforma.Dominio;

namespace Plataforma.Infraestrutura;

/// <summary>
/// Contexto de empresa fixo para desenvolvimento (single-tenant local).
/// Em produção troca por um que lê o tenant do usuário autenticado / config do device.
/// </summary>
public sealed class ContextoEmpresaFixo(string empresaId) : IContextoEmpresa
{
    /// <summary>EmpresaId padrão usado em dev quando nada é configurado.</summary>
    public const string EmpresaPadrao = "EMPRESA_DEV";

    public ContextoEmpresaFixo() : this(EmpresaPadrao) { }

    public string EmpresaId { get; } = empresaId;
}
