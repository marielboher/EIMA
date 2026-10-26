using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class InscripcionesGrupoYPreferenciasInforme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InscripcionesGrupo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AsignacionAulaId = table.Column<int>(type: "integer", nullable: false),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscripcionesGrupo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscripcionesGrupo_AsignacionesAula_AsignacionAulaId",
                        column: x => x.AsignacionAulaId,
                        principalTable: "AsignacionesAula",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InscripcionesGrupo_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreferenciasInformeAsignacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoTipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: true),
                    Comparar = table.Column<bool>(type: "boolean", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasInformeAsignacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasInformeAsignacion_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PreferenciasInformeAsignacion_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesGrupo_Grupo_Alumno_Activo",
                table: "InscripcionesGrupo",
                columns: new[] { "AsignacionAulaId", "PersonaId" },
                unique: true,
                filter: "\"FechaBaja\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesGrupo_PersonaId",
                table: "InscripcionesGrupo",
                column: "PersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasInformeAsignacion_MateriaId",
                table: "PreferenciasInformeAsignacion",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasInformeAsignacion_PersonaId",
                table: "PreferenciasInformeAsignacion",
                column: "PersonaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InscripcionesGrupo");

            migrationBuilder.DropTable(
                name: "PreferenciasInformeAsignacion");
        }
    }
}
