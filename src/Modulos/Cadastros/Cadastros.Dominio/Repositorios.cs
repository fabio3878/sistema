namespace Cadastros.Dominio;

/// <summary>
/// Portas de persistência (o Dominio define, a Infraestrutura implementa).
/// Assim a Aplicacao orquestra sem conhecer EF Core.
/// </summary>
public interface IPessoaRepositorio
{
    Task Adicionar(Pessoa pessoa, CancellationToken ct = default);
    Task<Pessoa?> ObterPorId(string empresaId, string id, CancellationToken ct = default);
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
