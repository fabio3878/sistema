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
}

public interface IProdutoRepositorio
{
    Task Adicionar(Produto produto, CancellationToken ct = default);
    Task<Produto?> ObterPorId(string empresaId, string id, CancellationToken ct = default);
}

/// <summary>Confirma as mutações pendentes numa transação (Unit of Work do módulo).</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> Salvar(CancellationToken ct = default);
}
