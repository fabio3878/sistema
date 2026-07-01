using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Implementa a API pública de consulta (<see cref="ICadastrosConsulta"/>) mapeando
/// entidades de domínio para DTOs. Depende só das portas do Dominio — nada de EF.
/// </summary>
public sealed class CadastrosConsulta(IPessoaRepositorio pessoas, IProdutoRepositorio produtos)
    : ICadastrosConsulta
{
    public async Task<PessoaDto?> ObterPessoa(string empresaId, string pessoaId, CancellationToken ct = default)
    {
        var p = await pessoas.ObterPorId(empresaId, pessoaId, ct);
        return p is null ? null : new PessoaDto(p.Id, p.EmpresaId, p.Nome, p.Documento, p.Papeis);
    }

    public async Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default)
    {
        var p = await produtos.ObterPorId(empresaId, produtoId, ct);
        return p is null
            ? null
            : new ProdutoDto(p.Id, p.EmpresaId, p.Sku, p.Descricao, p.CodigoBarras, p.Ncm, p.PrecoVenda);
    }
}
