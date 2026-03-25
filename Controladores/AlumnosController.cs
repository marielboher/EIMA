using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class AlumnosController : ControllerBase
{
    private readonly EimaDbContext _context;

    public AlumnosController(EimaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alumno>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Alumnos
            .AsSplitQuery()
            .Include(a => a.Inscripciones).ThenInclude(i => i.Materia)
            .Include(a => a.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(a => a.Pagos)
            .Include(a => a.Asistencias).ThenInclude(asist => asist.Clase)
            .Include(a => a.Consultas).ThenInclude(c => c.Materia)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Alumno>> GetById(int id, CancellationToken ct)
    {
        var alumno = await _context.Alumnos
            .AsSplitQuery()
            .Include(a => a.Inscripciones).ThenInclude(i => i.Materia)
            .Include(a => a.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(a => a.Pagos)
            .Include(a => a.Asistencias).ThenInclude(asist => asist.Clase)
            .Include(a => a.Consultas).ThenInclude(c => c.Materia)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return alumno == null ? NotFound() : Ok(alumno);
    }
}
