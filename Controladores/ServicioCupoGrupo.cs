using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Controladores;

/// <summary>
/// Reserva y libera cupos de un grupo (HU29).
/// Ocupa cupo un vínculo activo de una persona activa con inscripción Activa a la materia del grupo.
/// El contador se recalcula en la misma transacción del alta o la baja.
/// </summary>
public class ServicioCupoGrupo
{
    private readonly EimaDbContext _context;
    private readonly IBloqueoFilaGrupo _bloqueo;

    public ServicioCupoGrupo(EimaDbContext context, IBloqueoFilaGrupo bloqueo)
    {
        _context = context;
        _bloqueo = bloqueo;
    }

    public async Task<ListadoAlumnosGrupoDto?> ListarInscriptosAsync(int asignacionAulaId, CancellationToken ct)
    {
        var grupo = await _context.AsignacionesAula
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == asignacionAulaId, ct);

        if (grupo is null)
            return null;

        var alumnos = await _context.InscripcionesGrupo
            .AsNoTracking()
            .Where(v => v.AsignacionAulaId == asignacionAulaId && v.FechaBaja == null)
            .OrderBy(v => v.Persona.Apellido)
            .ThenBy(v => v.Persona.Nombre)
            .Select(v => new AlumnoGrupoDto
            {
                VinculoId = v.Id,
                PersonaId = v.PersonaId,
                NombreCompleto = (v.Persona.Apellido + ", " + v.Persona.Nombre).Trim(),
                Dni = v.Persona.Dni,
                FechaAsignacion = v.FechaAsignacion
            })
            .ToListAsync(ct);

