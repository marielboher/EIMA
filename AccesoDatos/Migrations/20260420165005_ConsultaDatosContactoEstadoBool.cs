using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class ConsultaDatosContactoEstadoBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaRespuesta",
                table: "Consultas");

            migrationBuilder.DropColumn(
                name: "Respuesta",
                table: "Consultas");

            migrationBuilder.AddColumn<bool>(
                name: "EstadoBool",
                table: "Consultas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE Consultas
                SET EstadoBool = CASE
                    WHEN LTRIM(RTRIM(Estado)) IN (N'respondida', N'resuelta', N'cerrada') THEN 1
                    ELSE 0
                END
                """);

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Consultas");

            migrationBuilder.RenameColumn(
                name: "EstadoBool",
                table: "Consultas",
                newName: "Estado");

            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Consultas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Consultas",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Consultas",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Consultas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Consultas");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Consultas");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Consultas");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Consultas");

            migrationBuilder.AddColumn<string>(
                name: "EstadoStr",
                table: "Consultas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE Consultas
                SET EstadoStr = CASE WHEN Estado = 1 THEN N'respondida' ELSE N'pendiente' END
                """);

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Consultas");

            migrationBuilder.RenameColumn(
                name: "EstadoStr",
                table: "Consultas",
                newName: "Estado");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRespuesta",
                table: "Consultas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Respuesta",
                table: "Consultas",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }
    }
}
