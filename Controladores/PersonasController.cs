using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public PersonasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista personas con perfiles TPT vinculados (empleado, alumno o profesor).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Persona>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Personas
            .AsSplitQuery()
            .Include(p => p.Empleado).ThenInclude(e => e.Rol)
            .Include(p => p.Empleado).ThenInclude(e => e.TipoColaborador)
            .Include(p => p.Alumno)
            .Include(p => p.Profesor)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var persona = await _context.Personas
            .AsSplitQuery()
            .Include(p => p.Empleado).ThenInclude(e => e.Rol)
            .Include(p => p.Empleado).ThenInclude(e => e.TipoColaborador)
            .Include(p => p.Alumno)
            .Include(p => p.Profesor)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return persona == null ? NotFound() : Ok(persona);
    }
}
