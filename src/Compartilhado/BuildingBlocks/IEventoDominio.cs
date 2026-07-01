namespace BuildingBlocks;

/// <summary>
/// Marcador de evento de domínio (seção 7). Eventos são <c>record</c> imutáveis,
/// nomeados no passado (fato ocorrido), publicados via Wolverine e consumidos por
/// handlers. Vivem em <c>*.Contratos</c> para poderem cruzar a fronteira dos módulos.
/// </summary>
public interface IEventoDominio
{
    /// <summary>Empresa/tenant dono do fato (todo evento é particionado por empresa).</summary>
    string EmpresaId { get; }
}
