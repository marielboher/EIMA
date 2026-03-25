using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class ProfesoresController : ControllerBase
{
    private readonly EimaDbContext _context;

    public ProfesoresController(EimaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Profesor>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Profesores
            .AsSplitQuery()
            .Include(p => p.ProfesoresMaterias).ThenInclude(pm => pm.Materia)
            .Include(p => p.HorariosDisponibles)
            .Include(p => p.Clases).ThenInclude(c => c.Materia)
            .Include(p => p.Clases).ThenInclude(c => c.Aula)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Profesor>> GetById(int id, CancellationToken ct)
    {
        var profesor = await _context.Profesores
            .AsSplitQuery()
            .Include(p => p.ProfesoresMaterias).ThenInclude(pm => pm.Materia)
            .Include(p => p.HorariosDisponibles)
            .Include(p => p.Clases).ThenInclude(c => c.Materia)
            .Include(p => p.Clases).ThenInclude(c => c.Aula)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return profesor == null ? NotFound() : Ok(profesor);
    }
}
