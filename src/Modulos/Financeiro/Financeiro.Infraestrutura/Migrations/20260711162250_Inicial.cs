using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financeiro.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fin_auditoria",
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
                    table.PrimaryKey("PK_fin_auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_contas_receber",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ClienteId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TipoOrigem = table.Column<int>(type: "integer", nullable: false),
                    DocumentoOrigem = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    NumeroDocumento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: false),
                    CategoriaFinanceira = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UsuarioResponsavelId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_contas_receber", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_formas_pagamento",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_fin_formas_pagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_parametros",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    JurosMoraMensalPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    MultaPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_parametros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fin_parcelas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ContaReceberId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    TotalParcelas = table.Column<int>(type: "integer", nullable: false),
                    ValorOriginal = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPrevistaRecebimento = table.Column<DateOnly>(type: "date", nullable: true),
                    PercentualJurosOverride = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    TotalPago = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Cancelada = table.Column<bool>(type: "boolean", nullable: false),
                    Renegociada = table.Column<bool>(type: "boolean", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_parcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_parcelas_fin_contas_receber_ContaReceberId",
                        column: x => x.ContaReceberId,
                        principalTable: "fin_contas_receber",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fin_recebimentos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    ParcelaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorRecebido = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Desconto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Juros = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Multa = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Acrescimos = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FormaPagamentoId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true),
                    Estornado = table.Column<bool>(type: "boolean", nullable: false),
                    EstornadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstornoMotivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fin_recebimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fin_recebimentos_fin_parcelas_ParcelaId",
                        column: x => x.ParcelaId,
                        principalTable: "fin_parcelas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fin_auditoria_EmpresaId_Entidade_RegistroId",
                table: "fin_auditoria",
                columns: new[] { "EmpresaId", "Entidade", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_auditoria_OcorridoEm",
                table: "fin_auditoria",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_fin_contas_receber_EmpresaId",
                table: "fin_contas_receber",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_contas_receber_EmpresaId_ClienteId",
                table: "fin_contas_receber",
                columns: new[] { "EmpresaId", "ClienteId" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_contas_receber_EmpresaId_DataEmissao",
                table: "fin_contas_receber",
                columns: new[] { "EmpresaId", "DataEmissao" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_formas_pagamento_EmpresaId",
                table: "fin_formas_pagamento",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_formas_pagamento_EmpresaId_Nome",
                table: "fin_formas_pagamento",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fin_parametros_EmpresaId",
                table: "fin_parametros",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fin_parcelas_ContaReceberId",
                table: "fin_parcelas",
                column: "ContaReceberId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_parcelas_EmpresaId",
                table: "fin_parcelas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_parcelas_EmpresaId_ContaReceberId",
                table: "fin_parcelas",
                columns: new[] { "EmpresaId", "ContaReceberId" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_parcelas_EmpresaId_Vencimento",
                table: "fin_parcelas",
                columns: new[] { "EmpresaId", "Vencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_recebimentos_EmpresaId",
                table: "fin_recebimentos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_fin_recebimentos_EmpresaId_ParcelaId",
                table: "fin_recebimentos",
                columns: new[] { "EmpresaId", "ParcelaId" });

            migrationBuilder.CreateIndex(
                name: "IX_fin_recebimentos_ParcelaId",
                table: "fin_recebimentos",
                column: "ParcelaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fin_auditoria");

            migrationBuilder.DropTable(
                name: "fin_formas_pagamento");

            migrationBuilder.DropTable(
                name: "fin_parametros");

            migrationBuilder.DropTable(
                name: "fin_recebimentos");

            migrationBuilder.DropTable(
                name: "fin_parcelas");

            migrationBuilder.DropTable(
                name: "fin_contas_receber");
        }
    }
}
