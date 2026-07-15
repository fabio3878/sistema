using BuildingBlocks;
using Financeiro.Contratos;

namespace Financeiro.Dominio;

/// <summary>Dados do cabeçalho de uma Conta a Receber (entrada da factory).</summary>
public sealed record DadosConta(
    string ClienteId,
    decimal ValorTotal,
    int QuantidadeParcelas,
    DateOnly DataEmissao,
    string? Descricao = null,
    TipoOrigemConta TipoOrigem = TipoOrigemConta.Manual,
    string? DocumentoOrigem = null,
    string? NumeroDocumento = null,
    string? CategoriaFinanceira = null,
    string? Observacoes = null,
    string? UsuarioResponsavelId = null);

/// <summary>Uma parcela do plano (na criação): número, valor e vencimento já definidos/editados.</summary>
public sealed record PlanoParcela(
    int Numero,
    int TotalParcelas,
    decimal Valor,
    DateOnly Vencimento,
    DateOnly? DataPrevistaRecebimento = null,
    decimal? PercentualJurosOverride = null);

/// <summary>
/// Conta a Receber = obrigação financeira do cliente (aggregate root). NÃO tem status próprio: seu
/// estado é <b>derivado</b> da situação de suas parcelas. Uma conta sempre tem ≥1 parcela e nunca é
/// excluída — só cancelada (cancela as parcelas em aberto).
/// </summary>
public sealed class ContaReceber : EntidadeBase
{
    private readonly List<Parcela> _parcelas = [];
    private readonly List<Renegociacao> _renegociacoes = [];

