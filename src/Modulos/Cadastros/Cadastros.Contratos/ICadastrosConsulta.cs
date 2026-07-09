namespace Cadastros.Contratos;

/// <summary>
/// API PÚBLICA de consulta do módulo Cadastros (seção 6.2). Outros módulos consultam
/// Cliente/Produto por AQUI — nunca acessando o Dominio/Infra do Cadastros direto.
/// </summary>
public interface ICadastrosConsulta
{
    Task<ClienteDto?> ObterCliente(string empresaId, string clienteId, CancellationToken ct = default);
    Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default);
}
