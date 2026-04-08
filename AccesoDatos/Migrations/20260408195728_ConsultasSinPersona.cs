using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class ConsultasSinPersona : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Personas_PersonaId",
                table: "Consultas");

            migrationBuilder.DropIndex(
                name: "IX_Consultas_PersonaId",
                table: "Consultas");

            migrationBuilder.DropColumn(
                name: "PersonaId",
                table: "Consultas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonaId",
                table: "Consultas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_PersonaId",
                table: "Consultas",
                column: "PersonaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Personas_PersonaId",
                table: "Consultas",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
