using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Renegociacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RenegociacaoId",
                table: "fin_parcelas",
                type: "character varying(26)",
                maxLength: 26,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fin_renegociacoes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ContaReceberId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorBase = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Entrada = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorRenegociado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_renegociacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_renegociacoes_fin_contas_receber_ContaReceberId",
                        column: x => x.ContaReceberId,
                        principalTable: "fin_contas_receber",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fin_parcelas_EmpresaId_RenegociacaoId",
                table: "fin_parcelas",
                columns: new[] { "EmpresaId", "RenegociacaoId" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_renegociacoes_ContaReceberId",
                table: "fin_renegociacoes",
                column: "ContaReceberId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_renegociacoes_EmpresaId",
                table: "fin_renegociacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_renegociacoes_EmpresaId_ContaReceberId",
                table: "fin_renegociacoes",
                columns: new[] { "EmpresaId", "ContaReceberId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fin_renegociacoes");

            migrationBuilder.DropIndex(
                name: "IX_fin_parcelas_EmpresaId_RenegociacaoId",
                table: "fin_parcelas");

            migrationBuilder.DropColumn(
                name: "RenegociacaoId",
                table: "fin_parcelas");
        }
    }
}
