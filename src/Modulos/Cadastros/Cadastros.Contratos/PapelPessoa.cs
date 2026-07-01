namespace Cadastros.Contratos;

/// <summary>
/// Papéis que uma mesma Pessoa pode acumular (seção 6.2). Uma Pessoa unificada evita
/// cadastrar o mesmo CPF/CNPJ três vezes. É [Flags]: combina papéis (Cliente | Fornecedor).
/// </summary>
[Flags]
public enum PapelPessoa
{
    Nenhum = 0,
    Cliente = 1,
    Fornecedor = 2,
    Funcionario = 4,
    Transportadora = 8,
}
