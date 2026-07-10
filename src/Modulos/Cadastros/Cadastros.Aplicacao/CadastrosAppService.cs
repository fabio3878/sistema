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
    IServicoRepositorio servicos,
    IUnidadeRepositorio unidades,
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
        string empresaId, ProdutoEntradaDto dados, CancellationToken ct = default)
    {
        var unidadeValida = await UnidadeExiste(dados.Unidade, ct);
        if (unidadeValida.Falhou)
            return Result<string>.Falha(unidadeValida.Erro!);

        var criacao = Produto.Criar(empresaId, ParaDados(dados));
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var produto = criacao.Valor!;

        // Código interno é único por empresa quando informado: pré-checa para mensagem amigável.
        if (produto.CodigoInterno is not null &&
            await produtos.ObterPorCodigo(empresaId, produto.CodigoInterno, ct) is not null)
            return Result<string>.Falha("Já existe um produto com este código interno.");

        await produtos.Adicionar(produto, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(produto.Id);
    }

    public async Task<Result> AtualizarProduto(
        string empresaId, string produtoId, ProdutoEntradaDto dados, CancellationToken ct = default)
    {
        var produto = await produtos.ObterPorId(empresaId, produtoId, ct);
        if (produto is null)
            return Result.Falha("Produto não encontrado.");

        var unidadeValida = await UnidadeExiste(dados.Unidade, ct);
        if (unidadeValida.Falhou)
            return unidadeValida;

        var atualizacao = produto.Atualizar(ParaDados(dados));
        if (atualizacao.Falhou)
            return atualizacao;

        // Se o código mudou, garante que não colide com outro produto da empresa.
        if (produto.CodigoInterno is not null)
        {
            var outro = await produtos.ObterPorCodigo(empresaId, produto.CodigoInterno, ct);
            if (outro is not null && outro.Id != produtoId)
                return Result.Falha("Já existe um produto com este código interno.");
        }

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> AlterarSituacaoProduto(
        string empresaId, string produtoId, bool ativo, CancellationToken ct = default)
    {
        var produto = await produtos.ObterPorId(empresaId, produtoId, ct);
        if (produto is null)
            return Result.Falha("Produto não encontrado.");

        if (ativo) produto.Ativar();
        else produto.Inativar();

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result<string>> CriarServico(
        string empresaId, ServicoEntradaDto dados, CancellationToken ct = default)
    {
        var unidadeValida = await UnidadeExiste(dados.Unidade, ct);
        if (unidadeValida.Falhou)
            return Result<string>.Falha(unidadeValida.Erro!);

        var criacao = Servico.Criar(empresaId, ParaDados(dados));
        if (criacao.Falhou)
            return Result<string>.Falha(criacao.Erro!);

        var servico = criacao.Valor!;

        if (servico.CodigoInterno is not null &&
            await servicos.ObterPorCodigo(empresaId, servico.CodigoInterno, ct) is not null)
            return Result<string>.Falha("Já existe um serviço com este código interno.");

        await servicos.Adicionar(servico, ct);
        await uow.Salvar(ct);
        return Result<string>.Ok(servico.Id);
    }

    public async Task<Result> AtualizarServico(
        string empresaId, string servicoId, ServicoEntradaDto dados, CancellationToken ct = default)
    {
        var servico = await servicos.ObterPorId(empresaId, servicoId, ct);
        if (servico is null)
            return Result.Falha("Serviço não encontrado.");

        var unidadeValida = await UnidadeExiste(dados.Unidade, ct);
        if (unidadeValida.Falhou)
            return unidadeValida;

        var atualizacao = servico.Atualizar(ParaDados(dados));
        if (atualizacao.Falhou)
            return atualizacao;

        if (servico.CodigoInterno is not null)
        {
            var outro = await servicos.ObterPorCodigo(empresaId, servico.CodigoInterno, ct);
            if (outro is not null && outro.Id != servicoId)
                return Result.Falha("Já existe um serviço com este código interno.");
        }

        await uow.Salvar(ct);
        return Result.Ok();
    }

    public async Task<Result> AlterarSituacaoServico(
        string empresaId, string servicoId, bool ativo, CancellationToken ct = default)
    {
        var servico = await servicos.ObterPorId(empresaId, servicoId, ct);
        if (servico is null)
            return Result.Falha("Serviço não encontrado.");

        if (ativo) servico.Ativar();
        else servico.Inativar();

        await uow.Salvar(ct);
        return Result.Ok();
    }

    private async Task<Result> UnidadeExiste(string sigla, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sigla))
            return Result.Falha("Unidade é obrigatória.");
        if (await unidades.ObterPorSigla(sigla, ct) is null)
            return Result.Falha($"Unidade '{sigla}' não existe no cadastro.");
        return Result.Ok();
    }

    private static DadosProduto ParaDados(ProdutoEntradaDto d) =>
        new(d.Descricao, d.Ncm, d.PrecoVenda, d.Unidade, d.Origem, d.CodigoInterno, d.CodigoBarras, d.Cest);

    private static DadosServico ParaDados(ServicoEntradaDto d) =>
        new(d.Descricao, d.PrecoVenda, d.Unidade, d.CodigoInterno);

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
