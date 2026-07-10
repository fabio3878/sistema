using Cadastros.Dominio;

namespace Cadastros.Infraestrutura;

/// <summary>
/// Semeia a tabela de referência global de unidades de medida (<c>cad_unidades</c>) a partir de
/// uma lista fixa em código. Idempotente: só roda se a tabela estiver vazia. Chamado no startup
/// do host, após as migrations. <c>CasasDecimais</c> governa a exibição da quantidade por unidade.
/// </summary>
public sealed class SeederUnidades(IUnidadeRepositorio unidades)
{
    public async Task ExecutarAsync(CancellationToken ct = default)
    {
        if (!await unidades.Vazio(ct)) return; // já semeado
        await unidades.SemearAsync(Unidades(), ct);
    }

    private static IEnumerable<Unidade> Unidades() =>
    [
        Unidade.Criar("UN", "Unidade", 0, fracionavel: false),
        Unidade.Criar("HR", "Hora", 2, fracionavel: true),
        Unidade.Criar("PC", "Peça", 0, fracionavel: false),
        Unidade.Criar("CX", "Caixa", 0, fracionavel: false),
        Unidade.Criar("DZ", "Dúzia", 0, fracionavel: false),
        Unidade.Criar("PAR", "Par", 0, fracionavel: false),
        Unidade.Criar("PCT", "Pacote", 0, fracionavel: false),
        Unidade.Criar("ROL", "Rolo", 0, fracionavel: false),
        Unidade.Criar("KG", "Quilograma", 3, fracionavel: true),
        Unidade.Criar("G", "Grama", 3, fracionavel: true),
        Unidade.Criar("L", "Litro", 3, fracionavel: true),
        Unidade.Criar("ML", "Mililitro", 0, fracionavel: false),
        Unidade.Criar("M", "Metro", 3, fracionavel: true),
        Unidade.Criar("CM", "Centímetro", 0, fracionavel: false),
        Unidade.Criar("M2", "Metro quadrado", 3, fracionavel: true),
        Unidade.Criar("M3", "Metro cúbico", 3, fracionavel: true),
    ];
}
