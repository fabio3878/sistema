namespace BuildingBlocks;

/// <summary>
/// Coleta os tipos de DbContext que cada módulo quer que o host migre no startup.
/// Fica em BuildingBlocks de propósito SEM depender de EF Core — guarda apenas os
/// <see cref="Type"/>. Quem resolve e aplica a migration é o host (que tem EF).
/// </summary>
public sealed class MigrationRegistry
{
    private readonly List<Type> _contextos = [];

    /// <summary>Registra um DbContext (por tipo) para migração no startup.</summary>
    public void Adicionar<TContexto>() where TContexto : class => _contextos.Add(typeof(TContexto));

    /// <summary>Tipos de DbContext registrados pelos módulos.</summary>
    public IReadOnlyList<Type> Contextos => _contextos;
}
