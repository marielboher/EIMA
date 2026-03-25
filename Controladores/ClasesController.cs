using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class ClasesController : ControllerBase
{
    private readonly EimaDbContext _context;

    public ClasesController(EimaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Clase>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Clases
            .AsSplitQuery()
            .Include(c => c.Materia)
            .Include(c => c.Profesor)
            .Include(c => c.Aula)
            .Include(c => c.Asistencias).ThenInclude(a => a.Alumno)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Clase>> GetById(int id, CancellationToken ct)
    {
        var clase = await _context.Clases
            .AsSplitQuery()
            .Include(c => c.Materia)
            .Include(c => c.Profesor)
            .Include(c => c.Aula)
            .Include(c => c.Asistencias).ThenInclude(a => a.Alumno)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return clase == null ? NotFound() : Ok(clase);
    }
}
