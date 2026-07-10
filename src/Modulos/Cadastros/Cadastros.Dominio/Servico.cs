using BuildingBlocks;

namespace Cadastros.Dominio;

/// <summary>Dados de um serviço (entrada da factory).</summary>
public sealed record DadosServico(
    string Descricao,
    decimal PrecoVenda,
    string Unidade,
    string? CodigoInterno = null);

/// <summary>
/// Serviço = cadastro próprio, separado de <see cref="Produto"/> (mercadoria) — seção 6.2
/// ("cada papel tem entidade/tabela própria"). Campos mínimos agora; a identidade fiscal do
/// serviço (código de serviço LC116/ISS para NFS-e) entra com o módulo Fiscal.
/// </summary>
public sealed class Servico : EntidadeBase
{
    /// <summary>Código interno/referência do serviço (opcional; único por empresa quando informado).</summary>
    public string? CodigoInterno { get; private set; }

    public string Descricao { get; private set; } = default!;

    /// <summary>Sigla da unidade de medida (ref. <c>cad_unidades</c>) — ex.: HR, UN.</summary>
    public string Unidade { get; private set; } = default!;

    /// <summary>Preço base/referência. Múltiplos preços virão das Tabelas de Preço.</summary>
    public decimal PrecoVenda { get; private set; }

    /// <summary>Status de negócio: inativo continua visível, mas bloqueado. Difere do soft delete.</summary>
    public bool Ativo { get; private set; } = true;

    private Servico() { }

    public static Result<Servico> Criar(string empresaId, DadosServico dados)
    {
        var validacao = Validar(empresaId, dados);
        if (validacao.Falhou)
            return Result<Servico>.Falha(validacao.Erro!);

        var servico = new Servico { EmpresaId = empresaId };
        servico.AplicarDados(dados);
        return Result<Servico>.Ok(servico);
    }

    public Result Atualizar(DadosServico dados)
    {
        var validacao = Validar(EmpresaId, dados);
        if (validacao.Falhou)
            return validacao;

        AplicarDados(dados);
        MarcarAtualizado();
        return Result.Ok();
    }

    private static Result Validar(string empresaId, DadosServico dados)
    {
        if (string.IsNullOrWhiteSpace(empresaId))
            return Result.Falha("EmpresaId é obrigatório.");
        if (string.IsNullOrWhiteSpace(dados.Descricao))
            return Result.Falha("Descrição é obrigatória.");
        if (string.IsNullOrWhiteSpace(dados.Unidade))
            return Result.Falha("Unidade é obrigatória.");
        if (dados.PrecoVenda < 0)
            return Result.Falha("Preço não pode ser negativo.");

        return Result.Ok();
    }

    private void AplicarDados(DadosServico dados)
    {
        CodigoInterno = string.IsNullOrWhiteSpace(dados.CodigoInterno) ? null : dados.CodigoInterno.Trim();
        Descricao = dados.Descricao.Trim();
        Unidade = dados.Unidade.Trim().ToUpperInvariant();
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
}
