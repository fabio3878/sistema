using System.Text.Json;
using Cadastros.Dominio;

namespace Cadastros.Infraestrutura;

/// <summary>
/// Semeia as tabelas de referência global do IBGE (estados + municípios) a partir de um dataset
/// embarcado (`Recursos/municipios.json`). Idempotente: só roda se as tabelas estiverem vazias.
/// Funciona offline — não depende de rede. Chamado no startup do host, após as migrations.
/// </summary>
public sealed class SeederLocalidades(ILocalidadeRepositorio localidades)
{
    private sealed record MunicipioJson(string codigo, string nome, string uf);

    public async Task ExecutarAsync(CancellationToken ct = default)
    {
        if (!await localidades.Vazio(ct)) return; // já semeado

        var asm = typeof(SeederLocalidades).Assembly;
        var recurso = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("municipios.json"))
            ?? throw new InvalidOperationException("Recurso municipios.json não encontrado no assembly.");

        await using var stream = asm.GetManifestResourceStream(recurso)!;
        var itens = await JsonSerializer.DeserializeAsync<List<MunicipioJson>>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct) ?? [];

        var municipios = itens.Select(m => Municipio.Criar(m.codigo, m.nome, m.uf)).ToList();
        await localidades.SemearAsync(Estados(), municipios, ct);
    }

    /// <summary>As 27 unidades federativas (UF, nome, código IBGE de 2 dígitos).</summary>
    private static IEnumerable<Estado> Estados() =>
    [
        Estado.Criar("RO", "Rondônia", "11"),
        Estado.Criar("AC", "Acre", "12"),
        Estado.Criar("AM", "Amazonas", "13"),
        Estado.Criar("RR", "Roraima", "14"),
        Estado.Criar("PA", "Pará", "15"),
        Estado.Criar("AP", "Amapá", "16"),
        Estado.Criar("TO", "Tocantins", "17"),
        Estado.Criar("MA", "Maranhão", "21"),
        Estado.Criar("PI", "Piauí", "22"),
        Estado.Criar("CE", "Ceará", "23"),
        Estado.Criar("RN", "Rio Grande do Norte", "24"),
        Estado.Criar("PB", "Paraíba", "25"),
        Estado.Criar("PE", "Pernambuco", "26"),
        Estado.Criar("AL", "Alagoas", "27"),
        Estado.Criar("SE", "Sergipe", "28"),
        Estado.Criar("BA", "Bahia", "29"),
        Estado.Criar("MG", "Minas Gerais", "31"),
        Estado.Criar("ES", "Espírito Santo", "32"),
        Estado.Criar("RJ", "Rio de Janeiro", "33"),
        Estado.Criar("SP", "São Paulo", "35"),
        Estado.Criar("PR", "Paraná", "41"),
        Estado.Criar("SC", "Santa Catarina", "42"),
        Estado.Criar("RS", "Rio Grande do Sul", "43"),
        Estado.Criar("MS", "Mato Grosso do Sul", "50"),
        Estado.Criar("MT", "Mato Grosso", "51"),
        Estado.Criar("GO", "Goiás", "52"),
        Estado.Criar("DF", "Distrito Federal", "53"),
    ];
}
