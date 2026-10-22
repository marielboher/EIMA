using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class AsignacionesAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsignacionesAula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AulaId = table.Column<int>(type: "integer", nullable: false),
                    MateriaId = table.Column<int>(type: "integer", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CupoHabilitado = table.Column<int>(type: "integer", nullable: false),
                    CuposOcupados = table.Column<int>(type: "integer", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesAula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesAula_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesAula_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesAula_Personas_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAula_AulaId",
                table: "AsignacionesAula",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAula_DocenteId",
                table: "AsignacionesAula",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAula_Estado",
                table: "AsignacionesAula",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAula_FechaAsignacion",
                table: "AsignacionesAula",
                column: "FechaAsignacion");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAula_MateriaId",
                table: "AsignacionesAula",
                column: "MateriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsignacionesAula");
        }
    }
}
