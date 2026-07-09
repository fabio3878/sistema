using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acesso.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acs_refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    StampSeguranca = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevogadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubstituidoPorId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    MotivoRevogacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acs_refresh_tokens_EmpresaId",
                table: "acs_refresh_tokens",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_refresh_tokens_EmpresaId_TokenHash",
                table: "acs_refresh_tokens",
                columns: new[] { "EmpresaId", "TokenHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acs_refresh_tokens_EmpresaId_UsuarioId",
                table: "acs_refresh_tokens",
                columns: new[] { "EmpresaId", "UsuarioId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acs_refresh_tokens");
        }
    }
}
