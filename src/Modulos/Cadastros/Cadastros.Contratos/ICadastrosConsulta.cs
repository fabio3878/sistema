using BuildingBlocks;

namespace Cadastros.Contratos;

/// <summary>
/// API PÚBLICA de consulta do módulo Cadastros (seção 6.2). Outros módulos consultam
/// Cliente/Produto por AQUI — nunca acessando o Dominio/Infra do Cadastros direto.
/// </summary>
public interface ICadastrosConsulta
{
    /// <summary>Trilha de auditoria do módulo (clientes, produtos, serviços), paginada.</summary>
    Task<PaginaResultado<AuditoriaDto>> ListarAuditoria(string empresaId, FiltroAuditoria filtro, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteResumoDto>> ListarClientes(string empresaId, FiltroClientes filtro, CancellationToken ct = default);
    Task<ClienteDto?> ObterCliente(string empresaId, string clienteId, CancellationToken ct = default);

    Task<IReadOnlyList<ProdutoResumoDto>> ListarProdutos(string empresaId, FiltroProdutos filtro, CancellationToken ct = default);
    Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default);

    Task<IReadOnlyList<ServicoResumoDto>> ListarServicos(string empresaId, FiltroServicos filtro, CancellationToken ct = default);
    Task<ServicoDto?> ObterServico(string empresaId, string servicoId, CancellationToken ct = default);

    /// <summary>Estados (UF) do IBGE — referência global (não-tenant).</summary>
    Task<IReadOnlyList<EstadoDto>> ListarEstados(CancellationToken ct = default);

    /// <summary>Municípios de uma UF (IBGE) — referência global (não-tenant).</summary>
    Task<IReadOnlyList<MunicipioDto>> ListarMunicipios(string uf, CancellationToken ct = default);

    /// <summary>Unidades de medida — referência global (não-tenant).</summary>
    Task<IReadOnlyList<UnidadeDto>> ListarUnidades(CancellationToken ct = default);
}
