using Controladores;
using Entidades;
using Xunit;

namespace Eima.Pruebas;

public class CalculadoraInformeAsignacionesPruebas
{
    [Fact]
    public void Variacion_absoluta_y_porcentual()
    {
        var variacion = CalculadoraInformeAsignaciones.Variacion(10, 8);

        Assert.Equal(2, variacion.Absoluta);
        Assert.Equal(25, variacion.Porcentaje);
    }

    [Fact]
    public void Porcentaje_queda_vacio_cuando_el_periodo_anterior_es_cero()
    {
        var variacion = CalculadoraInformeAsignaciones.Variacion(4, 0);

        Assert.Equal(4, variacion.Absoluta);
        Assert.Null(variacion.Porcentaje);
    }

    [Fact]
    public void Ultimos_tres_meses_compara_contra_el_bloque_anterior()
    {
        var actual = CalculadoraInformeAsignaciones.Ventana(PeriodosInformeAsignacion.Ultimos3Meses, 2026, 10);
        var anterior = CalculadoraInformeAsignaciones.VentanaAnterior(PeriodosInformeAsignacion.Ultimos3Meses, 2026, 10);

        Assert.Equal(actual.InicioUtc, anterior.FinUtc);
        Assert.Contains("agosto", actual.Etiqueta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("octubre", actual.Etiqueta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mayo", anterior.Etiqueta, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("julio", anterior.Etiqueta, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Comparativo_arma_dos_series_y_sin_comparar_queda_una()
    {
        var actual = CalculadoraInformeAsignaciones.Ventana(PeriodosInformeAsignacion.Mes, 2026, 10);
        var matematica = Grupo(actual.InicioUtc.AddDays(1), materiaId: 1, nombre: "Matemática", creadas: true);
        var fisica = Grupo(actual.InicioUtc.AddDays(2), materiaId: 2, nombre: "Física", creadas: true);
        var anterior = CalculadoraInformeAsignaciones.VentanaAnterior(PeriodosInformeAsignacion.Mes, 2026, 10);
        var matematicaAnterior = Grupo(anterior.InicioUtc.AddDays(1), materiaId: 1, nombre: "Matemática", creadas: true);

        var comparado = CalculadoraInformeAsignaciones.Calcular(
            new[] { matematica, fisica, matematicaAnterior },
            PeriodosInformeAsignacion.Mes,
            2026,
            10,
            comparar: true,
            materiaId: null,
            materiaNombre: null,
            Array.Empty<MateriaOpcionDto>());

        Assert.Equal(2, comparado.SeriesPorMateria.Count);
        Assert.All(comparado.SeriesPorMateria, serie => Assert.NotNull(serie.CreadasAnterior));
        Assert.Equal("Octubre 2026", comparado.EtiquetaPeriodo);
        Assert.Contains("septiembre", comparado.EtiquetaPeriodoAnterior, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(comparado.Variaciones);
        Assert.Equal(2, comparado.Creadas);
        Assert.Equal(1, comparado.Variaciones!.Creadas.Absoluta);

        var simple = CalculadoraInformeAsignaciones.Calcular(
            new[] { matematica, fisica, matematicaAnterior },
            PeriodosInformeAsignacion.Mes,
            2026,
            10,
            comparar: false,
            materiaId: null,
            materiaNombre: null,
            Array.Empty<MateriaOpcionDto>());

        Assert.All(simple.SeriesPorMateria, serie => Assert.Null(serie.CreadasAnterior));
        Assert.Null(simple.Variaciones);
        Assert.Equal(2, simple.SeriesPorMateria.Count);
    }

    [Fact]
    public void Sla_del_trimestre_se_calcula_sobre_el_conjunto()
    {
        var ventana = CalculadoraInformeAsignaciones.Ventana(PeriodosInformeAsignacion.Ultimos3Meses, 2026, 10);
        var enAgosto = Cerrada(ventana.InicioUtc.AddDays(2), dentroDeVigencia: true);
        var enOctubre = Enumerable.Range(0, 3)
            .Select(i => Cerrada(ventana.InicioUtc.AddDays(70 + i), dentroDeVigencia: false))
            .ToArray();

        var informe = CalculadoraInformeAsignaciones.Calcular(
            new[] { enAgosto }.Concat(enOctubre).ToArray(),
            PeriodosInformeAsignacion.Ultimos3Meses,
            2026,
            10,
            comparar: false,
            materiaId: null,
            materiaNombre: null,
            Array.Empty<MateriaOpcionDto>());

        Assert.Equal(4, informe.Cerradas);
        Assert.Equal(25, informe.SlaPorcentaje);
        Assert.Equal(informe.Creadas, informe.Total.Creadas);
        Assert.Equal(informe.Cerradas, informe.Total.Cerradas);
    }

    [Fact]
    public void Ocupacion_media_es_el_promedio_de_los_porcentajes_de_cada_grupo()
    {
        var ventana = CalculadoraInformeAsignaciones.Ventana(PeriodosInformeAsignacion.Mes, 2026, 10);
        var grupos = new[]
        {
            Ocupado(ventana.InicioUtc.AddHours(1), ocupados: 5, habilitado: 10, docente: "Pérez, Ana"),
            Ocupado(ventana.InicioUtc.AddHours(2), ocupados: 4, habilitado: 4, docente: "Pérez, Ana"),
            Ocupado(ventana.InicioUtc.AddHours(3), ocupados: 0, habilitado: 2, docente: "Gómez, Luis")
        };

        var informe = CalculadoraInformeAsignaciones.Calcular(
            grupos,
            PeriodosInformeAsignacion.Mes,
            2026,
            10,
            comparar: false,
            materiaId: null,
            materiaNombre: null,
            Array.Empty<MateriaOpcionDto>());

        Assert.Equal(50, informe.OcupacionMedia);
        Assert.Equal(50, informe.OcupacionP50);
        Assert.Equal(90, informe.OcupacionP90);
        Assert.Equal(50, informe.Total.OcupacionMedia);

        var materia = Assert.Single(informe.Tabla);
        Assert.Equal(2, materia.Docentes.Count);
        Assert.Equal(3, materia.Creadas);
        Assert.Equal(informe.Creadas, informe.Tabla.Sum(f => f.Creadas));
    }

    [Fact]
    public void Filtrar_por_materia_restringe_tarjetas_grafico_y_tabla()
    {
        var ventana = CalculadoraInformeAsignaciones.Ventana(PeriodosInformeAsignacion.Mes, 2026, 10);
        var matematica = Grupo(ventana.InicioUtc.AddDays(1), 1, "Matemática", creadas: true);
        var fisica = Grupo(ventana.InicioUtc.AddDays(1), 2, "Física", creadas: true);

        var informe = CalculadoraInformeAsignaciones.Calcular(
            new[] { matematica },
            PeriodosInformeAsignacion.Mes,
            2026,
            10,
            comparar: false,
            materiaId: 1,
            materiaNombre: "Matemática",
            Array.Empty<MateriaOpcionDto>());

        Assert.Equal(1, informe.Creadas);
        Assert.Equal("Matemática", Assert.Single(informe.SeriesPorMateria).MateriaNombre);
        Assert.Equal("Matemática", Assert.Single(informe.Tabla).Nombre);
        Assert.DoesNotContain(informe.Tabla, fila => fila.Nombre == "Física");
        _ = fisica;
    }

    [Fact]
    public void Percentil_de_un_solo_valor_y_de_lista_vacia()
    {
        Assert.Null(CalculadoraInformeAsignaciones.Percentil(Array.Empty<double>(), 0.9));
        Assert.Equal(40, CalculadoraInformeAsignaciones.Percentil(new[] { 40d }, 0.9));
    }

    private static int _secuencia = 1;

    private static AsignacionInformeOrigen Grupo(DateTime fecha, int materiaId, string nombre, bool creadas) =>
        new()
        {
            Id = _secuencia++,
            MateriaId = materiaId,
            MateriaNombre = nombre,
            DocenteId = materiaId,
            DocenteNombre = "Pérez, Ana",
            FechaAsignacion = fecha,
            FinVigencia = fecha.AddMonths(2),
            CupoHabilitado = 10,
            CuposOcupados = 0
        };

    private static AsignacionInformeOrigen Cerrada(DateTime cierre, bool dentroDeVigencia) =>
        new()
        {
            Id = _secuencia++,
            MateriaId = 1,
            MateriaNombre = "Matemática",
            DocenteId = 1,
            DocenteNombre = "Pérez, Ana",
            FechaAsignacion = cierre.AddDays(-2),
            FinVigencia = dentroDeVigencia ? cierre.AddDays(1) : cierre.AddDays(-1),
            FechaCierre = cierre,
            CupoHabilitado = 10,
            CuposOcupados = 1
        };

    private static AsignacionInformeOrigen Ocupado(DateTime alta, int ocupados, int habilitado, string docente) =>
        new()
        {
            Id = _secuencia++,
            MateriaId = 1,
            MateriaNombre = "Matemática",
            DocenteId = docente.Contains("Gómez", StringComparison.Ordinal) ? 2 : 1,
            DocenteNombre = docente,
            FechaAsignacion = alta,
            FinVigencia = alta.AddMonths(1),
            CupoHabilitado = habilitado,
            CuposOcupados = ocupados
        };
}
