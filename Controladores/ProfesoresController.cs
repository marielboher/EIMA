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
    public async Task<ActionResult<IEnumerable<Persona>>> GetAll(CancellationToken ct)
    {
        var list = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.Rol.Nombre == RolesSistema.Profesor)
            .Include(p => p.ProfesoresMaterias).ThenInclude(pm => pm.Materia)
            .Include(p => p.HorariosDisponibles)
            .Include(p => p.ClasesComoDocente).ThenInclude(c => c.Materia)
            .Include(p => p.ClasesComoDocente).ThenInclude(c => c.Aula)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var profesor = await _context.Personas
            .AsSplitQuery()
            .Where(p => p.Rol.Nombre == RolesSistema.Profesor)
            .Include(p => p.ProfesoresMaterias).ThenInclude(pm => pm.Materia)
            .Include(p => p.HorariosDisponibles)
            .Include(p => p.ClasesComoDocente).ThenInclude(c => c.Materia)
            .Include(p => p.ClasesComoDocente).ThenInclude(c => c.Aula)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return profesor == null ? NotFound() : Ok(profesor);
    }
}
