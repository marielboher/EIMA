using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly EimaDbContext _context;

    public EmpleadosController(EimaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Persona>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.TipoColaboradorId != null)
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var empleado = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.TipoColaboradorId != null)
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return empleado == null ? NotFound() : Ok(empleado);
    }
}
