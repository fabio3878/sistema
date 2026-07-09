using BuildingBlocks;

namespace Acesso.Contratos;

/// <summary>Fato: um usuário foi criado. Consumível por outros módulos via Wolverine.</summary>
public sealed record UsuarioCriado(string EmpresaId, string UsuarioId, string Login) : IEventoDominio;
