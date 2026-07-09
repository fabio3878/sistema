using BuildingBlocks;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Casos de uso de escrita do módulo. Valida no domínio, persiste via portas e confirma
/// na unidade de trabalho. Não conhece EF Core.
/// </summary>
public sealed class CadastrosAppService(
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho uow)
{
    public async Task<Result<string>> CriarCliente(
        string empresaId, DadosCliente dados, IReadOnlyList<DadosEndereco>? enderecos = null,
        CancellationToken ct = default)
    {
        var criacao = Cliente.Criar(empresaId, dados);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var cliente = criacao.Valor!;

        foreach (var endereco in enderecos ?? [])
        {
            var add = cliente.AdicionarEndereco(endereco);
            if (add.Falhou)
                return Result<string>.Falha(add.Erro!);
        }

        await clientes.Adicionar(cliente, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(cliente.Id);
    }

    public async Task<Result<string>> CriarProduto(
        string empresaId, string sku, string descricao, string ncm, decimal precoVenda,
        string? codigoBarras = null, CancellationToken ct = default)
    {
        var criacao = Produto.Criar(empresaId, sku, descricao, ncm, precoVenda, codigoBarras);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var produto = criacao.Valor!;
        await produtos.Adicionar(produto, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(produto.Id);
    }
}
