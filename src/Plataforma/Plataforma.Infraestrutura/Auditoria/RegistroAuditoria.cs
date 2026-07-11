using BuildingBlocks;

namespace Plataforma.Infraestrutura.Auditoria;

/// <summary>
/// Uma linha da trilha de auditoria — "quem alterou o quê". Gravada pelo
/// <see cref="AuditoriaInterceptor"/> no MESMO DbContext/transação da alteração, em uma tabela
/// por módulo (cad_auditoria, acs_auditoria...). É <b>append-only</b>: não herda
/// <see cref="EntidadeBase"/> (sem soft delete, sem versão, sem carimbo imutável) — trilha não
/// sofre update nem delete.
/// </summary>
public sealed class RegistroAuditoria
{
    /// <summary>PK = ULID (ordenável por tempo), gerado pela aplicação.</summary>
    public string Id { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>Tenant da operação.</summary>
    public string EmpresaId { get; set; } = default!;

    /// <summary>Instante da ocorrência (UTC).</summary>
    public DateTimeOffset OcorridoEm { get; set; }

    /// <summary>Id do usuário autenticado. Nulo fora de HTTP (seed, fila).</summary>
    public string? UsuarioId { get; set; }

    /// <summary>Login do usuário (desnormalizado para leitura da trilha).</summary>
    public string? UsuarioLogin { get; set; }

    /// <summary>Nome do tipo CLR da entidade alterada (ex.: "Cliente").</summary>
    public string Entidade { get; set; } = default!;

    /// <summary>Id (ULID) do registro alterado.</summary>
    public string RegistroId { get; set; } = default!;

    /// <summary>Operação registrada.</summary>
    public OperacaoAuditoria Operacao { get; set; }

    /// <summary>
    /// Diff em JSON. Alteração: <c>{ "campo": { "de": antigo, "para": novo } }</c>.
    /// Criação: snapshot dos campos (de = null). Exclusão: idem alteração (o campo Excluido).
    /// </summary>
    public string Alteracoes { get; set; } = default!;
}
