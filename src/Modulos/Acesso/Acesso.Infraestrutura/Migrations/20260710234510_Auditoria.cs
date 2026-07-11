using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acesso.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Auditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acs_auditoria",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    UsuarioLogin = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Entidade = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RegistroId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Operacao = table.Column<int>(type: "integer", nullable: false),
                    Alteracoes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_auditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acs_auditoria_EmpresaId_Entidade_RegistroId",
                table: "acs_auditoria",
                columns: new[] { "EmpresaId", "Entidade", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_acs_auditoria_OcorridoEm",
                table: "acs_auditoria",
                column: "OcorridoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acs_auditoria");
        }
    }
}
