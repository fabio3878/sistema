namespace Acesso.Contratos;

/// <summary>Credenciais de login.</summary>
public sealed record LoginRequest(string Login, string Senha);

/// <summary>Pedido de renovação com o refresh token bruto.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Pedido de logout (revoga o refresh token informado).</summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>Troca de senha do usuário autenticado.</summary>
public sealed record TrocarSenhaRequest(string SenhaAtual, string SenhaNova);

/// <summary>Par de tokens emitido no login/refresh (o refresh vem em texto, só desta vez).</summary>
public sealed record TokenResposta(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiraEm,
    bool DeveTrocarSenha);
