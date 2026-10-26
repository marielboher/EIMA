using System.Globalization;
using Entidades;

namespace Controladores;

public readonly record struct VentanaInforme(DateTime InicioUtc, DateTime FinUtc, string Etiqueta);

public sealed class AsignacionInformeOrigen
{
    public int Id { get; init; }
    public int MateriaId { get; init; }
    public string MateriaNombre { get; init; } = string.Empty;
    public int DocenteId { get; init; }
    public string DocenteNombre { get; init; } = string.Empty;
    public DateTime FechaAsignacion { get; init; }
    public DateTime FinVigencia { get; init; }
    public DateTime? FechaCierre { get; init; }
    public int CupoHabilitado { get; init; }
    public int CuposOcupados { get; init; }
}

/// <summary>
/// Indicadores, variación, serie de creadas y desglose por materia y docente (HU28, HU31, HU32, HU33).
/// SLA y TME se calculan sobre el conjunto del periodo, no como promedio de porcentajes mensuales.
/// </summary>
public static class CalculadoraInformeAsignaciones
{
    private static readonly CultureInfo Espanol = CultureInfo.GetCultureInfo("es-AR");

    public static VentanaInforme Ventana(string periodoTipo, int anio, int mes)
    {
        var meses = MesesDelPeriodo(periodoTipo);
        var finLocal = new DateTime(anio, mes, 1).AddMonths(1);
        var inicioLocal = finLocal.AddMonths(-meses);
        return new VentanaInforme(
            ZonaHorariaArgentina.AUtc(inicioLocal),
            ZonaHorariaArgentina.AUtc(finLocal),
            Etiqueta(inicioLocal, finLocal));
    }

    public static VentanaInforme VentanaAnterior(string periodoTipo, int anio, int mes)
    {
        var meses = MesesDelPeriodo(periodoTipo);
        var finActualLocal = new DateTime(anio, mes, 1).AddMonths(1);
        var finAnteriorLocal = finActualLocal.AddMonths(-meses);
        var inicioAnteriorLocal = finAnteriorLocal.AddMonths(-meses);
        return new VentanaInforme(
            ZonaHorariaArgentina.AUtc(inicioAnteriorLocal),
            ZonaHorariaArgentina.AUtc(finAnteriorLocal),
            Etiqueta(inicioAnteriorLocal, finAnteriorLocal));
    }

    public static InformeAsignacionesAulaDto Calcular(
        IReadOnlyList<AsignacionInformeOrigen> asignaciones,
        string periodoTipo,
        int anio,
        int mes,
        bool comparar,
        int? materiaId,
        string? materiaNombre,
        IReadOnlyList<MateriaOpcionDto> materiasDisponibles)
    {
        var actual = Ventana(periodoTipo, anio, mes);
        var anterior = comparar ? VentanaAnterior(periodoTipo, anio, mes) : (VentanaInforme?)null;

        var hechos = Resumir(asignaciones, actual);
        var hechosAnterior = anterior is null
            ? null
            : Resumir(asignaciones, anterior.Value);

        var informe = new InformeAsignacionesAulaDto
        {
            Anio = anio,
            Mes = mes,
            PeriodoTipo = periodoTipo,
            Periodo = actual.Etiqueta,
            EtiquetaPeriodo = actual.Etiqueta,
            EtiquetaPeriodoAnterior = anterior?.Etiqueta,
            Comparar = comparar,
            MateriaId = materiaId,
            MateriaNombre = materiaNombre,
            FechaActualizacion = DateTime.UtcNow,
            Creadas = hechos.Creadas,
            Cerradas = hechos.Cerradas,
            AbiertasAlCierre = hechos.AbiertasAlCierre,
            CerradasEnVigencia = hechos.CerradasEnVigencia,
            SlaPorcentaje = Porcentaje(hechos.CerradasEnVigencia, hechos.Cerradas),
            TmeDias = Promedio(hechos.DiasCierre),
            OcupacionMedia = Promedio(hechos.Ocupaciones),
            OcupacionP50 = Percentil(hechos.Ocupaciones, 0.50),
            OcupacionP90 = Percentil(hechos.Ocupaciones, 0.90),
            SeriesPorMateria = Series(asignaciones, actual, anterior),
            MateriasDisponibles = materiasDisponibles,
            Definiciones = Definiciones(periodoTipo)
        };

        informe.Tabla = Tabla(asignaciones, actual);
        informe.Total = new FilaInformeDto
        {
            Nombre = "Total",
            Creadas = informe.Creadas,
            Cerradas = informe.Cerradas,
            OcupacionMedia = informe.OcupacionMedia,
            SlaPorcentaje = informe.SlaPorcentaje
        };

        if (hechosAnterior is not null)
        {
            informe.Variaciones = new VariacionesInformeDto
            {
                Creadas = Variacion(hechos.Creadas, hechosAnterior.Creadas),
                Cerradas = Variacion(hechos.Cerradas, hechosAnterior.Cerradas),
                AbiertasAlCierre = Variacion(hechos.AbiertasAlCierre, hechosAnterior.AbiertasAlCierre),
                Sla = VariacionNullable(
                    informe.SlaPorcentaje,
                    Porcentaje(hechosAnterior.CerradasEnVigencia, hechosAnterior.Cerradas)),
                Tme = VariacionNullable(informe.TmeDias, Promedio(hechosAnterior.DiasCierre))
            };
        }

        return informe;
    }

