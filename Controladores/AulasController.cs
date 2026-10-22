using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.SuperAdmin + "," + RolesSistema.Administrativo)]
public class AulasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public AulasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Catálogo de aulas (HU22). Sin pagina/limite devuelve un array plano;
    /// con paginación devuelve { datos, paginaActual, limite, totalRegistros, paginasTotales }.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? buscar,
        [FromQuery] string? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? limite,
        CancellationToken ct = default)
    {
        IQueryable<Aula> query = _context.Aulas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.Nombre.ToLower().Contains(term) ||
                (a.Ubicacion != null && a.Ubicacion.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var activa = estado.Trim().Equals("activo", StringComparison.OrdinalIgnoreCase)
                         || estado.Trim().Equals("activa", StringComparison.OrdinalIgnoreCase);
            query = query.Where(a => a.Activa == activa);
        }

        query = query.OrderBy(a => a.Nombre);

        // Sin paginación: respuesta plana (selectores / listados simples).
        if (pagina is null && limite is null)
        {
            var todas = await query.Select(MapProjection()).ToListAsync(ct);
            return Ok(todas);
        }

        var lim = limite is null or < 1 ? 10 : Math.Min(limite.Value, 100);
        var pag = pagina is null or < 1 ? 1 : pagina.Value;

        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling(totalRegistros / (double)lim);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pag > paginasTotales) pag = paginasTotales;

        var datos = await query
            .Skip((pag - 1) * lim)
            .Take(lim)
            .Select(MapProjection())
            .ToListAsync(ct);

        return Ok(new
        {
            datos,
            paginaActual = pag,
            limite = lim,
            totalRegistros,
            paginasTotales
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var aula = await _context.Aulas
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapProjection())
            .FirstOrDefaultAsync(ct);

        return aula == null
            ? NotFound(new { mensaje = "No se encontró el aula solicitada." })
            : Ok(aula);
    }

    /// <summary>Alta de aula. Siempre se crea en estado Activa (CA02).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] GuardarAulaDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError(nameof(dto.Nombre), "El nombre es obligatorio.");
            return ValidationProblem(ModelState);
        }

        if (await ExisteNombreDuplicadoAsync(nombre, excluirId: null, ct))
        {
            return BadRequest(new
            {
                mensaje = "Ya existe un aula con ese nombre (sin distinguir mayúsculas, minúsculas ni acentos)."
            });
        }

        var entidad = new Aula
        {
            Nombre = nombre,
            Capacidad = dto.Capacidad,
            Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim(),
            Activa = true
        };

        _context.Aulas.Add(entidad);
        await _context.SaveChangesAsync(ct);

        var creada = await _context.Aulas
            .AsNoTracking()
            .Where(a => a.Id == entidad.Id)
            .Select(MapProjection())
            .FirstAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, creada);
    }

    /// <summary>Edición de nombre, capacidad y ubicación. No modifica el estado (CA07).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int id, [FromBody] GuardarAulaDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula == null)
            return NotFound(new { mensaje = "No se encontró el aula solicitada." });

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError(nameof(dto.Nombre), "El nombre es obligatorio.");
            return ValidationProblem(ModelState);
        }

        if (await ExisteNombreDuplicadoAsync(nombre, excluirId: id, ct))
        {
            return BadRequest(new
            {
                mensaje = "Ya existe un aula con ese nombre (sin distinguir mayúsculas, minúsculas ni acentos)."
            });
        }

        var tieneAsignacionesAbiertas = await _context.AsignacionesAula
            .AnyAsync(x => x.AulaId == id && x.Estado == EstadosAsignacionAula.Abierta, ct);

        if (tieneAsignacionesAbiertas)
        {
            var cupoMaximoAbierto = await _context.AsignacionesAula
                .Where(x => x.AulaId == id && x.Estado == EstadosAsignacionAula.Abierta)
                .MaxAsync(x => x.CupoHabilitado, ct);

            if (dto.Capacidad < cupoMaximoAbierto)
            {
                return BadRequest(new
                {
                    mensaje = "La nueva capacidad no puede ser menor al cupo habilitado de una asignación abierta."
                });
            }
        }

        aula.Nombre = nombre;
        aula.Capacidad = dto.Capacidad;
        aula.Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim();

        await _context.SaveChangesAsync(ct);

        var actualizada = await _context.Aulas
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapProjection())
            .FirstAsync(ct);

        return Ok(actualizada);
    }

    /// <summary>
    /// Baja/alta lógica (CA09). La inactivación se bloquea si el aula tiene
    /// al menos una asignación en estado Abierta (CA10).
    /// </summary>
    [HttpPatch("{id:int}/cambiar-estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(int id, CancellationToken ct)
    {
        var aula = await _context.Aulas.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (aula == null)
            return NotFound(new { mensaje = "No se encontró el aula solicitada." });

        if (aula.Activa)
        {
            var abiertas = await _context.AsignacionesAula
                .CountAsync(x => x.AulaId == id && x.Estado == EstadosAsignacionAula.Abierta, ct);

            if (abiertas > 0)
            {
                return BadRequest(new
                {
                    mensaje = $"No se puede inactivar el aula: tiene {abiertas} asignación(es) abierta(s). " +
                              "Cerrá o reasigná esas asignaciones antes."
                });
            }
        }

        aula.Activa = !aula.Activa;
        await _context.SaveChangesAsync(ct);

        return Ok(new { id = aula.Id, activa = aula.Activa });
    }

    private async Task<bool> ExisteNombreDuplicadoAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        var nombreNorm = TextoNormalizador.Normalizar(nombre);
        var nombres = await _context.Aulas
            .AsNoTracking()
            .Where(a => excluirId == null || a.Id != excluirId.Value)
            .Select(a => a.Nombre)
            .ToListAsync(ct);

        return nombres.Any(n => TextoNormalizador.Normalizar(n) == nombreNorm);
    }

    private static System.Linq.Expressions.Expression<Func<Aula, AulaListadoDto>> MapProjection() =>
        a => new AulaListadoDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            Capacidad = a.Capacidad,
            Ubicacion = a.Ubicacion,
            Activa = a.Activa,
            AsignacionesAbiertas = a.AsignacionesAula.Count(x => x.Estado == EstadosAsignacionAula.Abierta)
        };
}
