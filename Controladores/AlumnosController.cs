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
    public async Task<ActionResult<IEnumerable<Persona>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.Rol.Nombre == RolesSistema.Alumno)
            .Include(p => p.Inscripciones).ThenInclude(i => i.Materia)
            .Include(p => p.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(p => p.Pagos)
            .Include(p => p.Asistencias).ThenInclude(asist => asist.Clase)
            .Include(p => p.Consultas)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var alumno = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.Rol.Nombre == RolesSistema.Alumno)
            .Include(p => p.Inscripciones).ThenInclude(i => i.Materia)
            .Include(p => p.Inscripciones).ThenInclude(i => i.Pagos)
            .Include(p => p.Pagos)
            .Include(p => p.Asistencias).ThenInclude(asist => asist.Clase)
            .Include(p => p.Consultas)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return alumno == null ? NotFound() : Ok(alumno);
    }
}
