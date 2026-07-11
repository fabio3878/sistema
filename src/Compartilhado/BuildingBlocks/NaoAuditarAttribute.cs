namespace BuildingBlocks;

/// <summary>
/// Marca uma propriedade que a trilha de auditoria NUNCA deve serializar (ex.: hash de senha,
/// hash de token). O interceptor de auditoria ignora colunas anotadas ao montar o diff.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NaoAuditarAttribute : Attribute;