    /// <summary>
    /// Percentil inclusivo (el mismo criterio que PERCENTILE.INC): se interpola entre
    /// los valores ordenados. P50 es la mediana; P90 deja por debajo al 90 % de los grupos.
    /// </summary>
    public static double? Percentil(IReadOnlyList<double> valores, double proporcion)
    {
        if (valores.Count == 0)
            return null;

        var ordenados = valores.OrderBy(v => v).ToArray();
        if (ordenados.Length == 1)
            return Redondear(ordenados[0]);

        var rango = (ordenados.Length - 1) * proporcion;
        var inferior = (int)Math.Floor(rango);
        var superior = (int)Math.Ceiling(rango);
        if (inferior == superior)
            return Redondear(ordenados[inferior]);

        var peso = rango - inferior;
        var interpolado = ordenados[inferior] + (ordenados[superior] - ordenados[inferior]) * peso;
        return Redondear(interpolado);
    }

    public static VariacionIndicadorDto Variacion(double actual, double anterior)
    {
        var absoluta = Redondear(actual - anterior);
        double? porcentaje = anterior == 0
            ? null
            : Redondear((actual - anterior) / anterior * 100.0);

        return new VariacionIndicadorDto
        {
            Absoluta = absoluta,
            Porcentaje = porcentaje
        };
    }

    public static double OcupacionGrupo(int cuposOcupados, int cupoHabilitado) =>
        cupoHabilitado <= 0 ? 0 : cuposOcupados * 100.0 / cupoHabilitado;

    private static Resumen Resumir(IReadOnlyList<AsignacionInformeOrigen> asignaciones, VentanaInforme ventana)
    {
        var resumen = new Resumen();

        foreach (var asignacion in asignaciones)
        {
            if (asignacion.FechaAsignacion >= ventana.InicioUtc && asignacion.FechaAsignacion < ventana.FinUtc)
                resumen.Creadas++;

            if (asignacion.FechaCierre is DateTime cierre &&
                cierre >= ventana.InicioUtc &&
                cierre < ventana.FinUtc)
            {
                resumen.Cerradas++;
                if (cierre <= asignacion.FinVigencia)
                    resumen.CerradasEnVigencia++;

                resumen.DiasCierre.Add((cierre - asignacion.FechaAsignacion).TotalDays);
            }

            if (asignacion.FechaAsignacion < ventana.FinUtc &&
                (asignacion.FechaCierre is null || asignacion.FechaCierre >= ventana.FinUtc))
            {
                resumen.AbiertasAlCierre++;
            }

            if (Intersecta(asignacion, ventana) && asignacion.CupoHabilitado > 0)
                resumen.Ocupaciones.Add(OcupacionGrupo(asignacion.CuposOcupados, asignacion.CupoHabilitado));
        }

        return resumen;
    }

    private static bool Intersecta(AsignacionInformeOrigen asignacion, VentanaInforme ventana) =>
        asignacion.FechaAsignacion < ventana.FinUtc &&
        (asignacion.FechaCierre is null || asignacion.FechaCierre >= ventana.InicioUtc);

    private static List<SerieMateriaInformeDto> Series(
        IReadOnlyList<AsignacionInformeOrigen> asignaciones,
        VentanaInforme actual,
        VentanaInforme? anterior)
    {
        var materias = asignaciones
            .GroupBy(a => a.MateriaId)
            .Select(grupo =>
            {
                var creadas = grupo.Count(a => EnVentana(a.FechaAsignacion, actual));
                int? creadasAnterior = anterior is null
                    ? null
                    : grupo.Count(a => EnVentana(a.FechaAsignacion, anterior.Value));

                return new SerieMateriaInformeDto
                {
                    MateriaId = grupo.Key,
                    MateriaNombre = grupo.First().MateriaNombre,
                    Creadas = creadas,
                    CreadasAnterior = creadasAnterior
                };
            })
            .Where(serie => serie.Creadas > 0 || (serie.CreadasAnterior ?? 0) > 0)
            .OrderBy(serie => serie.MateriaNombre)
            .ToList();

        return materias;
    }

    private static List<FilaInformeDto> Tabla(
        IReadOnlyList<AsignacionInformeOrigen> asignaciones,
        VentanaInforme ventana)
    {
        return asignaciones
            .GroupBy(a => new { a.MateriaId, a.MateriaNombre })
            .Select(grupo =>
            {
                var fila = Fila(grupo.Key.MateriaNombre, grupo.Key.MateriaId, grupo, ventana);
                fila.Docentes = grupo
                    .GroupBy(a => new { a.DocenteId, a.DocenteNombre })
                    .Select(docente => Fila(docente.Key.DocenteNombre, docente.Key.DocenteId, docente, ventana))
                    .Where(TieneDatos)
                    .OrderBy(d => d.Nombre)
                    .ToList();
                return fila;
            })
            .Where(TieneDatos)
            .OrderBy(f => f.Nombre)
            .ToList();
    }

