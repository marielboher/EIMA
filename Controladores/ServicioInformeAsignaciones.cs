using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

public class ServicioInformeAsignaciones
{
    private readonly EimaDbContext _context;

    public ServicioInformeAsignaciones(EimaDbContext context)
    {
        _context = context;
    }

    public async Task<(InformeAsignacionesAulaDto? Informe, string? Error)> GenerarAsync(
        int anio,
        int mes,
        string periodoTipo,
        int? materiaId,
        bool comparar,
        CancellationToken ct)
    {
        if (mes is < 1 or > 12)
            return (null, "El mes debe estar entre 1 y 12.");

        if (anio is < 2000 or > 2100)
            return (null, "El año debe estar entre 2000 y 2100.");

        if (!PeriodosInformeAsignacion.IntentarNormalizar(periodoTipo, out var periodo))
            return (null, "El periodo debe ser mes o ultimos3meses.");

        string? materiaNombre = null;
        if (materiaId is > 0)
        {
            materiaNombre = await _context.Materias.AsNoTracking()
                .Where(m => m.Id == materiaId.Value)
                .Select(m => m.Nombre)
                .FirstOrDefaultAsync(ct);

            if (materiaNombre is null)
                return (null, "La materia indicada no existe.");
        }
        else
        {
            materiaId = null;
        }

        var actual = CalculadoraInformeAsignaciones.Ventana(periodo, anio, mes);
        var inicioDatos = comparar
            ? CalculadoraInformeAsignaciones.VentanaAnterior(periodo, anio, mes).InicioUtc
            : actual.InicioUtc;
        var fin = actual.FinUtc;

        var asignaciones = await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => materiaId == null || a.MateriaId == materiaId)
            .Where(a =>
                (a.FechaAsignacion >= inicioDatos && a.FechaAsignacion < fin) ||
                (a.FechaCierre != null && a.FechaCierre >= inicioDatos && a.FechaCierre < fin) ||
                (a.FechaAsignacion < fin && (a.FechaCierre == null || a.FechaCierre >= inicioDatos)))
            .Select(a => new AsignacionInformeOrigen
            {
                Id = a.Id,
                MateriaId = a.MateriaId,
                MateriaNombre = a.Materia.Nombre,
                DocenteId = a.DocenteId,
                DocenteNombre = (a.Docente.Apellido + ", " + a.Docente.Nombre).Trim(),
                FechaAsignacion = a.FechaAsignacion,
                FinVigencia = a.FinVigencia,
                FechaCierre = a.FechaCierre,
                CupoHabilitado = a.CupoHabilitado,
                CuposOcupados = a.CuposOcupados
            })
            .ToListAsync(ct);