    public string ClienteId { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public TipoOrigemConta TipoOrigem { get; private set; }
    public string? DocumentoOrigem { get; private set; }
    public string? NumeroDocumento { get; private set; }
    public decimal ValorTotal { get; private set; }
    public int QuantidadeParcelas { get; private set; }
    public DateOnly DataEmissao { get; private set; }
    public string? CategoriaFinanceira { get; private set; }
    public string? Observacoes { get; private set; }
    public string? UsuarioResponsavelId { get; private set; }

    public IReadOnlyCollection<Parcela> Parcelas => _parcelas.AsReadOnly();
    public IReadOnlyCollection<Renegociacao> Renegociacoes => _renegociacoes.AsReadOnly();

    private ContaReceber() { }

    /// <summary>Cria a conta com suas parcelas. O <paramref name="plano"/> já vem pronto (gerado ou editado no cadastro).</summary>
    public static Result<ContaReceber> Criar(string empresaId, DadosConta dados, IReadOnlyList<PlanoParcela> plano)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<ContaReceber>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.ClienteId))
            return Result<ContaReceber>.Falha("Cliente é obrigatório.");
        if (dados.ValorTotal <= 0)
            return Result<ContaReceber>.Falha("Valor total deve ser maior que zero.");
        if (dados.QuantidadeParcelas < 1)
            return Result<ContaReceber>.Falha("A conta deve ter ao menos uma parcela.");
        if (plano.Count != dados.QuantidadeParcelas)
            return Result<ContaReceber>.Falha("O número de parcelas do plano não confere com a quantidade informada.");

        var soma = plano.Sum(p => p.Valor);
        if (Math.Abs(soma - dados.ValorTotal) > 0.01m)
            return Result<ContaReceber>.Falha($"A soma das parcelas (R$ {soma:0.00}) não confere com o valor total (R$ {dados.ValorTotal:0.00}).");

        var conta = new ContaReceber { EmpresaId = empresaId };
        conta.AplicarCabecalho(dados);

        foreach (var item in plano.OrderBy(p => p.Numero))
        {
            var criacao = Parcela.Criar(empresaId, conta.Id, item);
            if (criacao.Falhou)
                return Result<ContaReceber>.Falha(criacao.Erro!);
            conta._parcelas.Add(criacao.Valor!);
        }

        return Result<ContaReceber>.Ok(conta);
    }

    /// <summary>Gera um plano de parcelas iguais (ajuste de centavos na última) com vencimentos periódicos.</summary>
    public static IReadOnlyList<PlanoParcela> GerarPlano(decimal valorTotal, int quantidade, DateOnly primeiroVencimento, int intervaloDias = 30)
    {
        var baseVal = Math.Truncate(valorTotal / quantidade * 100) / 100m;
        var lista = new List<PlanoParcela>(quantidade);
        var acumulado = 0m;

        for (var i = 1; i <= quantidade; i++)
        {
            decimal valor;
            if (i < quantidade)
            {
                valor = baseVal;
                acumulado += baseVal;
            }
            else
            {
                valor = valorTotal - acumulado; // absorve a diferença de arredondamento
            }

            var vencimento = primeiroVencimento.AddDays(intervaloDias * (i - 1));
            lista.Add(new PlanoParcela(i, quantidade, valor, vencimento));
        }

        return lista;
    }

    public Result AtualizarCabecalho(string? descricao, string? documentoOrigem, string? numeroDocumento, string? categoriaFinanceira, string? observacoes)
    {
        Descricao = Limpar(descricao);
        DocumentoOrigem = Limpar(documentoOrigem);
        NumeroDocumento = Limpar(numeroDocumento);
        CategoriaFinanceira = Limpar(categoriaFinanceira);
        Observacoes = Limpar(observacoes);
        MarcarAtualizado();
        return Result.Ok();
    }

    public Result<Recebimento> RegistrarRecebimento(string parcelaId, DadosRecebimento dados)
    {
        var parcela = _parcelas.FirstOrDefault(p => p.Id == parcelaId);
        if (parcela is null)
            return Result<Recebimento>.Falha("Parcela não encontrada nesta conta.");

        var resultado = parcela.RegistrarRecebimento(dados);
        if (resultado.Sucesso)
            MarcarAtualizado();
        return resultado;
    }

    public Result<Recebimento> EstornarRecebimento(string parcelaId, string recebimentoId, string? motivo)
    {
        var parcela = _parcelas.FirstOrDefault(p => p.Id == parcelaId);
        if (parcela is null)
            return Result<Recebimento>.Falha("Parcela não encontrada nesta conta.");

        var resultado = parcela.EstornarRecebimento(recebimentoId, motivo);
        if (resultado.Sucesso)
            MarcarAtualizado();
        return resultado;
    }

    public Result AlterarParcela(string parcelaId, decimal valor, DateOnly vencimento, DateOnly? dataPrevista, decimal? percentualOverride, string? observacoes)
    {
        var parcela = _parcelas.FirstOrDefault(p => p.Id == parcelaId);
        if (parcela is null)
            return Result.Falha("Parcela não encontrada nesta conta.");

        var resultado = parcela.AlterarDados(valor, vencimento, dataPrevista, percentualOverride, observacoes);
        if (resultado.Falhou)
            return resultado;

        // Mantém o valor total da conta coerente com a soma das parcelas.
        ValorTotal = _parcelas.Where(p => !p.Cancelada).Sum(p => p.ValorOriginal);
        MarcarAtualizado();
        return Result.Ok();
    }

    /// <summary>
    /// Renegocia parcelas em aberto desta conta: fecha as <paramref name="parcelaIds"/> como
    /// <see cref="Parcela.Renegociada"/> e anexa o <paramref name="novoPlano"/> (parcelas geradas) à mesma
    /// conta, vinculadas pela renegociação criada. Não recalcula <see cref="ValorTotal"/> — que segue como
    /// valor originalmente contratado; os números vivos são derivados na leitura. A soma do novo plano deve
    /// bater com o valor a reparcelar (base − desconto − entrada, calculado em <paramref name="info"/>).
    /// </summary>
    public Result<Renegociacao> Renegociar(IReadOnlyList<string> parcelaIds, IReadOnlyList<PlanoParcela> novoPlano, DadosRenegociacao info)
    {
        if (parcelaIds is null || parcelaIds.Count == 0)
            return Result<Renegociacao>.Falha("Selecione ao menos uma parcela para renegociar.");
        if (novoPlano is null || novoPlano.Count == 0)
            return Result<Renegociacao>.Falha("Informe o novo plano de parcelas.");

        // Resolve as parcelas de origem e valida elegibilidade ANTES de qualquer mutação (evita estado parcial).
        var origens = new List<Parcela>(parcelaIds.Count);
        foreach (var id in parcelaIds.Distinct())
        {
            var p = _parcelas.FirstOrDefault(x => x.Id == id);
            if (p is null)
                return Result<Renegociacao>.Falha("Parcela não encontrada nesta conta.");
            if (p.Cancelada)
                return Result<Renegociacao>.Falha("Parcela cancelada não pode ser renegociada.");
            if (p.Renegociada)
                return Result<Renegociacao>.Falha("Parcela já foi renegociada.");
            if (p.SaldoPrincipal <= 0)
                return Result<Renegociacao>.Falha("Parcela quitada não pode ser renegociada.");
            origens.Add(p);
        }

        var criacao = Renegociacao.Criar(EmpresaId, Id, info);
        if (criacao.Falhou)
            return criacao;
        var reneg = criacao.Valor!;

        var soma = novoPlano.Sum(p => p.Valor);
        if (Math.Abs(soma - reneg.ValorRenegociado) > 0.01m)
            return Result<Renegociacao>.Falha($"A soma do novo plano (R$ {soma:0.00}) não confere com o valor a reparcelar (R$ {reneg.ValorRenegociado:0.00}).");

        foreach (var origem in origens)
        {
            var marca = origem.MarcarRenegociada(reneg.Id);
            if (marca.Falhou)
                return Result<Renegociacao>.Falha(marca.Erro!);
        }

        // Novas parcelas: numeradas continuando após a maior existente; TotalParcelas = tamanho do novo plano.
        var proximo = _parcelas.Max(p => p.Numero) + 1;
        var total = novoPlano.Count;
        var indice = 0;
        foreach (var item in novoPlano.OrderBy(p => p.Numero))
        {
            var plano = item with { Numero = proximo + indice, TotalParcelas = total };
            var novaParcela = Parcela.Criar(EmpresaId, Id, plano);
            if (novaParcela.Falhou)
                return Result<Renegociacao>.Falha(novaParcela.Erro!);
            novaParcela.Valor!.VincularComoGerada(reneg.Id);
            _parcelas.Add(novaParcela.Valor!);
            indice++;
        }

        _renegociacoes.Add(reneg);
        MarcarAtualizado();
        return Result<Renegociacao>.Ok(reneg);
    }

    /// <summary>Cancela a conta: cancela as parcelas em aberto (sem recebimento). Parcelas já recebidas permanecem.</summary>
    public Result Cancelar()
    {
        foreach (var parcela in _parcelas.Where(p => !p.Cancelada && p.TotalPago == 0))
            parcela.Cancelar();

        MarcarAtualizado();
        return Result.Ok();
    }

    public Parcela? ObterParcela(string parcelaId) => _parcelas.FirstOrDefault(p => p.Id == parcelaId);

    private void AplicarCabecalho(DadosConta dados)
    {
        ClienteId = dados.ClienteId;
        ValorTotal = dados.ValorTotal;
        QuantidadeParcelas = dados.QuantidadeParcelas;
        DataEmissao = dados.DataEmissao;
        TipoOrigem = dados.TipoOrigem;
        Descricao = Limpar(dados.Descricao);
        DocumentoOrigem = Limpar(dados.DocumentoOrigem);
        NumeroDocumento = Limpar(dados.NumeroDocumento);
        CategoriaFinanceira = Limpar(dados.CategoriaFinanceira);
        Observacoes = Limpar(dados.Observacoes);
        UsuarioResponsavelId = dados.UsuarioResponsavelId;
    }

    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
