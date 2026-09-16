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
public class MateriasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public MateriasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Materias agrupadas por <see cref="Materia.Area"/> para el formulario de contacto (público).</summary>
    [AllowAnonymous]
    [HttpGet("catalogo-por-area")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MateriaCatalogoAreaDto>>> GetCatalogoPorArea(CancellationToken ct)
    {
        var materias = await _context.Materias
            .AsNoTracking()
            .Where(m => m.Activa && m.Area != null && m.Area != "")
            .Select(m => new { m.Id, m.Nombre, Area = m.Area! })
            .ToListAsync(ct);

        var porArea = materias
            .GroupBy(m => m.Area)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(m => m.Nombre)
                    .Select(m => new MateriaCatalogoItemDto(m.Id, m.Nombre))
                    .ToList());

        var resultado = new List<MateriaCatalogoAreaDto>();
        var areasVistas = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (area, _) in MateriasCatalogoSemilla.Filas)
        {
            if (!porArea.TryGetValue(area, out var items) || items.Count == 0)
                continue;
            resultado.Add(new MateriaCatalogoAreaDto(area, items));
            areasVistas.Add(area);
        }

        foreach (var area in porArea.Keys.OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
        {
            if (areasVistas.Contains(area))
                continue;
            resultado.Add(new MateriaCatalogoAreaDto(area, porArea[area]));
        }

        return Ok(resultado);
    }

    /// <summary>Listado liviano de materias (ABM y asignación a profesores).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MateriaListadoDto>>> GetAll(
        [FromQuery] bool? soloActivas,
        CancellationToken ct)
    {
        var query = _context.Materias.AsNoTracking().AsQueryable();
        if (soloActivas == true)
            query = query.Where(m => m.Activa);

        var list = await query
            .OrderBy(m => m.Area)
            .ThenBy(m => m.Nombre)
            .Select(m => new MateriaListadoDto(
                m.Id,
                m.Nombre,
                m.Area,
                m.Descripcion,
                m.DuracionHoras,
                m.PrecioPorClase,
                m.Activa))
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MateriaListadoDto>> GetById(int id, CancellationToken ct)
    {
        var materia = await _context.Materias
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MateriaListadoDto(
                m.Id,
                m.Nombre,
                m.Area,
                m.Descripcion,
                m.DuracionHoras,
                m.PrecioPorClase,
                m.Activa))
            .FirstOrDefaultAsync(ct);

        return materia == null ? NotFound() : Ok(materia);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MateriaListadoDto>> Crear([FromBody] GuardarMateriaDto dto, CancellationToken ct)
    {
        var error = Validar(dto);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var nombre = dto.Nombre.Trim();
        var area = NormalizarArea(dto.Area);

        var duplicada = await _context.Materias.AnyAsync(
            m => m.Nombre.ToLower() == nombre.ToLower() &&
                 ((m.Area ?? "") == (area ?? "")),
            ct);
        if (duplicada)
            return BadRequest(new { mensaje = $"Ya existe la materia \"{nombre}\" en el área indicada." });

        var materia = new Materia
        {
            Nombre = nombre,
            Area = area,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            DuracionHoras = Math.Max(0, dto.DuracionHoras),
            PrecioPorClase = dto.PrecioPorClase < 0 ? 0 : dto.PrecioPorClase,
            Activa = dto.Activa
        };

        _context.Materias.Add(materia);
        await _context.SaveChangesAsync(ct);

        var creado = ToDto(materia);
        return CreatedAtAction(nameof(GetById), new { id = materia.Id }, creado);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MateriaListadoDto>> Editar(int id, [FromBody] GuardarMateriaDto dto, CancellationToken ct)
    {
        var error = Validar(dto);
        if (error != null)
            return BadRequest(new { mensaje = error });

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia == null)
            return NotFound();

        var nombre = dto.Nombre.Trim();
        var area = NormalizarArea(dto.Area);

        var duplicada = await _context.Materias.AnyAsync(
            m => m.Id != id &&
                 m.Nombre.ToLower() == nombre.ToLower() &&
                 ((m.Area ?? "") == (area ?? "")),
            ct);
        if (duplicada)
            return BadRequest(new { mensaje = $"Ya existe la materia \"{nombre}\" en el área indicada." });

        materia.Nombre = nombre;
        materia.Area = area;
        materia.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        materia.DuracionHoras = Math.Max(0, dto.DuracionHoras);
        materia.PrecioPorClase = dto.PrecioPorClase < 0 ? 0 : dto.PrecioPorClase;
        materia.Activa = dto.Activa;

        await _context.SaveChangesAsync(ct);
        return Ok(ToDto(materia));
    }

    /// <summary>Baja lógica: marca la materia como inactiva.</summary>
    [HttpPatch("{id:int}/cambiar-estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MateriaListadoDto>> CambiarEstado(int id, CancellationToken ct)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia == null)
            return NotFound();

        materia.Activa = !materia.Activa;
        await _context.SaveChangesAsync(ct);
        return Ok(ToDto(materia));
    }

    private static string? Validar(GuardarMateriaDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Nombre))
            return "El nombre de la materia es obligatorio.";
        if (dto.Nombre.Trim().Length > 200)
            return "El nombre no puede superar 200 caracteres.";
        if (!string.IsNullOrWhiteSpace(dto.Area) && dto.Area.Trim().Length > 150)
            return "El área no puede superar 150 caracteres.";
        if (!string.IsNullOrWhiteSpace(dto.Descripcion) && dto.Descripcion.Trim().Length > 2000)
            return "La descripción no puede superar 2000 caracteres.";
        return null;
    }

    private static string? NormalizarArea(string? area) =>
        string.IsNullOrWhiteSpace(area) ? null : area.Trim();

    private static MateriaListadoDto ToDto(Materia m) =>
        new(m.Id, m.Nombre, m.Area, m.Descripcion, m.DuracionHoras, m.PrecioPorClase, m.Activa);
}
