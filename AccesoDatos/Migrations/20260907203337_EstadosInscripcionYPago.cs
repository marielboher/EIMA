using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccesoDatos.Migrations
{
    /// <inheritdoc />
    public partial class EstadosInscripcionYPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadosInscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosInscripcion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstadosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosPago", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstadosInscripcion_Nombre",
                table: "EstadosInscripcion",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstadosPago_Nombre",
                table: "EstadosPago",
                column: "Nombre",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "EstadosInscripcion" ("Nombre", "Descripcion") VALUES
                ('Activa', 'Cursada en curso'),
                ('Finalizada', 'Cursada completada'),
                ('Suspendida', 'Cursada pausada temporalmente'),
                ('Cancelada', 'Baja lógica de la inscripción');
                """);

            migrationBuilder.Sql("""
                INSERT INTO "EstadosPago" ("Nombre", "Descripcion") VALUES
                ('Pendiente', 'Pago registrado pendiente de confirmación'),
                ('Confirmado', 'Pago confirmado; suma al monto pagado de la inscripción');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Comprobante",
                table: "Pagos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoId",
                table: "InscripcionesMateria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoId",
                table: "Pagos",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "InscripcionesMateria" i
                SET "EstadoId" = e."Id"
                FROM "EstadosInscripcion" e
                WHERE lower(trim(i."Estado")) = lower(e."Nombre");

                UPDATE "InscripcionesMateria"
                SET "EstadoId" = (SELECT "Id" FROM "EstadosInscripcion" WHERE "Nombre" = 'Activa' LIMIT 1)
                WHERE "EstadoId" IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Pagos" p
                SET "EstadoId" = e."Id"
                FROM "EstadosPago" e
                WHERE lower(trim(p."Estado")) = lower(e."Nombre");

                UPDATE "Pagos"
                SET "EstadoId" = (SELECT "Id" FROM "EstadosPago" WHERE "Nombre" = 'Pendiente' LIMIT 1)
                WHERE "EstadoId" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "InscripcionesMateria");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Pagos");

            migrationBuilder.AlterColumn<int>(
                name: "EstadoId",
                table: "InscripcionesMateria",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EstadoId",
                table: "Pagos",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesMateria_EstadoId",
                table: "InscripcionesMateria",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_EstadoId",
                table: "Pagos",
                column: "EstadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_InscripcionesMateria_EstadosInscripcion_EstadoId",
                table: "InscripcionesMateria",
                column: "EstadoId",
                principalTable: "EstadosInscripcion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_EstadosPago_EstadoId",
                table: "Pagos",
                column: "EstadoId",
                principalTable: "EstadosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InscripcionesMateria_EstadosInscripcion_EstadoId",
                table: "InscripcionesMateria");

            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_EstadosPago_EstadoId",
                table: "Pagos");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "InscripcionesMateria",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Pagos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "InscripcionesMateria" i
                SET "Estado" = e."Nombre"
                FROM "EstadosInscripcion" e
                WHERE i."EstadoId" = e."Id";

                UPDATE "Pagos" p
                SET "Estado" = e."Nombre"
                FROM "EstadosPago" e
                WHERE p."EstadoId" = e."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "InscripcionesMateria",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Pagos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Pagos_EstadoId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_InscripcionesMateria_EstadoId",
                table: "InscripcionesMateria");

            migrationBuilder.DropColumn(
                name: "EstadoId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "EstadoId",
                table: "InscripcionesMateria");

            migrationBuilder.DropTable(
                name: "EstadosInscripcion");

            migrationBuilder.DropTable(
                name: "EstadosPago");

            migrationBuilder.AlterColumn<string>(
                name: "Comprobante",
                table: "Pagos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
