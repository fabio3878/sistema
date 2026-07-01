namespace Plataforma.Dominio;

/// <summary>
/// Fornece o tenant (EmpresaId) atual. Toda query de módulo filtra por ele.
/// Em produção virá do usuário autenticado / configuração do device; em dev, um stub fixo.
/// </summary>
public interface IContextoEmpresa
{
    /// <summary>EmpresaId (tenant) da requisição/operação corrente.</summary>
    string EmpresaId { get; }
}
