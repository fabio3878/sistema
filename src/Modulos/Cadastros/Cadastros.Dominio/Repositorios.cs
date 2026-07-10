using Cadastros.Contratos;

namespace Cadastros.Dominio;

/// <summary>
/// Portas de persistência (o Dominio define, a Infraestrutura implementa).
/// Assim a Aplicacao orquestra sem conhecer EF Core.
/// </summary>
public interface IClienteRepositorio
{
    Task Adicionar(Cliente cliente, CancellationToken ct = default);

    /// <summary>Carrega o cliente com seus endereços (agregado completo).</summary>
    Task<Cliente?> ObterPorId(string empresaId, string id, CancellationToken ct = default);

    /// <summary>Busca por documento (só dígitos) para checar duplicidade no cadastro.</summary>
    Task<Cliente?> ObterPorDocumento(string empresaId, string documento, CancellationToken ct = default);

    /// <summary>Lista os clientes da empresa (com endereços), aplicando os filtros informados.</summary>
    Task<IReadOnlyList<Cliente>> Listar(string empresaId, FiltroClientes filtro, CancellationToken ct = default);
}

/// <summary>Porta de leitura das tabelas de referência IBGE (dado global, não-tenant).</summary>
public interface ILocalidadeRepositorio
{
    Task<IReadOnlyList<Estado>> ListarEstados(CancellationToken ct = default);
    Task<IReadOnlyList<Municipio>> ListarMunicipios(string uf, CancellationToken ct = default);
    Task<bool> Vazio(CancellationToken ct = default);
    Task SemearAsync(IEnumerable<Estado> estados, IEnumerable<Municipio> municipios, CancellationToken ct = default);
}

/// <summary>Porta da tabela de referência global de unidades de medida (não-tenant).</summary>
public interface IUnidadeRepositorio
{
    Task<IReadOnlyList<Unidade>> Listar(CancellationToken ct = default);
    Task<Unidade?> ObterPorSigla(string sigla, CancellationToken ct = default);
    Task<bool> Vazio(CancellationToken ct = default);
    Task SemearAsync(IEnumerable<Unidade> unidades, CancellationToken ct = default);
}

public interface IProdutoRepositorio
{
    Task Adicionar(Produto produto, CancellationToken ct = default);
    Task<Produto?> ObterPorId(string empresaId, string id, CancellationToken ct = default);

    /// <summary>Busca por código interno (único por empresa) para checar duplicidade no cadastro.</summary>
    Task<Produto?> ObterPorCodigo(string empresaId, string codigo, CancellationToken ct = default);

    /// <summary>Lista os produtos da empresa aplicando os filtros informados.</summary>
    Task<IReadOnlyList<Produto>> Listar(string empresaId, FiltroProdutos filtro, CancellationToken ct = default);
}

public interface IServicoRepositorio
{
    Task Adicionar(Servico servico, CancellationToken ct = default);
    Task<Servico?> ObterPorId(string empresaId, string id, CancellationToken ct = default);

    /// <summary>Busca por código interno (único por empresa) para checar duplicidade no cadastro.</summary>
    Task<Servico?> ObterPorCodigo(string empresaId, string codigo, CancellationToken ct = default);

    /// <summary>Lista os serviços da empresa aplicando os filtros informados.</summary>
    Task<IReadOnlyList<Servico>> Listar(string empresaId, FiltroServicos filtro, CancellationToken ct = default);
}

/// <summary>Confirma as mutações pendentes numa transação (Unit of Work do módulo).</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> Salvar(CancellationToken ct = default);
}
