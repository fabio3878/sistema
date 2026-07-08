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
                name: "cad_pessoas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Papeis = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_pessoas", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_cad_pessoas_EmpresaId",
                table: "cad_pessoas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_cad_pessoas_EmpresaId_Documento",
                table: "cad_pessoas",
                columns: new[] { "EmpresaId", "Documento" });

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
                name: "cad_pessoas");

            migrationBuilder.DropTable(
                name: "cad_produtos");
        }
    }
}
