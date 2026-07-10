namespace Cadastros.Contratos;

/// <summary>
/// API PÚBLICA de consulta do módulo Cadastros (seção 6.2). Outros módulos consultam
/// Cliente/Produto por AQUI — nunca acessando o Dominio/Infra do Cadastros direto.
/// </summary>
public interface ICadastrosConsulta
{
    Task<IReadOnlyList<ClienteResumoDto>> ListarClientes(string empresaId, FiltroClientes filtro, CancellationToken ct = default);
    Task<ClienteDto?> ObterCliente(string empresaId, string clienteId, CancellationToken ct = default);
    Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default);

    /// <summary>Estados (UF) do IBGE — referência global (não-tenant).</summary>
    Task<IReadOnlyList<EstadoDto>> ListarEstados(CancellationToken ct = default);

    /// <summary>Municípios de uma UF (IBGE) — referência global (não-tenant).</summary>
    Task<IReadOnlyList<MunicipioDto>> ListarMunicipios(string uf, CancellationToken ct = default);
}
