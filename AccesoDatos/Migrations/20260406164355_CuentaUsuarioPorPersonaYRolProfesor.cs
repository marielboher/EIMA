using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class CuentaUsuarioPorPersonaYRolProfesor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuentasUsuario_Alumnos_AlumnoId",
                table: "CuentasUsuario");

            migrationBuilder.RenameColumn(
                name: "AlumnoId",
                table: "CuentasUsuario",
                newName: "PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_CuentasUsuario_AlumnoId",
                table: "CuentasUsuario",
                newName: "IX_CuentasUsuario_PersonaId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasUsuario_Personas_PersonaId",
                table: "CuentasUsuario",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = N'super_admin')
                    INSERT INTO Roles (Nombre, Descripcion) VALUES (N'super_admin', N'Super administrador del sistema');
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = N'secretaria')
                    INSERT INTO Roles (Nombre, Descripcion) VALUES (N'secretaria', N'Secretaría / administración');
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Nombre = N'profesor')
                    INSERT INTO Roles (Nombre, Descripcion) VALUES (N'profesor', N'Docente');
                """);

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Profesores",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                DECLARE @RidProf INT = (SELECT TOP 1 Id FROM Roles WHERE Nombre = N'profesor');
                UPDATE Profesores SET RolId = @RidProf WHERE RolId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Profesores",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profesores_RolId",
                table: "Profesores",
                column: "RolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Profesores_Roles_RolId",
                table: "Profesores",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profesores_Roles_RolId",
                table: "Profesores");

            migrationBuilder.DropForeignKey(
                name: "FK_CuentasUsuario_Personas_PersonaId",
                table: "CuentasUsuario");

            migrationBuilder.DropIndex(
                name: "IX_Profesores_RolId",
                table: "Profesores");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Profesores");

            migrationBuilder.RenameColumn(
                name: "PersonaId",
                table: "CuentasUsuario",
                newName: "AlumnoId");

            migrationBuilder.RenameIndex(
                name: "IX_CuentasUsuario_PersonaId",
                table: "CuentasUsuario",
                newName: "IX_CuentasUsuario_AlumnoId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuentasUsuario_Alumnos_AlumnoId",
                table: "CuentasUsuario",
                column: "AlumnoId",
                principalTable: "Alumnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
