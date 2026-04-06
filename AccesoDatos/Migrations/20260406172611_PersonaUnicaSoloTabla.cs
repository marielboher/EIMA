using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class PersonaUnicaSoloTabla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Alumnos_AlumnoId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Clases_Profesores_ProfesorId",
                table: "Clases");

            migrationBuilder.DropForeignKey(
                name: "FK_Consultas_Alumnos_AlumnoId",
                table: "Consultas");

            migrationBuilder.DropForeignKey(
                name: "FK_HorariosDisponibles_Profesores_ProfesorId",
                table: "HorariosDisponibles");

            migrationBuilder.DropForeignKey(
                name: "FK_InscripcionesMateria_Alumnos_AlumnoId",
                table: "InscripcionesMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Alumnos_AlumnoId",
                table: "Pagos");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfesoresMaterias_Profesores_ProfesorId",
                table: "ProfesoresMaterias");

            // Ampliar Personas (todo nullable hasta fusionar roles)
            migrationBuilder.AddColumn<bool>(
                name: "ActivoComoColaborador",
                table: "Personas",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Colegio",
                table: "Personas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Especialidades",
                table: "Personas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaContratacion",
                table: "Personas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinContratacion",
                table: "Personas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIngresoDocente",
                table: "Personas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradoCurso",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NivelEducativo",
                table: "Personas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Personas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Salario",
                table: "Personas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoColaboradorId",
                table: "Personas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Personas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE p SET
                    p.RolId = a.RolId,
                    p.Colegio = a.Colegio,
                    p.GradoCurso = a.GradoCurso,
                    p.NivelEducativo = a.NivelEducativo
                FROM Personas p
                INNER JOIN Alumnos a ON a.Id = p.Id;

                UPDATE p SET
                    p.RolId = pr.RolId,
                    p.Especialidades = pr.Especialidades,
                    p.Titulo = pr.Titulo,
                    p.FechaIngresoDocente = pr.FechaIngreso
                FROM Personas p
                INNER JOIN Profesores pr ON pr.Id = p.Id;

                UPDATE p SET
                    p.RolId = e.RolId,
                    p.TipoColaboradorId = e.TipoColaboradorId,
                    p.FechaContratacion = e.FechaContratacion,
                    p.FechaFinContratacion = e.FechaFinContratacion,
                    p.Salario = e.Salario,
                    p.ActivoComoColaborador = e.Activo
                FROM Personas p
                INNER JOIN Empleados e ON e.Id = p.Id;

                UPDATE Personas
                SET RolId = (SELECT TOP 1 Id FROM Roles WHERE Nombre = N'alumno')
                WHERE RolId IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Personas",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "ProfesorId",
                table: "ProfesoresMaterias",
                newName: "DocenteId");

            migrationBuilder.RenameIndex(
                name: "IX_ProfesoresMaterias_ProfesorId",
                table: "ProfesoresMaterias",
                newName: "IX_ProfesoresMaterias_DocenteId");

            migrationBuilder.RenameColumn(
                name: "AlumnoId",
                table: "Pagos",
                newName: "PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_Pagos_AlumnoId",
                table: "Pagos",
                newName: "IX_Pagos_PersonaId");

            migrationBuilder.RenameColumn(
                name: "AlumnoId",
                table: "InscripcionesMateria",
                newName: "PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_InscripcionesMateria_AlumnoId",
                table: "InscripcionesMateria",
                newName: "IX_InscripcionesMateria_PersonaId");

            migrationBuilder.RenameColumn(
                name: "ProfesorId",
                table: "HorariosDisponibles",
                newName: "DocenteId");

            migrationBuilder.RenameIndex(
                name: "IX_HorariosDisponibles_ProfesorId",
                table: "HorariosDisponibles",
                newName: "IX_HorariosDisponibles_DocenteId");

            migrationBuilder.RenameColumn(
                name: "AlumnoId",
                table: "Consultas",
                newName: "PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_Consultas_AlumnoId",
                table: "Consultas",
                newName: "IX_Consultas_PersonaId");

            migrationBuilder.RenameColumn(
                name: "ProfesorId",
                table: "Clases",
                newName: "DocenteId");

            migrationBuilder.RenameIndex(
                name: "IX_Clases_ProfesorId",
                table: "Clases",
                newName: "IX_Clases_DocenteId");

            migrationBuilder.RenameColumn(
                name: "AlumnoId",
                table: "Asistencias",
                newName: "PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_Asistencias_ClaseId_AlumnoId",
                table: "Asistencias",
                newName: "IX_Asistencias_ClaseId_PersonaId");

            migrationBuilder.RenameIndex(
                name: "IX_Asistencias_AlumnoId",
                table: "Asistencias",
                newName: "IX_Asistencias_PersonaId");

            migrationBuilder.DropForeignKey(
                name: "FK_Alumnos_Personas_Id",
                table: "Alumnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Alumnos_Roles_RolId",
                table: "Alumnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Profesores_Personas_Id",
                table: "Profesores");

            migrationBuilder.DropForeignKey(
                name: "FK_Profesores_Roles_RolId",
                table: "Profesores");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Personas_Id",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_Roles_RolId",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_TiposColaborador_TipoColaboradorId",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Alumnos_RolId",
                table: "Alumnos");

            migrationBuilder.DropIndex(
                name: "IX_Profesores_RolId",
                table: "Profesores");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_RolId",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_TipoColaboradorId",
                table: "Empleados");

            migrationBuilder.DropTable(
                name: "Alumnos");

            migrationBuilder.DropTable(
                name: "Profesores");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_RolId",
                table: "Personas",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_TipoColaboradorId",
                table: "Personas",
                column: "TipoColaboradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_Roles_RolId",
                table: "Personas",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personas_TiposColaborador_TipoColaboradorId",
                table: "Personas",
                column: "TipoColaboradorId",
                principalTable: "TiposColaborador",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Personas_PersonaId",
                table: "Asistencias",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clases_Personas_DocenteId",
                table: "Clases",
                column: "DocenteId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Consultas_Personas_PersonaId",
                table: "Consultas",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosDisponibles_Personas_DocenteId",
                table: "HorariosDisponibles",
                column: "DocenteId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InscripcionesMateria_Personas_PersonaId",
                table: "InscripcionesMateria",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Personas_PersonaId",
                table: "Pagos",
                column: "PersonaId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfesoresMaterias_Personas_DocenteId",
                table: "ProfesoresMaterias",
                column: "DocenteId",
                principalTable: "Personas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Revertir esta migración puede provocar pérdida de datos. Restaure un respaldo anterior si lo necesita.");
        }
    }
}
