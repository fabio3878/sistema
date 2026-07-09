using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acesso.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "acs_modulos",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_modulos", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "acs_perfis",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Protegido = table.Column<bool>(type: "boolean", nullable: false),
                    ConcedeTodas = table.Column<bool>(type: "boolean", nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_perfis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "acs_usuarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LoginNormalizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeExibicao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SenhaHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenhaAlteradaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DeveTrocarSenha = table.Column<bool>(type: "boolean", nullable: false),
                    UltimoLoginEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StampSeguranca = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "acs_funcionalidades",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModuloCodigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Obsoleta = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_funcionalidades", x => x.Codigo);
                    table.ForeignKey(
                        name: "FK_acs_funcionalidades_acs_modulos_ModuloCodigo",
                        column: x => x.ModuloCodigo,
                        principalTable: "acs_modulos",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acs_perfil_funcionalidades",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    PerfilId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    FuncionalidadeCodigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_perfil_funcionalidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acs_perfil_funcionalidades_acs_perfis_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "acs_perfis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "acs_usuario_perfis",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    PerfilId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    EmpresaId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Versao = table.Column<long>(type: "bigint", nullable: false),
                    Excluido = table.Column<bool>(type: "boolean", nullable: false),
                    OrigemId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acs_usuario_perfis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_acs_usuario_perfis_acs_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "acs_usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acs_funcionalidades_ModuloCodigo",
                table: "acs_funcionalidades",
                column: "ModuloCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_acs_perfil_funcionalidades_EmpresaId",
                table: "acs_perfil_funcionalidades",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_perfil_funcionalidades_EmpresaId_PerfilId_Funcionalidad~",
                table: "acs_perfil_funcionalidades",
                columns: new[] { "EmpresaId", "PerfilId", "FuncionalidadeCodigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acs_perfil_funcionalidades_PerfilId",
                table: "acs_perfil_funcionalidades",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_perfis_EmpresaId",
                table: "acs_perfis",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_perfis_EmpresaId_Nome",
                table: "acs_perfis",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acs_usuario_perfis_EmpresaId",
                table: "acs_usuario_perfis",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_usuario_perfis_EmpresaId_UsuarioId_PerfilId",
                table: "acs_usuario_perfis",
                columns: new[] { "EmpresaId", "UsuarioId", "PerfilId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_acs_usuario_perfis_UsuarioId",
                table: "acs_usuario_perfis",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_usuarios_EmpresaId",
                table: "acs_usuarios",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_acs_usuarios_EmpresaId_LoginNormalizado",
                table: "acs_usuarios",
                columns: new[] { "EmpresaId", "LoginNormalizado" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acs_funcionalidades");

            migrationBuilder.DropTable(
                name: "acs_perfil_funcionalidades");

            migrationBuilder.DropTable(
                name: "acs_usuario_perfis");

            migrationBuilder.DropTable(
                name: "acs_modulos");

            migrationBuilder.DropTable(
                name: "acs_perfis");

            migrationBuilder.DropTable(
                name: "acs_usuarios");
        }
    }
}
