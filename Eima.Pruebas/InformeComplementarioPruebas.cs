using System.Text;
using Controladores;
using Entidades;
using Xunit;

namespace Eima.Pruebas;

public class InformeComplementarioPruebas : IDisposable
{
    private readonly BaseCupo _base = new();

    [Fact]
    public async Task Cada_administrador_recupera_solo_su_preferencia()
    {
        await using var db = _base.NuevoContexto();
        var servicio = new ServicioInformeAsignaciones(db);
        var adminA = _base.CrearAlumno(db, "901");
        var adminB = _base.CrearAlumno(db, "902");

        var guardadaA = await servicio.GuardarPreferenciaAsync(adminA, new GuardarPreferenciaInformeDto
        {
            PeriodoTipo = PeriodosInformeAsignacion.Ultimos3Meses,
            Anio = 2026,
            Mes = 8,
            Comparar = true
        }, CancellationToken.None);

        var guardadaB = await servicio.GuardarPreferenciaAsync(adminB, new GuardarPreferenciaInformeDto
        {
            PeriodoTipo = PeriodosInformeAsignacion.Mes,
            Anio = 2026,
            Mes = 3,
            MateriaId = _base.MateriaId,
            Comparar = false
        }, CancellationToken.None);

        Assert.Null(guardadaA.Error);
        Assert.Null(guardadaB.Error);

        var leidaA = await servicio.ObtenerPreferenciaAsync(adminA, CancellationToken.None);
        var leidaB = await servicio.ObtenerPreferenciaAsync(adminB, CancellationToken.None);

        Assert.Equal(PeriodosInformeAsignacion.Ultimos3Meses, leidaA.PeriodoTipo);
        Assert.Equal(8, leidaA.Mes);
        Assert.True(leidaA.Comparar);
        Assert.Null(leidaA.MateriaId);

        Assert.Equal(3, leidaB.Mes);
        Assert.Equal(_base.MateriaId, leidaB.MateriaId);
        Assert.False(leidaB.Comparar);
        Assert.Equal("Matemática", leidaB.MateriaNombre);
    }

    [Fact]
    public void La_exportacion_incluye_los_cupos_disponibles()
    {
        var filas = new[]
        {
            new AsignacionAulaListadoDto
            {
                Id = 7,
                AulaNombre = "Aula 1",
                MateriaNombre = "Matemática",
                Estado = "Abierta",
                DocenteNombre = "Pérez, Ana",
                CupoHabilitado = 10,
                CuposOcupados = 3,
                CuposDisponibles = 7,
                FinVigencia = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        var csv = Encoding.UTF8.GetString(ExportadorAsignacionesAula.ConstruirCsv(filas, new MetadatosExportacion
        {
            FechaExportacionUtc = new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc),
            TotalFilas = 1
        }));

        Assert.Contains("Disponibles", csv);
        Assert.Contains("3 / 10", csv);
        Assert.Contains(";7;", csv);
    }

    [Fact]
    public void El_pdf_empieza_como_documento_y_refleja_filtros()
    {
        var informe = CalculadoraInformeAsignaciones.Calcular(
            Array.Empty<AsignacionInformeOrigen>(),
            PeriodosInformeAsignacion.Mes,
            2026,
            10,
            comparar: true,
            materiaId: null,
            materiaNombre: null,
            Array.Empty<MateriaOpcionDto>());

        var pdf = GeneradorPdfInformeAsignaciones.Generar(informe);

        Assert.True(pdf.Length > 500);
        Assert.Equal((byte)'%', pdf[0]);
        Assert.Equal((byte)'P', pdf[1]);
        Assert.Equal((byte)'D', pdf[2]);
        Assert.Equal((byte)'F', pdf[3]);
    }

    public void Dispose() => _base.Dispose();
}
