using BuildingBlocks;
using Cadastros.Contratos;
using Cadastros.Dominio;

namespace Cadastros.Aplicacao;

/// <summary>
/// Casos de uso de escrita do módulo. Fala DTOs de <c>Cadastros.Contratos</c> na borda (mapeando
/// para os records do Domínio internamente), valida no domínio, persiste via portas e confirma
/// na unidade de trabalho. Não conhece EF Core.
/// </summary>
public sealed class CadastrosAppService(
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IUnidadeDeTrabalho uow)
{
    public async Task<Result<string>> CriarCliente(
        string empresaId, ClienteEntradaDto dados, CancellationToken ct = default)
    {
        var criacao = Cliente.Criar(empresaId, ParaDados(dados));
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var cliente = criacao.Valor!;

        // Documento é único por empresa: pré-checa para devolver mensagem amigável (evita 500 do índice).
        if (await clientes.ObterPorDocumento(empresaId, cliente.Documento, ct) is not null)
            return Result<string>.Falha("Já existe um cliente com este documento.");

        foreach (var endereco in dados.Enderecos ?? [])
        {
            var add = cliente.AdicionarEndereco(ParaDados(endereco));
            if (add.Falhou)
                return Result<string>.Falha(add.Erro!);
        }

        await clientes.Adicionar(cliente, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(cliente.Id);
    }

    public async Task<Result> AtualizarCliente(
        string empresaId, string clienteId, ClienteEntradaDto dados, CancellationToken ct = default)
    {
        var cliente = await clientes.ObterPorId(empresaId, clienteId, ct);
        if (cliente is null)
            return Result.Falha("Cliente não encontrado.");

        var atualizacao = cliente.Atualizar(ParaDados(dados));
        if (atualizacao.Falhou)
            return atualizacao;

        // Se o documento mudou, garante que não colide com outro cliente da empresa.
        var outro = await clientes.ObterPorDocumento(empresaId, cliente.Documento, ct);
        if (outro is not null && outro.Id != clienteId)
            return Result.Falha("Já existe um cliente com este documento.");

        var entradas = (dados.Enderecos ?? [])
            .Select(e => new EnderecoSync(e.Id, ParaDados(e)))
            .ToArray();

        var sync = cliente.SincronizarEnderecos(entradas);
        if (sync.Falhou)
            return sync;

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> AlterarSituacaoCliente(
        string empresaId, string clienteId, bool ativo, CancellationToken ct = default)
    {
        var cliente = await clientes.ObterPorId(empresaId, clienteId, ct);
        if (cliente is null)
            return Result.Falha("Cliente não encontrado.");

        if (ativo) cliente.Ativar();
        else cliente.Inativar();

        await uow.Salvar(ct);
        return Result.Ok();
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

    private static DadosCliente ParaDados(ClienteEntradaDto d) =>
        new(d.Nome, d.Documento, d.TipoPessoa, d.IndicadorIe, d.NomeFantasia,
            d.Email, d.EmailFinanceiro, d.Telefone, d.Celular, d.Whatsapp, d.Site,
            d.DataNascimento, d.Rg, d.OrgaoEmissorRg, d.InscricaoEstadual, d.InscricaoMunicipal,
            d.RegimeTributario, d.LimiteCredito, d.Origem, d.Preferencias, d.Observacoes,
            d.AceitaEmail, d.AceitaSms, d.AceitaWhatsapp, d.AceitaLigacoes, d.AceitouTermosLgpd, d.DataAceiteLgpd);

    private static DadosEndereco ParaDados(EnderecoEntradaDto e) =>
        new(e.Tipo, e.Cep, e.Logradouro, e.Numero, e.Bairro, e.Municipio, e.Uf,
            e.CodigoIbgeMunicipio, e.Complemento, e.Pais);
}
