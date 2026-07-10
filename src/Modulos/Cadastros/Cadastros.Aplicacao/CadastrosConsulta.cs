using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Implementa a API pública de consulta (<see cref="ICadastrosConsulta"/>) mapeando
/// entidades de domínio para DTOs. Depende só das portas do Dominio — nada de EF.
/// </summary>
public sealed class CadastrosConsulta(
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IServicoRepositorio servicos,
    ILocalidadeRepositorio localidades,
    IUnidadeRepositorio unidades)
    : ICadastrosConsulta
{
    public async Task<IReadOnlyList<ClienteResumoDto>> ListarClientes(string empresaId, FiltroClientes filtro, CancellationToken ct = default)
    {
        var lista = await clientes.Listar(empresaId, filtro, ct);
        return lista.Select(c =>
        {
            var principal = c.Enderecos.FirstOrDefault(e => e.Tipo == TipoEndereco.Principal)
                            ?? c.Enderecos.FirstOrDefault();
            return new ClienteResumoDto(
                c.Id, c.Nome, c.Documento, c.TipoPessoa, c.NomeFantasia, c.Email, c.Telefone,
                principal?.Municipio, principal?.Uf, c.Ativo, c.Enderecos.Count);
        }).ToArray();
    }

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
            c.Id, c.EmpresaId, c.Nome, c.Documento, c.TipoPessoa, c.NomeFantasia,
            c.Email, c.EmailFinanceiro, c.Telefone, c.Celular, c.Whatsapp, c.Site,
            c.DataNascimento, c.Rg, c.OrgaoEmissorRg, c.InscricaoEstadual, c.InscricaoMunicipal,
            c.IndicadorIe, c.RegimeTributario, c.LimiteCredito, c.Origem, c.Preferencias, c.Observacoes,
            c.AceitaEmail, c.AceitaSms, c.AceitaWhatsapp, c.AceitaLigacoes, c.AceitouTermosLgpd, c.DataAceiteLgpd,
            c.Ativo, enderecos);
    }

    public async Task<IReadOnlyList<ProdutoResumoDto>> ListarProdutos(string empresaId, FiltroProdutos filtro, CancellationToken ct = default)
    {
        var lista = await produtos.Listar(empresaId, filtro, ct);
        return lista.Select(p => new ProdutoResumoDto(
            p.Id, p.CodigoInterno, p.Descricao, p.CodigoBarras, p.Unidade, p.PrecoVenda, p.Ativo)).ToArray();
    }

    public async Task<ProdutoDto?> ObterProduto(string empresaId, string produtoId, CancellationToken ct = default)
    {
        var p = await produtos.ObterPorId(empresaId, produtoId, ct);
        return p is null
            ? null
            : new ProdutoDto(p.Id, p.EmpresaId, p.CodigoInterno, p.Descricao, p.CodigoBarras,
                p.Unidade, p.Ncm, p.Cest, p.Origem, p.PrecoVenda, p.Ativo);
    }

    public async Task<IReadOnlyList<ServicoResumoDto>> ListarServicos(string empresaId, FiltroServicos filtro, CancellationToken ct = default)
    {
        var lista = await servicos.Listar(empresaId, filtro, ct);
        return lista.Select(s => new ServicoResumoDto(
            s.Id, s.CodigoInterno, s.Descricao, s.Unidade, s.PrecoVenda, s.Ativo)).ToArray();
    }

    public async Task<ServicoDto?> ObterServico(string empresaId, string servicoId, CancellationToken ct = default)
    {
        var s = await servicos.ObterPorId(empresaId, servicoId, ct);
        return s is null
            ? null
            : new ServicoDto(s.Id, s.EmpresaId, s.CodigoInterno, s.Descricao, s.Unidade, s.PrecoVenda, s.Ativo);
    }

    public async Task<IReadOnlyList<EstadoDto>> ListarEstados(CancellationToken ct = default) =>
        (await localidades.ListarEstados(ct)).Select(e => new EstadoDto(e.Uf, e.Nome)).ToArray();

    public async Task<IReadOnlyList<MunicipioDto>> ListarMunicipios(string uf, CancellationToken ct = default) =>
        (await localidades.ListarMunicipios(uf, ct)).Select(m => new MunicipioDto(m.CodigoIbge, m.Nome, m.Uf)).ToArray();

    public async Task<IReadOnlyList<UnidadeDto>> ListarUnidades(CancellationToken ct = default) =>
        (await unidades.Listar(ct)).Select(u => new UnidadeDto(u.Sigla, u.Descricao, u.CasasDecimais, u.Fracionavel)).ToArray();
}