        var materias = await _context.Materias
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaOpcionDto { Id = m.Id, Nombre = m.Nombre })
            .ToListAsync(ct);

        var informe = CalculadoraInformeAsignaciones.Calcular(
            asignaciones,
            periodo,
            anio,
            mes,
            comparar,
            materiaId,
            materiaNombre,
            materias);

        return (informe, null);
    }

    public async Task<PreferenciaInformeDto> ObtenerPreferenciaAsync(int personaId, CancellationToken ct)
    {
        PreferenciaInformeAsignacion? guardada;
        try
        {
            guardada = await _context.PreferenciasInformeAsignacion
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PersonaId == personaId, ct);
        }
        catch (Exception ex) when (EsTablaInexistente(ex))
        {
            return PreferenciaPorDefecto();
        }

        if (guardada is null)
            return PreferenciaPorDefecto();

        string? materiaNombre = null;
        if (guardada.MateriaId is int materiaId)
        {
            materiaNombre = await _context.Materias.AsNoTracking()
                .Where(m => m.Id == materiaId)
                .Select(m => m.Nombre)
                .FirstOrDefaultAsync(ct);
        }

        return new PreferenciaInformeDto
        {
            PeriodoTipo = guardada.PeriodoTipo,
            Anio = guardada.Anio,
            Mes = guardada.Mes,
            MateriaId = guardada.MateriaId,
            MateriaNombre = materiaNombre,
            Comparar = guardada.Comparar,
            Guardada = true
        };
    }

    public async Task<(PreferenciaInformeDto? Preferencia, string? Error)> GuardarPreferenciaAsync(
        int personaId,
        GuardarPreferenciaInformeDto dto,
        CancellationToken ct)
    {
        if (dto.Mes is < 1 or > 12)
            return (null, "El mes debe estar entre 1 y 12.");

        if (dto.Anio is < 2000 or > 2100)
            return (null, "El año debe estar entre 2000 y 2100.");

        if (!PeriodosInformeAsignacion.IntentarNormalizar(dto.PeriodoTipo, out var periodo))
            return (null, "El periodo debe ser mes o ultimos3meses.");

        int? materiaId = dto.MateriaId is > 0 ? dto.MateriaId : null;
        string? materiaNombre = null;
        if (materiaId is int idMateria)
        {
            materiaNombre = await _context.Materias.AsNoTracking()
                .Where(m => m.Id == idMateria)
                .Select(m => m.Nombre)
                .FirstOrDefaultAsync(ct);

            if (materiaNombre is null)
                return (null, "La materia indicada no existe.");
        }

        try
        {
            var existente = await _context.PreferenciasInformeAsignacion
                .FirstOrDefaultAsync(p => p.PersonaId == personaId, ct);

            if (existente is null)
            {
                existente = new PreferenciaInformeAsignacion { PersonaId = personaId };
                _context.PreferenciasInformeAsignacion.Add(existente);
            }

            existente.PeriodoTipo = periodo;
            existente.Anio = dto.Anio;
            existente.Mes = dto.Mes;
            existente.MateriaId = materiaId;
            existente.Comparar = dto.Comparar;
            existente.FechaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            return (MapearPreferencia(existente, materiaNombre), null);
        }
        catch (DbUpdateException ex) when (EsViolacionUnica(ex))
        {
            _context.ChangeTracker.Clear();
            var existente = await _context.PreferenciasInformeAsignacion
                .FirstOrDefaultAsync(p => p.PersonaId == personaId, ct);

            if (existente is null)
                return (null, "No se pudo guardar la preferencia.");

            existente.PeriodoTipo = periodo;
            existente.Anio = dto.Anio;
            existente.Mes = dto.Mes;
            existente.MateriaId = materiaId;
            existente.Comparar = dto.Comparar;
            existente.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return (MapearPreferencia(existente, materiaNombre), null);
        }
        catch (Exception ex) when (EsTablaInexistente(ex))
        {
            _context.ChangeTracker.Clear();
            return (MapearPreferencia(new PreferenciaInformeAsignacion
            {
                PeriodoTipo = periodo,
                Anio = dto.Anio,
                Mes = dto.Mes,
                MateriaId = materiaId,
                Comparar = dto.Comparar
            }, materiaNombre), null);
        }
    }

    private static PreferenciaInformeDto MapearPreferencia(
        PreferenciaInformeAsignacion existente,
        string? materiaNombre) => new()
    {
        PeriodoTipo = existente.PeriodoTipo,
        Anio = existente.Anio,
        Mes = existente.Mes,
        MateriaId = existente.MateriaId,
        MateriaNombre = materiaNombre,
        Comparar = existente.Comparar,
        Guardada = true
    };

    private static bool EsViolacionUnica(DbUpdateException ex) =>
        RecorreMensaje(ex, texto =>
            texto.Contains("23505", StringComparison.Ordinal) ||
            texto.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase));

    private static bool EsTablaInexistente(Exception ex) =>
        RecorreMensaje(ex, texto =>
            texto.Contains("42P01", StringComparison.Ordinal) ||
            texto.Contains("does not exist", StringComparison.OrdinalIgnoreCase));

    private static bool RecorreMensaje(Exception ex, Func<string, bool> coincide)
    {
        for (Exception? actual = ex; actual is not null; actual = actual.InnerException)
        {
            if (coincide(actual.Message))
                return true;
        }

        return false;
    }

    private static PreferenciaInformeDto PreferenciaPorDefecto()
    {
        var hoy = ZonaHorariaArgentina.ALocal(DateTime.UtcNow);
        return new PreferenciaInformeDto
        {
            PeriodoTipo = PeriodosInformeAsignacion.Mes,
            Anio = hoy.Year,
            Mes = hoy.Month,
            Comparar = false,
            Guardada = false
        };
    }
}
