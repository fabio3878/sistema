namespace Cadastros.Contratos;

/// <summary>Projeção pública de um Cliente (com seus endereços).</summary>
public sealed record ClienteDto(
    string Id,
    string EmpresaId,
    string Nome,
    string Documento,
    TipoPessoa TipoPessoa,
    string? NomeFantasia,
    string? Email,
    string? EmailFinanceiro,
    string? Telefone,
    string? Celular,
    string? Whatsapp,
    string? Site,
    DateOnly? DataNascimento,
    string? Rg,
    string? OrgaoEmissorRg,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    IndicadorIe IndicadorIe,
    RegimeTributario? RegimeTributario,
    decimal? LimiteCredito,
    string? Origem,
    string? Preferencias,
    string? Observacoes,
    bool AceitaEmail,
    bool AceitaSms,
    bool AceitaWhatsapp,
    bool AceitaLigacoes,
    bool AceitouTermosLgpd,
    DateTimeOffset? DataAceiteLgpd,
    bool Ativo,
    IReadOnlyList<EnderecoDto> Enderecos);

/// <summary>Projeção pública de um endereço de cliente.</summary>
public sealed record EnderecoDto(
    string Id,
    TipoEndereco Tipo,
    string Cep,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Municipio,
    string Uf,
    string CodigoIbgeMunicipio,
    string Pais);

/// <summary>Linha enxuta da listagem de clientes (não carrega os endereços; traz o principal resumido + contagem).</summary>
public sealed record ClienteResumoDto(
    string Id,
    string Nome,
    string Documento,
    TipoPessoa TipoPessoa,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    string? Logradouro,
    string? Numero,
    string? Bairro,
    string? Cidade,
    string? Uf,
    bool Ativo,
    int QtdEnderecos);

/// <summary>Filtros da listagem de clientes. Tudo opcional (nulo = não filtra).</summary>
public sealed record FiltroClientes(
    string? Busca = null,
    string? Cidade = null,
    string? Bairro = null,
    bool? Ativo = null,
    int? MesAniversario = null);

/// <summary>Estado (UF) do IBGE — dado de referência global.</summary>
public sealed record EstadoDto(string Uf, string Nome);

/// <summary>Município do IBGE — dado de referência global.</summary>
public sealed record MunicipioDto(string CodigoIbge, string Nome, string Uf);

/// <summary>Payload de criação/edição de um endereço. <c>Id</c> nulo = endereço novo.</summary>
public sealed record EnderecoEntradaDto(
    TipoEndereco Tipo,
    string Cep,
    string Logradouro,
    string Numero,
    string Bairro,
    string Municipio,
    string Uf,
    string CodigoIbgeMunicipio,
    string? Id = null,
    string? Complemento = null,
    string Pais = "Brasil");

/// <summary>Payload de criação/edição de um Cliente (com a lista completa de endereços).</summary>
public sealed record ClienteEntradaDto(
    string Nome,
    string Documento,
    TipoPessoa TipoPessoa = TipoPessoa.Fisica,
    IndicadorIe IndicadorIe = IndicadorIe.NaoContribuinte,
    string? NomeFantasia = null,
    string? Email = null,
    string? EmailFinanceiro = null,
    string? Telefone = null,
    string? Celular = null,
    string? Whatsapp = null,
    string? Site = null,
    DateOnly? DataNascimento = null,
    string? Rg = null,
    string? OrgaoEmissorRg = null,
    string? InscricaoEstadual = null,
    string? InscricaoMunicipal = null,
    RegimeTributario? RegimeTributario = null,
    decimal? LimiteCredito = null,
    string? Origem = null,
    string? Preferencias = null,
    string? Observacoes = null,
    bool AceitaEmail = false,
    bool AceitaSms = false,
    bool AceitaWhatsapp = false,
    bool AceitaLigacoes = false,
    bool AceitouTermosLgpd = false,
    DateTimeOffset? DataAceiteLgpd = null,
    IReadOnlyList<EnderecoEntradaDto>? Enderecos = null);

/// <summary>Projeção pública de um Produto (mercadoria).</summary>
public sealed record ProdutoDto(
    string Id,
    string EmpresaId,
    string? CodigoInterno,
    string Descricao,
    string? CodigoBarras,
    string Unidade,
    string Ncm,
    string? Cest,
    OrigemMercadoria Origem,
    decimal PrecoVenda,
    bool Ativo);

/// <summary>Linha enxuta da listagem de produtos.</summary>
public sealed record ProdutoResumoDto(
    string Id,
    string? CodigoInterno,
    string Descricao,
    string? CodigoBarras,
    string Unidade,
    decimal PrecoVenda,
    bool Ativo);

/// <summary>Filtros da listagem de produtos. Tudo opcional (nulo = não filtra).</summary>
public sealed record FiltroProdutos(
    string? Busca = null,
    bool? Ativo = null);

/// <summary>Payload de criação/edição de um Produto.</summary>
public sealed record ProdutoEntradaDto(
    string Descricao,
    string Ncm,
    decimal PrecoVenda,
    string Unidade,
    OrigemMercadoria Origem = OrigemMercadoria.Nacional,
    string? CodigoInterno = null,
    string? CodigoBarras = null,
    string? Cest = null);

/// <summary>Projeção pública de um Serviço.</summary>
public sealed record ServicoDto(
    string Id,
    string EmpresaId,
    string? CodigoInterno,
    string Descricao,
    string Unidade,
    decimal PrecoVenda,
    bool Ativo);

/// <summary>Linha enxuta da listagem de serviços.</summary>
public sealed record ServicoResumoDto(
    string Id,
    string? CodigoInterno,
    string Descricao,
    string Unidade,
    decimal PrecoVenda,
    bool Ativo);

/// <summary>Filtros da listagem de serviços. Tudo opcional (nulo = não filtra).</summary>
public sealed record FiltroServicos(
    string? Busca = null,
    bool? Ativo = null);

/// <summary>Payload de criação/edição de um Serviço.</summary>
public sealed record ServicoEntradaDto(
    string Descricao,
    decimal PrecoVenda,
    string Unidade,
    string? CodigoInterno = null);

/// <summary>Unidade de medida (referência global) — para o combobox de produto/serviço.</summary>
public sealed record UnidadeDto(string Sigla, string Descricao, int CasasDecimais, bool Fracionavel);
