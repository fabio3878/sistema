using Acesso.Dominio;
using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plataforma.Dominio;

namespace Acesso.Infraestrutura;

/// <summary>
/// Semeadura de acesso no startup, chamada pelo host. Faz duas coisas idempotentes:
/// (1) reconcilia o catálogo (código → tabelas acs_modulos/acs_funcionalidades);
/// (2) no first-run de um tenant, cria o perfil "Administrador" e o usuário admin inicial.
/// </summary>
public sealed class SeederAcesso(
    AcessoDbContext db,
    IHashSenha hashSenha,
    IContextoEmpresa contexto,
    IConfiguration config,
    ILogger<SeederAcesso> logger)
{
    public async Task ExecutarAsync(IReadOnlyList<FuncionalidadeManifesto> manifesto, CancellationToken ct = default)
    {
        await ReconciliarCatalogoAsync(manifesto, ct);
        await SeedAdminAsync(ct);
    }

    /// <summary>Espelha o manifesto de código nas tabelas de catálogo. Não apaga: o que sumiu vira Obsoleta.</summary>
    private async Task ReconciliarCatalogoAsync(IReadOnlyList<FuncionalidadeManifesto> manifesto, CancellationToken ct)
    {
        var modulos = await db.Modulos.ToDictionaryAsync(m => m.Codigo, ct);
        var funcionalidades = await db.Funcionalidades.ToDictionaryAsync(f => f.Codigo, ct);

        // Módulos (deduplicados pelo código).
        foreach (var grupo in manifesto.GroupBy(m => m.ModuloCodigo))
        {
            var nome = grupo.First().ModuloNome;
            if (modulos.TryGetValue(grupo.Key, out var modulo))
                modulo.Atualizar(nome, null);
            else
                await db.Modulos.AddAsync(Modulo.Criar(grupo.Key, nome), ct);
        }

        // Funcionalidades.
        var codigosDoManifesto = new HashSet<string>();
        foreach (var f in manifesto)
        {
            codigosDoManifesto.Add(f.Codigo);
            if (funcionalidades.TryGetValue(f.Codigo, out var existente))
                existente.Atualizar(f.ModuloCodigo, f.Nome, f.Descricao);
            else
                await db.Funcionalidades.AddAsync(Funcionalidade.Criar(f.Codigo, f.ModuloCodigo, f.Nome, f.Descricao), ct);
        }

        // O que existe no banco mas saiu do código vira obsoleto (nunca apaga: grants antigos apontam).
        foreach (var (codigo, funcionalidade) in funcionalidades)
            if (!codigosDoManifesto.Contains(codigo) && !funcionalidade.Obsoleta)
                funcionalidade.MarcarObsoleta();

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Cria o admin do tenant só se ainda não houver nenhum usuário. Segredo vem de config/secret.</summary>
    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var empresaId = contexto.EmpresaId;

        if (await db.Usuarios.AnyAsync(u => u.EmpresaId == empresaId, ct))
            return; // já há usuário; nada a semear

        var login = config["Acesso:AdminInicial:Login"] ?? "admin";
        var senha = config["Acesso:AdminInicial:Senha"];
        if (string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning(
                "Nenhum usuário existe para a empresa {EmpresaId} e 'Acesso:AdminInicial:Senha' não está " +
                "configurado (use user-secrets/variável de ambiente). Admin inicial NÃO foi criado.",
                empresaId);
            return;
        }

        var perfilResult = Perfil.Criar(empresaId, "Administrador", "Acesso total ao sistema.",
            concedeTodas: true, protegido: true);
        if (perfilResult.Falhou)
        {
            logger.LogError("Falha ao criar perfil Administrador: {Erro}", perfilResult.Erro);
            return;
        }

        var usuarioResult = Usuario.Criar(empresaId,
            new DadosUsuario(login, "Administrador", senha, DeveTrocarSenha: true), hashSenha);
        if (usuarioResult.Falhou)
        {
            logger.LogError("Falha ao criar usuário admin inicial: {Erro}", usuarioResult.Erro);
            return;
        }

        var perfil = perfilResult.Valor!;
        var usuario = usuarioResult.Valor!;
        usuario.AtribuirPerfil(perfil.Id);

        await db.Perfis.AddAsync(perfil, ct);
        await db.Usuarios.AddAsync(usuario, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin inicial '{Login}' criado para a empresa {EmpresaId} (deve trocar a senha no primeiro acesso).",
            login, empresaId);
    }
}
