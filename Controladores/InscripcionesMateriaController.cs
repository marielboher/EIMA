using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class InscripcionesMateriaController : ControllerBase
{
    private readonly EimaDbContext _context;

    public InscripcionesMateriaController(EimaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InscripcionMateria>>> GetAll(CancellationToken ct)
    {
        var list = await _context.InscripcionesMateria
            .AsSplitQuery()
            .Include(i => i.Persona)
            .Include(i => i.Materia)
            .Include(i => i.Pagos)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InscripcionMateria>> GetById(int id, CancellationToken ct)
    {
        var inscripcion = await _context.InscripcionesMateria
            .AsSplitQuery()
            .Include(i => i.Persona)
            .Include(i => i.Materia)
            .Include(i => i.Pagos)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        return inscripcion == null ? NotFound() : Ok(inscripcion);
    }
}
