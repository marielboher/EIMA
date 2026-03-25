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
    public async Task<ActionResult<IEnumerable<Empleado>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Empleados
            .AsSplitQuery()
            .Include(e => e.Rol)
            .Include(e => e.TipoColaborador)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Empleado>> GetById(int id, CancellationToken ct)
    {
        var empleado = await _context.Empleados
            .AsSplitQuery()
            .Include(e => e.Rol)
            .Include(e => e.TipoColaborador)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return empleado == null ? NotFound() : Ok(empleado);
    }
}
