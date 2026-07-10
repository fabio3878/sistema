using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadastros.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class ProdutosEServicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cad_produtos_EmpresaId_Sku",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "cad_produtos");

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "cad_produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Cest",
                table: "cad_produtos",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoInterno",
                table: "cad_produtos",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Origem",
                table: "cad_produtos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unidade",
                table: "cad_produtos",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "cad_servicos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Unidade = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_cad_servicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cad_unidades",
                columns: table => new
                {
                    Sigla = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CasasDecimais = table.Column<int>(type: "integer", nullable: false),
                    Fracionavel = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_unidades", x => x.Sigla);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cad_produtos_EmpresaId_CodigoInterno",
                table: "cad_produtos",
                columns: new[] { "EmpresaId", "CodigoInterno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cad_servicos_EmpresaId",
                table: "cad_servicos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_servicos_EmpresaId_CodigoInterno",
                table: "cad_servicos",
                columns: new[] { "EmpresaId", "CodigoInterno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cad_servicos");

            migrationBuilder.DropTable(
                name: "cad_unidades");

            migrationBuilder.DropIndex(
                name: "IX_cad_produtos_EmpresaId_CodigoInterno",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "Cest",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "CodigoInterno",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "cad_produtos");

            migrationBuilder.DropColumn(
                name: "Unidade",
                table: "cad_produtos");

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "cad_produtos",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_cad_produtos_EmpresaId_Sku",
                table: "cad_produtos",
                columns: new[] { "EmpresaId", "Sku" },
                unique: true);
        }
    }
}
