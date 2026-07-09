using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadastros.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cad_clientes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    TipoPessoa = table.Column<int>(type: "integer", nullable: false),
                    NomeFantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Rg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrgaoEmissorRg = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IndicadorIe = table.Column<int>(type: "integer", nullable: false),
                    RegimeTributario = table.Column<int>(type: "integer", nullable: true),
                    LimiteCredito = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cad_produtos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Sku = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cad_cliente_enderecos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ClienteId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    CodigoIbgeMunicipio = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Pais = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_cliente_enderecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cad_cliente_enderecos_cad_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "cad_clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cad_cliente_enderecos_ClienteId",
                table: "cad_cliente_enderecos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_cliente_enderecos_EmpresaId",
                table: "cad_cliente_enderecos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_cliente_enderecos_EmpresaId_ClienteId",
                table: "cad_cliente_enderecos",
                columns: new[] { "EmpresaId", "ClienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_cad_clientes_EmpresaId",
                table: "cad_clientes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_clientes_EmpresaId_Documento",
                table: "cad_clientes",
                columns: new[] { "EmpresaId", "Documento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cad_produtos_EmpresaId",
                table: "cad_produtos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_produtos_EmpresaId_Sku",
                table: "cad_produtos",
                columns: new[] { "EmpresaId", "Sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cad_cliente_enderecos");

            migrationBuilder.DropTable(
                name: "cad_produtos");

            migrationBuilder.DropTable(
                name: "cad_clientes");
        }
    }
}
