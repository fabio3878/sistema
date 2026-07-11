namespace BuildingBlocks;

/// <summary>Tipo de operação registrada na trilha de auditoria.</summary>
public enum OperacaoAuditoria
{
    /// <summary>Insert: registro criado.</summary>
    Criacao = 0,

    /// <summary>Update: um ou mais campos alterados.</summary>
    Alteracao = 1,

    /// <summary>Soft delete (Excluido=true): registro tombado.</summary>
    Exclusao = 2,
}
