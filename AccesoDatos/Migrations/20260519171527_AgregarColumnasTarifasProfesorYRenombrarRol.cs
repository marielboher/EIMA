using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarColumnasTarifasProfesorYRenombrarRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columnas de tarifas del Profesor
            migrationBuilder.AddColumn<double>(
                name: "CantidadHoras",
                table: "Personas",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimoAlumnosGrupo",
                table: "Personas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDescuentoGrupo",
                table: "Personas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorClasePorHora",
                table: "Personas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCursoCompleto",
                table: "Personas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Renombrar rol "secretaria" -> "administrativo" (término inclusivo)
            migrationBuilder.Sql(
                "UPDATE [Roles] SET [Nombre] = 'administrativo' WHERE [Nombre] = 'secretaria'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CantidadHoras", table: "Personas");
            migrationBuilder.DropColumn(name: "MinimoAlumnosGrupo", table: "Personas");
            migrationBuilder.DropColumn(name: "PorcentajeDescuentoGrupo", table: "Personas");
            migrationBuilder.DropColumn(name: "ValorClasePorHora", table: "Personas");
            migrationBuilder.DropColumn(name: "ValorCursoCompleto", table: "Personas");

            migrationBuilder.Sql(
                "UPDATE [Roles] SET [Nombre] = 'secretaria' WHERE [Nombre] = 'administrativo'");
        }
    }
}
