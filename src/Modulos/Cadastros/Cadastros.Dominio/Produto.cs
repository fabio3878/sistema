using BuildingBlocks;
using Cadastros.Contratos;

namespace Cadastros.Dominio;

/// <summary>
/// Dados de um produto (entrada da factory). Agrupa os campos para a assinatura de
/// <see cref="Produto.Criar"/>/<see cref="Produto.Atualizar"/> não explodir em posicionais.
/// </summary>
public sealed record DadosProduto(
    string Descricao,
    string Ncm,
    decimal PrecoVenda,
    string Unidade,
    OrigemMercadoria Origem = OrigemMercadoria.Nacional,
    string? CodigoInterno = null,
    string? CodigoBarras = null,
    string? Cest = null);

/// <summary>
/// Produto = MERCADORIA (seção 6.2) — master data. Serviço tem cadastro próprio
/// (<see cref="Servico"/>). Guarda só a IDENTIDADE fiscal intrínseca (NCM, CEST, origem, unidade)
/// + preço base. Regras tributárias (CST/CSOSN, alíquotas, CFOP por UF×regime) NÃO moram aqui:
/// são resolvidas no módulo Fiscal. Estoque/custo/tabelas de preço são de outros módulos.
/// </summary>
public sealed class Produto : EntidadeBase
{
    /// <summary>Código interno/referência do produto (opcional; único por empresa quando informado).</summary>
    public string? CodigoInterno { get; private set; }

    public string Descricao { get; private set; } = default!;
    public string? CodigoBarras { get; private set; }

    /// <summary>Sigla da unidade de medida (ref. <c>cad_unidades</c>).</summary>
    public string Unidade { get; private set; } = default!;

    /// <summary>NCM — classificação fiscal do produto (8 dígitos).</summary>
    public string Ncm { get; private set; } = default!;

    /// <summary>CEST — código especificador da substituição tributária (7 dígitos, opcional).</summary>
    public string? Cest { get; private set; }

    /// <summary>Origem da mercadoria (nacional/importada) — compõe o CST/CSOSN na venda.</summary>
    public OrigemMercadoria Origem { get; private set; }

    /// <summary>Preço base/referência de venda. Múltiplos preços virão das Tabelas de Preço.</summary>
    public decimal PrecoVenda { get; private set; }

    /// <summary>Status de negócio: inativo continua visível, mas bloqueado. Difere do soft delete.</summary>
    public bool Ativo { get; private set; } = true;

    private Produto() { }

    public static Result<Produto> Criar(string empresaId, DadosProduto dados)
    {
        var validacao = Validar(empresaId, dados);
        if (validacao.Falhou)
            return Result<Produto>.Falha(validacao.Erro!);

        var produto = new Produto { EmpresaId = empresaId };
        produto.AplicarDados(dados);
        return Result<Produto>.Ok(produto);
    }

    /// <summary>Atualiza os dados do produto. Não mexe em <see cref="Ativo"/> nem em CriadoEm (imutável).</summary>
    public Result Atualizar(DadosProduto dados)
    {
        var validacao = Validar(EmpresaId, dados);
        if (validacao.Falhou)
            return validacao;

        AplicarDados(dados);
        MarcarAtualizado();
        return Result.Ok();
    }

    private static Result Validar(string empresaId, DadosProduto dados)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Descricao))
            return Result.Falha("Descrição é obrigatória.");
        if (string.IsNullOrWhiteSpace(dados.Unidade))
            return Result.Falha("Unidade é obrigatória.");

        var ncm = SomenteDigitos(dados.Ncm);
        if (ncm.Length != 8)
            return Result.Falha("NCM deve ter 8 dígitos.");

        if (!string.IsNullOrWhiteSpace(dados.Cest))
        {
            var cest = SomenteDigitos(dados.Cest);
            if (cest.Length != 7)
                return Result.Falha("CEST deve ter 7 dígitos.");
        }

        if (dados.PrecoVenda < 0)
            return Result.Falha("Preço de venda não pode ser negativo.");

        return Result.Ok();
    }

    private void AplicarDados(DadosProduto dados)
    {
        CodigoInterno = string.IsNullOrWhiteSpace(dados.CodigoInterno) ? null : dados.CodigoInterno.Trim();
        Descricao = dados.Descricao.Trim();
        CodigoBarras = string.IsNullOrWhiteSpace(dados.CodigoBarras) ? null : dados.CodigoBarras.Trim();
        Unidade = dados.Unidade.Trim().ToUpperInvariant();
        Ncm = SomenteDigitos(dados.Ncm);
        Cest = string.IsNullOrWhiteSpace(dados.Cest) ? null : SomenteDigitos(dados.Cest);
        Origem = dados.Origem;
        PrecoVenda = dados.PrecoVenda;
    }

    public void AlterarPreco(decimal novoPreco)
    {
        if (novoPreco < 0) return;
        PrecoVenda = novoPreco;
        MarcarAtualizado();
    }

    public void Ativar()
    {
        if (Ativo) return;
        Ativo = true;
        MarcarAtualizado();
    }

    public void Inativar()
    {
        if (!Ativo) return;
        Ativo = false;
        MarcarAtualizado();
    }

    private static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
