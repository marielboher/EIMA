using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class RecuperacionContrasenaTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokensRecuperacionContrasena",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaUsuarioId = table.Column<int>(type: "int", nullable: false),
                    HashToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumidoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRecuperacionContrasena", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRecuperacionContrasena_CuentasUsuario_CuentaUsuarioId",
                        column: x => x.CuentaUsuarioId,
                        principalTable: "CuentasUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacionContrasena_CuentaUsuarioId",
                table: "TokensRecuperacionContrasena",
                column: "CuentaUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRecuperacionContrasena_HashToken",
                table: "TokensRecuperacionContrasena",
                column: "HashToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokensRecuperacionContrasena");
        }
    }
}
