using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposProfesorMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantAlumnos",
                table: "ProfesoresMaterias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CantHoras",
                table: "ProfesoresMaterias",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorHora",
                table: "ProfesoresMaterias",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantAlumnos",
                table: "ProfesoresMaterias");

            migrationBuilder.DropColumn(
                name: "CantHoras",
                table: "ProfesoresMaterias");

            migrationBuilder.DropColumn(
                name: "ValorHora",
                table: "ProfesoresMaterias");
        }
    }
}
