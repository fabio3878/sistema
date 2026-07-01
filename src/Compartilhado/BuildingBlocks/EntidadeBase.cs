namespace BuildingBlocks;

/// <summary>
/// Base de toda entidade sincronizável (seção 4.1 da arquitetura).
/// Os campos de sync existem desde o dia 1 — adicioná-los depois exigiria migração dolorosa.
/// </summary>
public abstract class EntidadeBase
{
    /// <summary>
    /// PK = ULID (string de 26 chars). NUNCA autoincrement: dois PDVs gerando id=1 colidiriam.
    /// ULID é ordenável por tempo, bom para índice.
    /// </summary>
    public string Id { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>Multi-tenant: tudo é particionado por empresa/filial.</summary>
    public string EmpresaId { get; set; } = default!;

    /// <summary>Instante de criação (UTC).</summary>
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Instante da última atualização (UTC). Usado na resolução de conflito de sync.</summary>
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Incrementa a cada update. Ajuda a resolver conflito de sync.</summary>
    public long Versao { get; set; }

    /// <summary>Soft delete (tombstone). NUNCA DELETE físico no que sincroniza.</summary>
    public bool Excluido { get; set; }

    /// <summary>Id do device/loja que originou o registro (rastreio de sync).</summary>
    public string? OrigemId { get; set; }

    /// <summary>Marca o registro como alterado: sobe a versão e o carimbo de tempo.</summary>
    public void MarcarAtualizado()
    {
        AtualizadoEm = DateTimeOffset.UtcNow;
        Versao++;
    }
}
