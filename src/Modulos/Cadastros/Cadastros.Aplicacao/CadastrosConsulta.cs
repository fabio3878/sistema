using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Implementa a API pública de consulta (<see cref="ICadastrosConsulta"/>) mapeando
/// entidades de domínio para DTOs. Depende só das portas do Dominio — nada de EF.
/// </summary>
public sealed class CadastrosConsulta(IClienteRepositorio clientes, IProdutoRepositorio produtos)
    : ICadastrosConsulta
{
    public async Task<ClienteDto?> ObterCliente(string empresaId, string clienteId, CancellationToken ct = default)
    {
        var c = await clientes.ObterPorId(empresaId, clienteId, ct);
        if (c is null) return null;

        var enderecos = c.Enderecos
            .Select(e => new EnderecoDto(
                e.Id, e.Tipo, e.Cep, e.Logradouro, e.Numero, e.Complemento,
                e.Bairro, e.Municipio, e.Uf, e.CodigoIbgeMunicipio, e.Pais))
            .ToArray();

        return new ClienteDto(
            c.Id, c.EmpresaId, c.Nome, c.Documento, c.TipoPessoa, c.NomeFantasia, c.Email,
            c.Telefone, c.DataNascimento, c.Rg, c.OrgaoEmissorRg, c.InscricaoEstadual,
            c.InscricaoMunicipal, c.IndicadorIe, c.RegimeTributario, c.LimiteCredito,
            c.Observacoes, c.Ativo, enderecos);
    }

    public async Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default)
    {
        var p = await produtos.ObterPorId(empresaId, produtoId, ct);
        return p is null
            ? null
            : new ProdutoDto(p.Id, p.EmpresaId, p.Sku, p.Descricao, p.CodigoBarras, p.Ncm, p.PrecoVenda);
    }
}