    private static FilaInformeDto Fila(
        string nombre,
        int id,
        IEnumerable<AsignacionInformeOrigen> asignaciones,
        VentanaInforme ventana)
    {
        var lista = asignaciones.ToList();
        var cerradas = lista.Where(a => a.FechaCierre is DateTime cierre && EnVentana(cierre, ventana)).ToList();
        var enVigencia = cerradas.Count(a => a.FechaCierre <= a.FinVigencia);
        var ocupaciones = lista
            .Where(a => Intersecta(a, ventana) && a.CupoHabilitado > 0)
            .Select(a => OcupacionGrupo(a.CuposOcupados, a.CupoHabilitado))
            .ToList();

        return new FilaInformeDto
        {
            Id = id,
            Nombre = nombre,
            Creadas = lista.Count(a => EnVentana(a.FechaAsignacion, ventana)),
            Cerradas = cerradas.Count,
            OcupacionMedia = Promedio(ocupaciones),
            SlaPorcentaje = Porcentaje(enVigencia, cerradas.Count)
        };
    }

    private static bool TieneDatos(FilaInformeDto fila) =>
        fila.Creadas > 0 || fila.Cerradas > 0 || fila.OcupacionMedia is not null;

    private static bool EnVentana(DateTime instante, VentanaInforme ventana) =>
        instante >= ventana.InicioUtc && instante < ventana.FinUtc;

    private static double? Porcentaje(int parte, int total) =>
        total > 0 ? Redondear(parte * 100.0 / total) : null;

    private static double? Promedio(IReadOnlyList<double> valores) =>
        valores.Count == 0 ? null : Redondear(valores.Average());

    private static VariacionIndicadorDto VariacionNullable(double? actual, double? anterior)
    {
        if (actual is null || anterior is null)
        {
            return new VariacionIndicadorDto();
        }

        return Variacion(actual.Value, anterior.Value);
    }

    private static double Redondear(double valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static int MesesDelPeriodo(string periodoTipo) =>
        periodoTipo == PeriodosInformeAsignacion.Ultimos3Meses ? 3 : 1;

    private static string Etiqueta(DateTime inicioLocal, DateTime finExclusivoLocal)
    {
        var ultimo = finExclusivoLocal.AddMonths(-1);
        if (inicioLocal.Year == ultimo.Year && inicioLocal.Month == ultimo.Month)
            return Capitalizar(inicioLocal.ToString("MMMM yyyy", Espanol));

        if (inicioLocal.Year == ultimo.Year)
        {
            return $"{Capitalizar(inicioLocal.ToString("MMMM", Espanol))} – {Capitalizar(ultimo.ToString("MMMM yyyy", Espanol))}";
        }

        return $"{Capitalizar(inicioLocal.ToString("MMMM yyyy", Espanol))} – {Capitalizar(ultimo.ToString("MMMM yyyy", Espanol))}";
    }

    private static string Capitalizar(string texto) =>
        string.IsNullOrEmpty(texto) ? texto : char.ToUpper(texto[0], Espanol) + texto[1..];

    private static DefinicionesInformeDto Definiciones(string periodoTipo)
    {
        var alcance = periodoTipo == PeriodosInformeAsignacion.Ultimos3Meses
            ? "el trimestre seleccionado"
            : "el mes seleccionado";

        return new DefinicionesInformeDto
        {
            Creadas = $"Cantidad de asignaciones con fecha de asignación dentro de {alcance} (hora de Argentina). En tres meses se suman; no se promedian.",
            Cerradas = $"Cantidad de asignaciones con fecha de cierre dentro de {alcance} (hora de Argentina).",
            AbiertasAlCierre = "Asignaciones creadas hasta el fin del periodo que seguían abiertas en ese instante (fecha de cierre nula o posterior).",
            Sla = "(Cerradas dentro de vigencia / cerradas del periodo) × 100. Se recalcula sobre el conjunto, no como promedio de los porcentajes mensuales. Dentro de vigencia: fecha de cierre ≤ fin de vigencia.",
            Tme = "Promedio de (fecha de cierre − fecha de asignación) en días, sobre las asignaciones cerradas del periodo.",
            Ocupacion = "Cada grupo aporta (cuposOcupados / cupoHabilitado) × 100. La ocupación media es el promedio de esas ocupaciones. cupoHabilitado es el cupo de la asignación. P50 y P90 usan el percentil inclusivo de esas mismas ocupaciones.",
        };
    }

    private sealed class Resumen
    {
        public int Creadas { get; set; }
        public int Cerradas { get; set; }
        public int CerradasEnVigencia { get; set; }
        public int AbiertasAlCierre { get; set; }
        public List<double> DiasCierre { get; } = new();
        public List<double> Ocupaciones { get; } = new();
    }
}
