using AccesoDatos;
using Entidades;
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
