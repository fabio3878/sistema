using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadastros.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class ExpansaoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceitaEmail",
                table: "cad_clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceitaLigacoes",
                table: "cad_clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceitaSms",
                table: "cad_clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceitaWhatsapp",
                table: "cad_clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AceitouTermosLgpd",
                table: "cad_clientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Celular",
                table: "cad_clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataAceiteLgpd",
                table: "cad_clientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailFinanceiro",
                table: "cad_clientes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "cad_clientes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Preferencias",
                table: "cad_clientes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Site",
                table: "cad_clientes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Whatsapp",
                table: "cad_clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cad_estados",
                columns: table => new
                {
                    Uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoIbge = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_estados", x => x.Uf);
                });

            migrationBuilder.CreateTable(
                name: "cad_municipios",
                columns: table => new
                {
                    CodigoIbge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cad_municipios", x => x.CodigoIbge);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cad_municipios_Uf",
                table: "cad_municipios",
                column: "Uf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cad_estados");

            migrationBuilder.DropTable(
                name: "cad_municipios");

            migrationBuilder.DropColumn(
                name: "AceitaEmail",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "AceitaLigacoes",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "AceitaSms",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "AceitaWhatsapp",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "AceitouTermosLgpd",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "Celular",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "DataAceiteLgpd",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "EmailFinanceiro",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "Preferencias",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "Site",
                table: "cad_clientes");

            migrationBuilder.DropColumn(
                name: "Whatsapp",
                table: "cad_clientes");
        }
    }
}
