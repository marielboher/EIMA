using AccesoDatos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly EimaDbContext _context;

    public RolesController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista roles del sistema (Id y Nombre) para filtros en cliente (por ejemplo Personas).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new { r.Id, r.Nombre, r.Descripcion })
            .ToListAsync(ct);
        return Ok(list);
    }
}
