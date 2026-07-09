namespace BuildingBlocks;

/// <summary>
/// Declaração, em CÓDIGO, de uma funcionalidade que um módulo sabe fazer (fonte da verdade).
/// O host agrega o manifesto de todos os módulos ativos e o módulo Acesso reconcilia isso
/// para as tabelas de catálogo (acs_modulos/acs_funcionalidades) no startup. Capacidade é
/// contrato do software — muda no deploy, não é dado editável por empresa.
/// </summary>
/// <param name="Codigo">Código estável e único da funcionalidade (ex.: "cad.cliente.criar").</param>
/// <param name="ModuloCodigo">Código do módulo agrupador (ex.: "cad").</param>
/// <param name="ModuloNome">Nome amigável do módulo (ex.: "Cadastros").</param>
/// <param name="Nome">Nome amigável da funcionalidade (ex.: "Criar cliente").</param>
/// <param name="Descricao">Descrição para a tela de permissões.</param>
public sealed record FuncionalidadeManifesto(
    string Codigo,
    string ModuloCodigo,
    string ModuloNome,
    string Nome,
    string Descricao);
