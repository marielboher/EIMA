using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class AutenticacionCuentasAlumno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Personas SET Telefono = N'' WHERE Telefono IS NULL;
                UPDATE Personas SET Direccion = N'' WHERE Direccion IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Personas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Personas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Alumnos",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = N'alumno')
                    INSERT INTO Roles (Nombre, Descripcion) VALUES (N'alumno', N'Usuario alumno del sistema');
                DECLARE @RolAlumnoId INT = (SELECT TOP 1 Id FROM Roles WHERE Nombre = N'alumno');
                UPDATE Alumnos SET RolId = @RolAlumnoId WHERE RolId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Alumnos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CuentasUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HashContrasena = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AlumnoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasUsuario_Alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "Alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alumnos_RolId",
                table: "Alumnos",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasUsuario_AlumnoId",
                table: "CuentasUsuario",
                column: "AlumnoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasUsuario_CorreoElectronico",
                table: "CuentasUsuario",
                column: "CorreoElectronico",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Alumnos_Roles_RolId",
                table: "Alumnos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alumnos_Roles_RolId",
                table: "Alumnos");

            migrationBuilder.DropTable(
                name: "CuentasUsuario");

            migrationBuilder.DropIndex(
                name: "IX_Alumnos_RolId",
                table: "Alumnos");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Alumnos");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Personas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "Personas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);
        }
    }
}
