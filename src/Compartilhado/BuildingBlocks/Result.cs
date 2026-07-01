namespace BuildingBlocks;

/// <summary>
/// Resultado de uma operação de domínio (seção 4.2). Preferir a exceção para fluxo
/// esperado (validação, regra de negócio). Exceção só para o inesperado.
/// </summary>
public readonly record struct Result(bool Sucesso, string? Erro)
{
    public static Result Ok() => new(true, null);
    public static Result Falha(string erro) => new(false, erro);

    public bool Falhou => !Sucesso;
}

/// <summary>Resultado que carrega um valor em caso de sucesso.</summary>
public readonly record struct Result<T>(bool Sucesso, T? Valor, string? Erro)
{
    public static Result<T> Ok(T valor) => new(true, valor, null);
    public static Result<T> Falha(string erro) => new(false, default, erro);

    public bool Falhou => !Sucesso;
}
