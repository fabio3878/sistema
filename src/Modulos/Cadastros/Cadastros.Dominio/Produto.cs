using BuildingBlocks;

namespace Cadastros.Dominio;

/// <summary>
/// Produto/Serviço (seção 6.2): SKU, código de barras, dados fiscais (NCM…), preço.
/// </summary>
public sealed class Produto : EntidadeBase
{
    public string Sku { get; private set; } = default!;
    public string Descricao { get; private set; } = default!;
    public string? CodigoBarras { get; private set; }

    /// <summary>NCM — classificação fiscal do produto (8 dígitos).</summary>
    public string Ncm { get; private set; } = default!;

    public decimal PrecoVenda { get; private set; }

    private Produto() { }

    public static Result<Produto> Criar(
        string empresaId, string sku, string descricao, string ncm, decimal precoVenda,
        string? codigoBarras = null)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result<Produto>.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(sku))
            return Result<Produto>.Falha("SKU é obrigatório.");
        if (string.IsNullOrWhiteSpace(descricao))
            return Result<Produto>.Falha("Descrição é obrigatória.");

        var ncmLimpo = new string((ncm ?? string.Empty).Where(char.IsDigit).ToArray());
        if (ncmLimpo.Length != 8)
            return Result<Produto>.Falha("NCM deve ter 8 dígitos.");
        if (precoVenda < 0)
            return Result<Produto>.Falha("Preço de venda não pode ser negativo.");

        return Result<Produto>.Ok(new Produto
        {
            EmpresaId = empresaId,
            Sku = sku.Trim(),
            Descricao = descricao.Trim(),
            Ncm = ncmLimpo,
            PrecoVenda = precoVenda,
            CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim(),
        });
    }

    public void AlterarPreco(decimal novoPreco)
    {
        if (novoPreco < 0) return;
        PrecoVenda = novoPreco;
        MarcarAtualizado();
    }
}
