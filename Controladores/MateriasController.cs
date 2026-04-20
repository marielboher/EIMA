using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
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

        var resultado = new List<MateriaCatalogoAreaDto>();
        foreach (var (area, nombres) in MateriasCatalogoSemilla.Filas)
        {
            var items = new List<MateriaCatalogoItemDto>();
            foreach (var nombre in nombres)
            {
                var fila = materias.FirstOrDefault(m => m.Area == area && m.Nombre == nombre);
                if (fila != null)
                    items.Add(new MateriaCatalogoItemDto(fila.Id, fila.Nombre));
            }

            if (items.Count > 0)
                resultado.Add(new MateriaCatalogoAreaDto(area, items));
        }

        return Ok(resultado);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Materia>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Materias
            .AsSplitQuery()
            .Include(m => m.ProfesoresMaterias).ThenInclude(pm => pm.Docente)
            .Include(m => m.Inscripciones).ThenInclude(i => i.Persona)
            .Include(m => m.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(m => m.Clases).ThenInclude(c => c.Docente)
            .Include(m => m.Clases).ThenInclude(c => c.Aula)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Materia>> GetById(int id, CancellationToken ct)
    {
        var materia = await _context.Materias
            .AsSplitQuery()
            .Include(m => m.ProfesoresMaterias).ThenInclude(pm => pm.Docente)
            .Include(m => m.Inscripciones).ThenInclude(i => i.Persona)
            .Include(m => m.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(m => m.Clases).ThenInclude(c => c.Docente)
            .Include(m => m.Clases).ThenInclude(c => c.Aula)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return materia == null ? NotFound() : Ok(materia);
    }
}