        return ArmarListado(grupo, alumnos);
    }

    public async Task<IReadOnlyList<CandidatoGrupoDto>?> ListarCandidatosAsync(
        int asignacionAulaId,
        string? buscar,
        CancellationToken ct)
    {
        var grupo = await _context.AsignacionesAula
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == asignacionAulaId, ct);

        if (grupo is null)
            return null;

        var vinculados = _context.InscripcionesGrupo
            .Where(v => v.AsignacionAulaId == asignacionAulaId && v.FechaBaja == null)
            .Select(v => v.PersonaId);

        var query = _context.Personas.AsNoTracking()
            .Where(p =>
                p.Activo &&
                p.Rol.Nombre == RolesSistema.Alumno &&
                !vinculados.Contains(p.Id) &&
                _context.Inscripciones.Any(i =>
                    i.PersonaId == p.Id &&
                    i.MateriaId == grupo.MateriaId &&
                    i.Estado.Nombre == EstadosInscripcionSistema.Activa));

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Dni.Contains(termino) ||
                p.Nombre.ToLower().Contains(termino) ||
                p.Apellido.ToLower().Contains(termino));
        }

        return await query
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .Take(20)
            .Select(p => new CandidatoGrupoDto
            {
                PersonaId = p.Id,
                NombreCompleto = (p.Apellido + ", " + p.Nombre).Trim(),
                Dni = p.Dni
            })
            .ToListAsync(ct);
    }

    public async Task<ResultadoOperacionCupo> VincularAsync(int asignacionAulaId, int personaId, CancellationToken ct)
    {
        IDbContextTransaction? transaccion = null;
        IAsyncDisposable? bloqueo = null;

        try
        {
            transaccion = await IniciarTransaccionAsync(ct);
            bloqueo = await _bloqueo.BloquearAsync(_context, asignacionAulaId, ct);

            var resultado = await VincularDentroDelBloqueoAsync(asignacionAulaId, personaId, ct);
            if (resultado.Exito && transaccion is not null)
                await transaccion.CommitAsync(ct);

            return resultado;
        }
        catch (DbUpdateException ex) when (EsViolacionUnica(ex))
        {
            _context.ChangeTracker.Clear();
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.Duplicado,
                "El alumno ya está vinculado de forma activa a este grupo.");
        }
        finally
        {
            if (transaccion is not null)
                await transaccion.DisposeAsync();

            if (bloqueo is not null)
                await bloqueo.DisposeAsync();
        }
    }

    public async Task<ResultadoOperacionCupo> DarBajaVinculoAsync(
        int asignacionAulaId,
        int personaId,
        CancellationToken ct)
    {
        IDbContextTransaction? transaccion = null;
        IAsyncDisposable? bloqueo = null;

        try
        {
            transaccion = await IniciarTransaccionAsync(ct);
            bloqueo = await _bloqueo.BloquearAsync(_context, asignacionAulaId, ct);

            var grupo = await _context.AsignacionesAula
                .FirstOrDefaultAsync(a => a.Id == asignacionAulaId, ct);

            if (grupo is null)
            {
                return ResultadoOperacionCupo.Fallo(
                    CodigosCupo.NoEncontrado,
                    "No se encontró el grupo solicitado.");
            }

            if (grupo.Estado != EstadosAsignacionAula.Abierta)
            {
                return ResultadoOperacionCupo.Fallo(
                    CodigosCupo.GrupoCerrado,
                    "El grupo está cerrado y no admite altas ni bajas de alumnos.");
            }

            var vinculo = await _context.InscripcionesGrupo
                .Include(v => v.Persona)
                .FirstOrDefaultAsync(v =>
                    v.AsignacionAulaId == asignacionAulaId &&
                    v.PersonaId == personaId &&
                    v.FechaBaja == null, ct);

            if (vinculo is null)
            {
                return ResultadoOperacionCupo.Fallo(
                    CodigosCupo.SinVinculo,
                    "El alumno no tiene un vínculo activo en este grupo.");
            }

            vinculo.FechaBaja = DateTime.UtcNow;
            await RecalcularGrupoAsync(grupo, ct);

            if (transaccion is not null)
                await transaccion.CommitAsync(ct);

            return ResultadoOperacionCupo.Ok(MapearAlumno(vinculo));
        }
        finally
        {
            if (transaccion is not null)
                await transaccion.DisposeAsync();

            if (bloqueo is not null)
                await bloqueo.DisposeAsync();
        }
    }

    /// <summary>
    /// Baja lógica de los vínculos activos del alumno en los grupos de la materia (HU21)
    /// y recálculo de cada cupo. Participa de la transacción ambiente si ya hay una.
    /// </summary>
    public async Task DarBajaVinculosDeInscripcionAsync(int personaId, int materiaId, CancellationToken ct)
    {
        await EjecutarEnTransaccionAsync(async () =>
        {
            var vinculos = await _context.InscripcionesGrupo
                .Include(v => v.AsignacionAula)
                .Where(v =>
                    v.PersonaId == personaId &&
                    v.FechaBaja == null &&
                    v.AsignacionAula.MateriaId == materiaId)
                .ToListAsync(ct);

            var ahora = DateTime.UtcNow;
            foreach (var vinculo in vinculos)
                vinculo.FechaBaja = ahora;

            var grupos = vinculos
                .Select(v => v.AsignacionAula)
                .DistinctBy(a => a.Id);

            foreach (var grupo in grupos)
                await RecalcularGrupoAsync(grupo, ct);

            if (vinculos.Count == 0)
                await _context.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>
    /// Recalcula los grupos donde el alumno tiene un vínculo activo.
    /// Se usa al crear o editar la inscripción y al activar o inactivar a la persona.
    /// </summary>
    public async Task RecalcularPorPersonaYMateriaAsync(int personaId, int materiaId, CancellationToken ct)
    {
        await EjecutarEnTransaccionAsync(async () =>
        {
            var grupos = await _context.AsignacionesAula
                .Where(a =>
                    a.MateriaId == materiaId &&
                    a.InscripcionesGrupo.Any(v => v.PersonaId == personaId && v.FechaBaja == null))
                .ToListAsync(ct);

            foreach (var grupo in grupos)
                await RecalcularGrupoAsync(grupo, ct);

            if (grupos.Count == 0)
                await _context.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task RecalcularPorPersonaAsync(int personaId, CancellationToken ct)
    {
        await EjecutarEnTransaccionAsync(async () =>
        {
            var grupos = await _context.AsignacionesAula
                .Where(a => a.InscripcionesGrupo.Any(v => v.PersonaId == personaId && v.FechaBaja == null))
                .ToListAsync(ct);

            foreach (var grupo in grupos)
                await RecalcularGrupoAsync(grupo, ct);

            if (grupos.Count == 0)
                await _context.SaveChangesAsync(ct);
        }, ct);
    }

    /// <summary>Deja <c>cuposOcupados</c> igual a la cantidad de vínculos que ocupan cupo.</summary>
    public async Task RecalcularTodosAsync(CancellationToken ct)
    {
        await EjecutarEnTransaccionAsync(async () =>
        {
            var grupos = await _context.AsignacionesAula.ToListAsync(ct);
            foreach (var grupo in grupos)
                await RecalcularGrupoAsync(grupo, ct);
        }, ct);
    }

    public static int CuposDisponibles(int cupoHabilitado, int cuposOcupados) =>
        Math.Max(0, cupoHabilitado - cuposOcupados);

    public static ListadoAlumnosGrupoDto ArmarListado(AsignacionAula grupo, IReadOnlyList<AlumnoGrupoDto> alumnos)
    {
        var disponibles = CuposDisponibles(grupo.CupoHabilitado, grupo.CuposOcupados);
        var completo = grupo.CuposOcupados >= grupo.CupoHabilitado;
        var cerrado = grupo.Estado != EstadosAsignacionAula.Abierta;
        var vencido = grupo.FinVigencia <= DateTime.UtcNow;

        string? motivo = null;
        if (cerrado)
            motivo = "El grupo está cerrado: no se pueden agregar ni dar de baja alumnos.";
        else if (vencido)
            motivo = "El grupo no admite altas porque su vigencia venció.";
        else if (completo)
            motivo = "Cupo completo.";

        return new ListadoAlumnosGrupoDto
        {
            AsignacionAulaId = grupo.Id,
            CupoHabilitado = grupo.CupoHabilitado,
            CuposOcupados = grupo.CuposOcupados,
            CuposDisponibles = disponibles,
            CupoCompleto = completo,
            AdmiteAltas = motivo is null,
            MotivoNoAdmiteAltas = motivo,
            Alumnos = alumnos
        };
    }

    private async Task<ResultadoOperacionCupo> VincularDentroDelBloqueoAsync(
        int asignacionAulaId,
        int personaId,
        CancellationToken ct)
    {
        var grupo = await _context.AsignacionesAula
            .FirstOrDefaultAsync(a => a.Id == asignacionAulaId, ct);

        if (grupo is null)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.NoEncontrado,
                "No se encontró el grupo solicitado.");
        }

        if (grupo.Estado != EstadosAsignacionAula.Abierta)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.GrupoCerrado,
                "El grupo está cerrado y no admite altas ni bajas de alumnos.");
        }

        if (grupo.FinVigencia <= DateTime.UtcNow)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.GrupoVencido,
                "El grupo no admite altas porque su vigencia venció.");
        }

        var persona = await _context.Personas
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.Id == personaId, ct);

        if (persona is null || persona.Rol?.Nombre != RolesSistema.Alumno)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.AlumnoInexistente,
                "El alumno indicado no existe.");
        }

        if (!persona.Activo)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.AlumnoInactivo,
                "El alumno está inactivo y no puede ocupar un cupo.");
        }

        var tieneInscripcionActiva = await _context.Inscripciones.AnyAsync(i =>
            i.PersonaId == personaId &&
            i.MateriaId == grupo.MateriaId &&
            i.Estado.Nombre == EstadosInscripcionSistema.Activa, ct);

        if (!tieneInscripcionActiva)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.SinInscripcion,
                "El alumno no tiene una inscripción activa a la materia del grupo.");
        }

        var yaVinculado = await _context.InscripcionesGrupo.AnyAsync(v =>
            v.AsignacionAulaId == asignacionAulaId &&
            v.PersonaId == personaId &&
            v.FechaBaja == null, ct);

        if (yaVinculado)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.Duplicado,
                "El alumno ya está vinculado de forma activa a este grupo.");
        }

        var ocupados = await ContarOcupantesAsync(grupo, ct);
        if (ocupados >= grupo.CupoHabilitado)
        {
            return ResultadoOperacionCupo.Fallo(
                CodigosCupo.CupoCompleto,
                "El cupo está completo.");
        }

        var vinculo = new InscripcionGrupo
        {
            AsignacionAulaId = grupo.Id,
            PersonaId = personaId,
            FechaAsignacion = DateTime.UtcNow
        };

        _context.InscripcionesGrupo.Add(vinculo);
        await _context.SaveChangesAsync(ct);
        await RecalcularGrupoAsync(grupo, ct);

        return ResultadoOperacionCupo.Ok(MapearAlumno(vinculo, persona));
    }

    private async Task RecalcularGrupoAsync(AsignacionAula grupo, CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
        grupo.CuposOcupados = await ContarOcupantesAsync(grupo, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Vínculo activo + persona activa + inscripción Activa a la misma materia.
    /// </summary>
    private Task<int> ContarOcupantesAsync(AsignacionAula grupo, CancellationToken ct) =>
        _context.InscripcionesGrupo.CountAsync(v =>
            v.AsignacionAulaId == grupo.Id &&
            v.FechaBaja == null &&
            v.Persona.Activo &&
            _context.Inscripciones.Any(i =>
                i.PersonaId == v.PersonaId &&
                i.MateriaId == grupo.MateriaId &&
                i.Estado.Nombre == EstadosInscripcionSistema.Activa), ct);

    private async Task EjecutarEnTransaccionAsync(Func<Task> accion, CancellationToken ct)
    {
        if (_context.Database.CurrentTransaction is not null || !_context.Database.IsRelational())
        {
            await accion();
            return;
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);
        await accion();
        await transaccion.CommitAsync(ct);
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionAsync(CancellationToken ct)
    {
        if (_context.Database.CurrentTransaction is not null || !_context.Database.IsRelational())
            return null;

        return await _context.Database.BeginTransactionAsync(ct);
    }

    private static bool EsViolacionUnica(DbUpdateException ex)
    {
        for (var actual = ex.InnerException; actual is not null; actual = actual.InnerException)
        {
            if (actual.Message.Contains("23505", StringComparison.Ordinal) ||
                actual.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static AlumnoGrupoDto MapearAlumno(InscripcionGrupo vinculo, Persona? persona = null)
    {
        persona ??= vinculo.Persona;
        return new AlumnoGrupoDto
        {
            VinculoId = vinculo.Id,
            PersonaId = vinculo.PersonaId,
            NombreCompleto = persona is null
                ? string.Empty
                : $"{persona.Apellido}, {persona.Nombre}".Trim().Trim(','),
            Dni = persona?.Dni ?? string.Empty,
            FechaAsignacion = vinculo.FechaAsignacion
        };
    }
}
