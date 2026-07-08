# Sistema de Automação Comercial

Modular monolith **cliente-servidor** em .NET 10. Um produto, várias verticais (frentes).
Modelo principal: um **servidor da loja** (PostgreSQL) e os **PDVs como clientes na LAN** —
vários caixas compartilham a mesma fonte de verdade (estoque, preços) em tempo real.
Contingência offline por PDV (SQLite local) é evolução futura, ainda não construída.
Documento de arquitetura completo: `ARQUITETURA_1.md`.

## Regras inegociáveis
- **PK = ULID** (string). NUNCA autoincrement (colidiria entre lojas na consolidação e entre
  PDVs no modo offline futuro; ULID é gerado pela aplicação, não pelo banco).
- **Soft delete** (`Excluido=true`) no que sincroniza. NUNCA DELETE físico.
- Toda entidade sincronizável herda `EntidadeBase` (Id, EmpresaId, CriadoEm,
  AtualizadoEm, Versao, Excluido, OrigemId).
- Toda query filtra por `EmpresaId` (multi-tenant). Filtro global no EF: `!e.Excluido`.
- Módulos só se comunicam por `*.Contratos` (interfaces + DTOs + eventos) e por
  eventos do Wolverine. **PROIBIDO** referenciar `Dominio`/`Infraestrutura` de outro módulo.
- A venda fecha no **servidor da loja** (Postgres, transação). Fiscal é assíncrono (não trava o caixa).
- Provider de banco é **por configuração** (seção `Banco`): `Postgres` (servidor) ou `Sqlite`
  (contingência local futura). A escolha mora só em `AdicionarDbContextConfiguravel` e no outbox.
- Bus + outbox = **Wolverine** (MIT). NÃO usar MediatR/MassTransit (viraram comerciais).
- `Result`/`Result<T>` para fluxo de domínio; exceção só para erro inesperado.

## Stack (versões fixadas em Directory.Packages.props)
- .NET 10.0.9 · EF Core 10.0.9 (Postgres no servidor da loja; SQLite = contingência local futura)
- Wolverine 6.16.0 (`WolverineFx`, `.Postgresql`, `.Sqlite`, `.Http`)
- Ulid 1.4.1 · NetArchTest.Rules 1.3.2 · xUnit
- Central Package Management ligado: `.csproj` referenciam pacote **sem** `Version=`.

## Estrutura
```
src/Compartilhado/BuildingBlocks           EntidadeBase, Result, IModulo, eventos base
src/Plataforma/{Dominio,Aplicacao,Infraestrutura}   shared kernel (licença, tenant)
src/Modulos/<Nome>/{Contratos,Dominio,Aplicacao,Infraestrutura}
src/Frentes/Frente.<Vertical>              (ainda não criadas)
src/Hosts/AgenteLocal                      servidor da loja: Wolverine + módulos + HTTP /health
tests/<X>.Tests                            unidade
tests/Arquitetura.Tests                    fronteiras (NetArchTest) — falha o build se violar
```

## Regra de referência entre projetos (a fronteira que protege tudo)
- `*.Contratos` → só BuildingBlocks.
- `*.Dominio` → BuildingBlocks + `*.Contratos` de outros. Nunca Dominio/Infra alheios.
- `*.Aplicacao` → seu Dominio + `*.Contratos` de outros.
- `*.Infraestrutura` → tudo do próprio módulo.
- Host → Contratos + Infraestrutura dos módulos que hospeda.
`tests/Arquitetura.Tests` impõe isso automaticamente.

## Ao adicionar um módulo
1. Crie os 4 projetos (Contratos, Dominio, Aplicacao, Infraestrutura) e adicione ao `.sln`.
2. Implemente `IModulo` para auto-registro no host.
3. `DbContext` próprio, tabelas com prefixo do módulo (`cad_`, `est_`, `fin_`...).
4. Publique eventos em Contratos; consuma via handlers Wolverine.
5. Adicione teste de arquitetura confirmando que não vaza internals.

## Comandos
Usar o SDK .NET 10 (instalado em `%LOCALAPPDATA%\Microsoft\dotnet`, já no PATH do usuário).
```
dotnet build                                    # compila a solução
dotnet test                                     # roda testes (inclui os de arquitetura)
dotnet run --project src/Hosts/AgenteLocal      # sobe o host; GET /health
# nova migration do Cadastros:
dotnet ef migrations add <Nome> \
  --project src/Modulos/Cadastros/Cadastros.Infraestrutura \
  --startup-project src/Hosts/AgenteLocal
```
