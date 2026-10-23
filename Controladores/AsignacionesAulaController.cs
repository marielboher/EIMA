using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.SuperAdmin + "," + RolesSistema.Administrativo)]
public class AsignacionesAulaController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonVistas = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly EimaDbContext _context;

    public AsignacionesAulaController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listado paginado de grupos de aula (HU23) con filtros (HU24/HU26),
    /// orden configurable (HU25) y rango de vigencia (HU26). Por defecto 25 registros por página.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] string? buscar,
        [FromQuery] int? aulaId,
        [FromQuery] int? materiaId,
        [FromQuery] int? docenteId,
        [FromQuery] string? ordenarPor,
        [FromQuery] string? direccion,
        [FromQuery] string? vigenciaDesde,
        [FromQuery] string? vigenciaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 25,
        CancellationToken ct = default)
    {
        if (pagina < 1) pagina = 1;
        if (limite < 1) limite = 25;
        if (limite > 100) limite = 100;

        var consulta = ConstruirConsulta(estado, buscar, aulaId, materiaId, docenteId, vigenciaDesde, vigenciaHasta);
        if (consulta.Error is not null)
            return BadRequest(new { mensaje = consulta.Error });

        var query = AplicarOrden(consulta.Query!, ordenarPor, direccion);

        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling(totalRegistros / (double)limite);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pagina > paginasTotales) pagina = paginasTotales;

        var datos = await query
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .Select(MapProjection())
            .ToListAsync(ct);

        return Ok(new
        {
            datos,
            paginaActual = pagina,
            limite,
            totalRegistros,
            paginasTotales
        });
    }

    /// <summary>Detalle de solo lectura para el panel/drawer sobre el listado (CA04).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var asignacion = await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapProjection())
            .FirstOrDefaultAsync(ct);

        return asignacion == null
            ? NotFound(new { mensaje = "No se encontró la asignación solicitada." })
            : Ok(asignacion);
    }

    /// <summary>
    /// Exporta el listado filtrado y ordenado (HU27) en CSV o XLSX, con la fecha de
    /// exportación y los filtros aplicados como metadatos.
    /// </summary>
    [HttpGet("exportar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar(
        [FromQuery] string? formato,
        [FromQuery] string? estado,
        [FromQuery] string? buscar,
        [FromQuery] int? aulaId,
        [FromQuery] int? materiaId,
        [FromQuery] int? docenteId,
        [FromQuery] string? ordenarPor,
        [FromQuery] string? direccion,
        [FromQuery] string? vigenciaDesde,
        [FromQuery] string? vigenciaHasta,
        CancellationToken ct = default)
    {
        var formatoNormalizado = string.IsNullOrWhiteSpace(formato) ? "csv" : formato.Trim().ToLowerInvariant();
        if (formatoNormalizado is not ("csv" or "xlsx"))
            return BadRequest(new { mensaje = "El formato debe ser csv o xlsx." });

        var consulta = ConstruirConsulta(estado, buscar, aulaId, materiaId, docenteId, vigenciaDesde, vigenciaHasta);
        if (consulta.Error is not null)
            return BadRequest(new { mensaje = consulta.Error });

        var query = AplicarOrden(consulta.Query!, ordenarPor, direccion);
        var filas = await query.Select(MapProjection()).ToListAsync(ct);

        var materiaNombre = materiaId is > 0
            ? await _context.Materias.AsNoTracking()
                .Where(m => m.Id == materiaId.Value)
                .Select(m => m.Nombre)
                .FirstOrDefaultAsync(ct)
            : null;

        var docenteNombre = docenteId is > 0
            ? await _context.Personas.AsNoTracking()
                .Where(p => p.Id == docenteId.Value)
                .Select(p => (p.Apellido + ", " + p.Nombre).Trim())
                .FirstOrDefaultAsync(ct)
            : null;

        var metadatos = new MetadatosExportacion
        {
            FechaExportacionUtc = DateTime.UtcNow,
            Estado = string.IsNullOrWhiteSpace(estado) || estado.Equals("todos", StringComparison.OrdinalIgnoreCase)
                ? null
                : estado.Trim(),
            Materia = materiaNombre,
            Docente = docenteNombre,
            Texto = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim(),
            VigenciaDesdeUtc = TryParseIsoUtc(vigenciaDesde, out var desdeUtc) ? desdeUtc : null,
            VigenciaHastaUtc = TryParseIsoUtc(vigenciaHasta, out var hastaUtc) ? hastaUtc : null,
            Orden = new OrdenListadoAsignacionesDto
            {
                Campo = string.IsNullOrWhiteSpace(ordenarPor) ? "fechaAsignacion" : ordenarPor.Trim(),
                Direccion = string.IsNullOrWhiteSpace(direccion) ? "desc" : direccion.Trim()
            },
            TotalFilas = filas.Count
        };

        var nombreArchivo = $"asignaciones-aula-{ZonaHorariaArgentina.FormatearFecha(metadatos.FechaExportacionUtc).Replace('/', '-')}";

        if (formatoNormalizado == "csv")
        {
            var csv = ExportadorAsignacionesAula.ConstruirCsv(filas, metadatos);
            return File(csv, "text/csv; charset=utf-8", $"{nombreArchivo}.csv");
        }

        var xlsx = ExportadorAsignacionesAula.ConstruirXlsx(filas, metadatos);
        return File(
            xlsx,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{nombreArchivo}.xlsx");
    }

    /// <summary>Lista las vistas guardadas del usuario autenticado (HU25, CA02/CA03).</summary>
    [HttpGet("vistas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VistaListadoDto>>> GetVistas(CancellationToken ct)
    {
        if (!TryObtenerPersonaId(out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var vistas = await _context.VistasListadoAsignaciones
            .AsNoTracking()
            .Where(v => v.PersonaId == personaId)
            .OrderBy(v => v.Nombre)
            .ToListAsync(ct);

        return Ok(vistas.Select(MapearVista).ToList());
    }

    /// <summary>Guarda (o actualiza por nombre) una vista del listado (HU25, CA02).</summary>
    [HttpPost("vistas")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GuardarVista([FromBody] GuardarVistaListadoDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!TryObtenerPersonaId(out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var errorOrden = ValidarOrden(dto.Orden);
        if (errorOrden is not null)
            return BadRequest(new { mensaje = errorOrden });

        var nombre = dto.Nombre.Trim();
        if (nombre.Length == 0)
            return BadRequest(new { mensaje = "El nombre de la vista es obligatorio." });

        var filtrosJson = JsonSerializer.Serialize(dto.Filtros ?? new FiltrosListadoAsignacionesDto(), JsonVistas);
        var ordenJson = JsonSerializer.Serialize(dto.Orden ?? new OrdenListadoAsignacionesDto(), JsonVistas);

        var nombreNormalizado = nombre.ToLowerInvariant();
        var existente = await _context.VistasListadoAsignaciones
            .FirstOrDefaultAsync(v => v.PersonaId == personaId && v.Nombre.ToLower() == nombreNormalizado, ct);

        if (existente is not null)
        {
            existente.FiltrosJson = filtrosJson;
            existente.OrdenJson = ordenJson;
            existente.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return Ok(MapearVista(existente));
        }

        var entidad = new VistaListadoAsignacion
        {
            PersonaId = personaId,
            Nombre = nombre,
            FiltrosJson = filtrosJson,
            OrdenJson = ordenJson,
            FechaCreacion = DateTime.UtcNow
        };

        _context.VistasListadoAsignaciones.Add(entidad);
        await _context.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, MapearVista(entidad));
    }

    /// <summary>Elimina una vista propia (HU25).</summary>
    [HttpDelete("vistas/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarVista(int id, CancellationToken ct)
    {
        if (!TryObtenerPersonaId(out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var vista = await _context.VistasListadoAsignaciones
            .FirstOrDefaultAsync(v => v.Id == id && v.PersonaId == personaId, ct);

        if (vista is null)
            return NotFound(new { mensaje = "No se encontró la vista solicitada." });

        _context.VistasListadoAsignaciones.Remove(vista);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Opciones del formulario: aulas activas, materias activas y docentes activos.</summary>
    [HttpGet("opciones")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<OpcionesAsignacionAulaDto>> GetOpciones(CancellationToken ct)
    {
        var aulas = await _context.Aulas
            .AsNoTracking()
            .Where(a => a.Activa)
            .OrderBy(a => a.Nombre)
            .Select(a => new AulaOpcionDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Capacidad = a.Capacidad
            })
            .ToListAsync(ct);

        var materias = await _context.Materias
            .AsNoTracking()
            .Where(m => m.Activa)
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaOpcionDto
            {
                Id = m.Id,
                Nombre = m.Nombre
            })
            .ToListAsync(ct);

        var docentes = await _context.Personas
            .AsNoTracking()
            .Where(p => p.Activo && p.Rol.Nombre == RolesSistema.Profesor)
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .Select(p => new DocenteOpcionDto
            {
                Id = p.Id,
                NombreCompleto = (p.Apellido + ", " + p.Nombre).Trim(),
                Dni = p.Dni
            })
            .ToListAsync(ct);

        return Ok(new OpcionesAsignacionAulaDto
        {
            Aulas = aulas,
            Materias = materias,
            Docentes = docentes
        });
    }

    /// <summary>Crea la asignación en estado Abierta, con fechaAsignacion y cuposOcupados = 0.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] GuardarAsignacionAulaDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (dto.FinVigencia is null)
        {
            ModelState.AddModelError(nameof(dto.FinVigencia), "El fin de vigencia es obligatorio.");
            return ValidationProblem(ModelState);
        }

        var finVigencia = AsUtc(dto.FinVigencia.Value);
        if (finVigencia <= DateTime.UtcNow)
            return BadRequest(new { mensaje = "El fin de vigencia debe ser posterior a la fecha y hora actual." });

        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == dto.AulaId, ct);
        if (aula == null)
            return BadRequest(new { mensaje = "El aula indicada no existe." });

        if (!aula.Activa)
            return BadRequest(new { mensaje = "Solo se puede asignar un aula activa." });

        if (dto.CupoHabilitado <= 0)
            return BadRequest(new { mensaje = "El cupo habilitado debe ser un entero mayor que 0." });

        if (dto.CupoHabilitado > aula.Capacidad)
        {
            return BadRequest(new
            {
                mensaje = $"El cupo habilitado no puede superar la capacidad del aula ({aula.Capacidad})."
            });
        }

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == dto.MateriaId, ct);
        if (materia == null)
            return BadRequest(new { mensaje = "La materia indicada no existe." });

        if (!materia.Activa)
            return BadRequest(new { mensaje = "Solo se puede asignar una materia activa." });

        var docente = await _context.Personas
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.Id == dto.DocenteId, ct);

        if (docente == null || docente.Rol?.Nombre != RolesSistema.Profesor)
            return BadRequest(new { mensaje = "El docente indicado no existe." });

        if (!docente.Activo)
            return BadRequest(new { mensaje = "Solo se puede asignar un docente activo." });

        var entidad = new AsignacionAula
        {
            AulaId = aula.Id,
            MateriaId = materia.Id,
            DocenteId = docente.Id,
            Estado = EstadosAsignacionAula.Abierta,
            CupoHabilitado = dto.CupoHabilitado,
            CuposOcupados = 0,
            FechaAsignacion = DateTime.UtcNow,
            FinVigencia = finVigencia
        };

        _context.AsignacionesAula.Add(entidad);
        await _context.SaveChangesAsync(ct);

        var creada = await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => a.Id == entidad.Id)
            .Select(MapProjection())
            .FirstAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, creada);
    }

    /// <summary>
    /// Cierre de la asignación (CA07). Registra fechaCierre una sola vez;
    /// no se reabre el mismo registro.
    /// </summary>
    [HttpPatch("{id:int}/cerrar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cerrar(int id, CancellationToken ct)
    {
        var asignacion = await _context.AsignacionesAula.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (asignacion == null)
            return NotFound(new { mensaje = "No se encontró la asignación solicitada." });

        if (asignacion.Estado == EstadosAsignacionAula.Cerrada)
            return BadRequest(new { mensaje = "La asignación ya se encuentra cerrada." });

        asignacion.Estado = EstadosAsignacionAula.Cerrada;
        asignacion.FechaCierre = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var cerrada = await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapProjection())
            .FirstAsync(ct);

        return Ok(cerrada);
    }

    /// <summary>
    /// Aplica los filtros comunes del listado y la exportación (HU24/HU26).
    /// Devuelve la consulta o el mensaje de error de validación.
    /// </summary>
    private (IQueryable<AsignacionAula>? Query, string? Error) ConstruirConsulta(
        string? estado,
        string? buscar,
        int? aulaId,
        int? materiaId,
        int? docenteId,
        string? vigenciaDesde,
        string? vigenciaHasta)
    {
        IQueryable<AsignacionAula> query = _context.AsignacionesAula.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var estadoNorm = EstadosAsignacionAula.Todos
                .FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));

            if (estadoNorm is null)
                return (null, "El estado debe ser Abierta o Cerrada.");

            query = query.Where(a => a.Estado == estadoNorm);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Aula.Nombre.ToLower().Contains(term) ||
                a.Materia.Nombre.ToLower().Contains(term) ||
                a.Docente.Nombre.ToLower().Contains(term) ||
                a.Docente.Apellido.ToLower().Contains(term) ||
                a.Docente.Dni.Contains(term));
        }

        if (aulaId is > 0)
            query = query.Where(a => a.AulaId == aulaId.Value);

        if (materiaId is > 0)
            query = query.Where(a => a.MateriaId == materiaId.Value);

        if (docenteId is > 0)
            query = query.Where(a => a.DocenteId == docenteId.Value);

        var tieneDesde = !string.IsNullOrWhiteSpace(vigenciaDesde);
        var tieneHasta = !string.IsNullOrWhiteSpace(vigenciaHasta);

        if (tieneDesde)
        {
            if (!TryParseIsoUtc(vigenciaDesde, out var desde))
                return (null, "La fecha 'desde' no es válida.");

            query = query.Where(a => a.FinVigencia >= desde);
        }

        if (tieneHasta)
        {
            if (!TryParseIsoUtc(vigenciaHasta, out var hasta))
                return (null, "La fecha 'hasta' no es válida.");

            query = query.Where(a => a.FinVigencia <= hasta);
        }

        if (tieneDesde && tieneHasta &&
            TryParseIsoUtc(vigenciaDesde, out var desdeUtc) &&
            TryParseIsoUtc(vigenciaHasta, out var hastaUtc) &&
            desdeUtc > hastaUtc)
        {
            return (null, "La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
        }

        return (query, null);
    }

    /// <summary>Orden configurable del listado y la exportación (HU25).</summary>
    private static IQueryable<AsignacionAula> AplicarOrden(
        IQueryable<AsignacionAula> query,
        string? ordenarPor,
        string? direccion)
    {
        if (string.IsNullOrWhiteSpace(ordenarPor))
        {
            return query
                .OrderByDescending(a => a.FechaAsignacion)
                .ThenByDescending(a => a.Id);
        }

        var ascendente = !string.Equals(direccion, "desc", StringComparison.OrdinalIgnoreCase);

        return ordenarPor.Trim().ToLowerInvariant() switch
        {
            "vigencia" => ascendente
                ? query.OrderBy(a => a.FinVigencia).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.FinVigencia).ThenByDescending(a => a.Id),

            "estado" => ascendente
                ? query.OrderBy(a => a.Estado).ThenBy(a => a.FinVigencia).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.Estado).ThenByDescending(a => a.FinVigencia).ThenByDescending(a => a.Id),

            "aula" => ascendente
                ? query.OrderBy(a => a.Aula.Nombre).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.Aula.Nombre).ThenByDescending(a => a.Id),

            "materia" => ascendente
                ? query.OrderBy(a => a.Materia.Nombre).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.Materia.Nombre).ThenByDescending(a => a.Id),

            "docente" => ascendente
                ? query.OrderBy(a => a.Docente.Apellido).ThenBy(a => a.Docente.Nombre).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.Docente.Apellido).ThenByDescending(a => a.Docente.Nombre).ThenByDescending(a => a.Id),

            "cupo" => ascendente
                ? query.OrderBy(a => a.CupoHabilitado).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.CupoHabilitado).ThenByDescending(a => a.Id),

            "id" => ascendente
                ? query.OrderBy(a => a.Id)
                : query.OrderByDescending(a => a.Id),

            _ => ascendente
                ? query.OrderBy(a => a.FechaAsignacion).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.FechaAsignacion).ThenByDescending(a => a.Id)
        };
    }

    private static string? ValidarOrden(OrdenListadoAsignacionesDto? orden)
    {
        if (orden is null)
            return null;

        var camposValidos = new[]
        {
            "vigencia", "estado", "aula", "materia", "docente", "cupo", "id", "fechaAsignacion"
        };

        if (!string.IsNullOrWhiteSpace(orden.Campo) &&
            !camposValidos.Contains(orden.Campo.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "El campo de orden no es válido.";
        }

        if (!string.IsNullOrWhiteSpace(orden.Direccion) &&
            !orden.Direccion.Trim().Equals("asc", StringComparison.OrdinalIgnoreCase) &&
            !orden.Direccion.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            return "La dirección del orden debe ser asc o desc.";
        }

        return null;
    }

    private bool TryObtenerPersonaId(out int personaId)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return int.TryParse(idStr, out personaId);
    }

    private static VistaListadoDto MapearVista(VistaListadoAsignacion vista) => new()
    {
        Id = vista.Id,
        Nombre = vista.Nombre,
        Filtros = Deserializar(vista.FiltrosJson, new FiltrosListadoAsignacionesDto()),
        Orden = Deserializar(vista.OrdenJson, new OrdenListadoAsignacionesDto()),
        FechaCreacion = vista.FechaCreacion,
        FechaActualizacion = vista.FechaActualizacion
    };

    private static T Deserializar<T>(string? json, T porDefecto) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return porDefecto;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonVistas) ?? porDefecto;
        }
        catch (JsonException)
        {
            return porDefecto;
        }
    }

    /// <summary>Convierte un valor ISO (con zona) al instante UTC que guarda Postgres.</summary>
    private static bool TryParseIsoUtc(string? valor, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        if (!DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return false;

        utc = dto.UtcDateTime;
        return true;
    }

    /// <summary>
    /// Npgsql exige UTC para columnas <c>timestamp with time zone</c>.
    /// El front envía <c>datetime-local</c> sin zona horaria.
    /// </summary>
    private static DateTime AsUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private static Expression<Func<AsignacionAula, AsignacionAulaListadoDto>> MapProjection() =>
        a => new AsignacionAulaListadoDto
        {
            Id = a.Id,
            AulaId = a.AulaId,
            AulaNombre = a.Aula.Nombre,
            CapacidadAula = a.Aula.Capacidad,
            MateriaId = a.MateriaId,
            MateriaNombre = a.Materia.Nombre,
            DocenteId = a.DocenteId,
            DocenteNombre = (a.Docente.Apellido + ", " + a.Docente.Nombre).Trim(),
            DocenteDni = a.Docente.Dni,
            Estado = a.Estado,
            CupoHabilitado = a.CupoHabilitado,
            CuposOcupados = a.CuposOcupados,
            FechaAsignacion = a.FechaAsignacion,
            FinVigencia = a.FinVigencia,
            FechaCierre = a.FechaCierre
        };
}
