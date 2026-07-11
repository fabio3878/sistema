using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Plataforma.Dominio;

namespace Plataforma.Infraestrutura.Auditoria;

/// <summary>
/// Registra a trilha de "quem alterou o quê" na mesma transação da alteração. Roda no
/// <c>SavingChanges</c> de todo DbContext (plugado em AdicionarDbContextConfiguravel), lê o
/// ChangeTracker para comparar valor antigo × novo e grava um <see cref="RegistroAuditoria"/>
/// no próprio contexto (tabela por módulo). Como as PKs são ULID da aplicação, o Id do registro
/// já existe antes do insert — um único passo basta.
/// </summary>
public sealed class AuditoriaInterceptor(IContextoUsuario usuario, IContextoEmpresa empresa)
    : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions OpcoesJson =
        new() { Converters = { new JsonStringEnumConverter() } };

    // Ruído de sync que nunca vira linha de auditoria por si só.
    private static readonly string[] SempreIgnorar =
        [nameof(EntidadeBase.Versao), nameof(EntidadeBase.AtualizadoEm)];

    private static readonly ConcurrentDictionary<Type, HashSet<string>> IgnoradasPorTipo = new();

    // Se o contexto mapeou RegistroAuditoria (cacheado por tipo de contexto).
    private static readonly ConcurrentDictionary<Type, bool> TrilhaMapeadaPorContexto = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Auditar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Auditar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void Auditar(DbContext? contexto)
    {
        if (contexto is null) return;

        // Blindagem: só audita contextos que mapearam a tabela de trilha. Um módulo que esqueceu
        // de chamar ConfigurarAuditoria não quebra os saves — apenas não gera trilha.
        if (!TrilhaMapeada(contexto)) return;

        var agora = DateTimeOffset.UtcNow;

        // Materializa antes de mexer no tracker (AddRange no fim modificaria a coleção).
        var alvos = contexto.ChangeTracker.Entries<EntidadeBase>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var registros = new List<RegistroAuditoria>();

        foreach (var entry in alvos)
        {
            var entidade = (EntidadeBase)entry.Entity;
            var ignorar = PropsIgnoradas(entry.Metadata.ClrType);
            var diff = new Dictionary<string, object?>();
            OperacaoAuditoria operacao;

            switch (entry.State)
            {
                case EntityState.Added:
                    operacao = OperacaoAuditoria.Criacao;
                    foreach (var p in entry.Properties)
                        if (!ignorar.Contains(p.Metadata.Name))
                            diff[p.Metadata.Name] = new { de = (object?)null, para = p.CurrentValue };
                    break;

                case EntityState.Deleted:
                    operacao = OperacaoAuditoria.Exclusao;
                    foreach (var p in entry.Properties)
                        if (!ignorar.Contains(p.Metadata.Name))
                            diff[p.Metadata.Name] = new { de = p.OriginalValue, para = (object?)null };
                    break;

                default: // Modified
                    foreach (var p in entry.Properties)
                    {
                        if (!p.IsModified || ignorar.Contains(p.Metadata.Name)) continue;
                        if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                        diff[p.Metadata.Name] = new { de = p.OriginalValue, para = p.CurrentValue };
                    }
                    // Soft delete (Excluido virou true) é registrado como Exclusão, não Alteração.
                    operacao = diff.ContainsKey(nameof(EntidadeBase.Excluido)) && entidade.Excluido
                        ? OperacaoAuditoria.Exclusao
                        : OperacaoAuditoria.Alteracao;
                    break;
            }

            // Update que só mexeu em Versao/AtualizadoEm não gera trilha.
            if (diff.Count == 0) continue;

            registros.Add(new RegistroAuditoria
            {
                EmpresaId = string.IsNullOrEmpty(entidade.EmpresaId) ? empresa.EmpresaId : entidade.EmpresaId,
                OcorridoEm = agora,
                UsuarioId = usuario.UsuarioId,
                UsuarioLogin = usuario.Login,
                Entidade = entry.Metadata.ClrType.Name,
                RegistroId = entidade.Id,
                Operacao = operacao,
                Alteracoes = JsonSerializer.Serialize(diff, OpcoesJson),
            });
        }

        if (registros.Count > 0)
            contexto.Set<RegistroAuditoria>().AddRange(registros);
    }

    private static bool TrilhaMapeada(DbContext contexto) =>
        TrilhaMapeadaPorContexto.GetOrAdd(
            contexto.GetType(),
            _ => contexto.Model.FindEntityType(typeof(RegistroAuditoria)) is not null);

    private static HashSet<string> PropsIgnoradas(Type tipo) =>
        IgnoradasPorTipo.GetOrAdd(tipo, t =>
        {
            var set = new HashSet<string>(SempreIgnorar);
            foreach (var p in t.GetProperties())
                if (p.GetCustomAttribute<NaoAuditarAttribute>() is not null)
                    set.Add(p.Name);
            return set;
        });
}
