using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.SuperAdmin + "," + RolesSistema.Administrativo)]
public class AsignacionesAulaController : ControllerBase
{
    private readonly EimaDbContext _context;

    public AsignacionesAulaController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listado paginado de grupos de aula (HU23). Por defecto 25 registros por página.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] string? buscar,
        [FromQuery] int? aulaId,
        [FromQuery] int? materiaId,
        [FromQuery] int? docenteId,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 25,
        CancellationToken ct = default)
    {
        if (pagina < 1) pagina = 1;
        if (limite < 1) limite = 25;
        if (limite > 100) limite = 100;

        IQueryable<AsignacionAula> query = _context.AsignacionesAula.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var estadoNorm = EstadosAsignacionAula.Todos
                .FirstOrDefault(e => e.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));

            if (estadoNorm is null)
                return BadRequest(new { mensaje = "El estado debe ser Abierta o Cerrada." });

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

        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling(totalRegistros / (double)limite);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pagina > paginasTotales) pagina = paginasTotales;

        var datos = await query
            .OrderByDescending(a => a.FechaAsignacion)
            .ThenByDescending(a => a.Id)
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
