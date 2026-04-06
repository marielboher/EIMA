using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class ConsultaMateriaTexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Materias_MateriaId",
                table: "Consultas");

            migrationBuilder.DropIndex(
                name: "IX_Consultas_MateriaId",
                table: "Consultas");

            migrationBuilder.AddColumn<string>(
                name: "Materia",
                table: "Consultas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE c
                SET c.Materia = m.Nombre
                FROM Consultas c
                INNER JOIN Materias m ON m.Id = c.MateriaId;

                UPDATE Consultas SET Materia = N'' WHERE Materia IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "MateriaId",
                table: "Consultas");

            migrationBuilder.AlterColumn<string>(
                name: "Materia",
                table: "Consultas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Materia",
                table: "Consultas");

            migrationBuilder.AddColumn<int>(
                name: "MateriaId",
                table: "Consultas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Consultas_MateriaId",
                table: "Consultas",
                column: "MateriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Materias_MateriaId",
                table: "Consultas",
                column: "MateriaId",
                principalTable: "Materias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
