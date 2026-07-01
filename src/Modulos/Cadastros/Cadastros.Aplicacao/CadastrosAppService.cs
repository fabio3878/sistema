using BuildingBlocks;
using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Casos de uso de escrita do módulo. Valida no domínio, persiste via portas e confirma
/// na unidade de trabalho. Não conhece EF Core.
/// </summary>
public sealed class CadastrosAppService(
    IPessoaRepositorio pessoas,
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho uow)
{
    public async Task<Result<string>> CriarPessoa(
        string empresaId, string nome, string documento, PapelPessoa papeis,
        CancellationToken ct = default)
    {
        var criacao = Pessoa.Criar(empresaId, nome, documento, papeis);
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var pessoa = criacao.Valor!;
        await pessoas.Adicionar(pessoa, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(pessoa.Id);
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
