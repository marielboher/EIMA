using System.Security.Claims;
using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public PersonasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Datos de la persona autenticada (perfil): nombre, apellido, DNI, contacto y correo de cuenta.</summary>
    [Authorize]
    [HttpGet("mi-perfil")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MiPerfil(CancellationToken ct)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idStr, out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var persona = await _context.Personas
            .AsNoTracking()
            .Include(p => p.Rol)
            .Include(p => p.CuentaUsuario)
            .FirstOrDefaultAsync(p => p.Id == personaId, ct);

        if (persona == null)
            return NotFound(new { mensaje = "No se encontró el perfil asociado a la cuenta." });

        return Ok(new
        {
            personaId = persona.Id,
            nombre = persona.Nombre,
            apellido = persona.Apellido,
            dni = persona.Dni,
            telefono = persona.Telefono,
            direccion = persona.Direccion,
            correoElectronico = persona.CuentaUsuario?.CorreoElectronico ?? string.Empty,
            rol = persona.Rol?.Nombre ?? string.Empty
        });
    }

    /// <summary>Lista personas con rol, tipo de colaborador y cuenta. Opcionalmente filtra por <c>rolId</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Persona>>> GetAll([FromQuery] int? rolId, CancellationToken ct)
    {
        IQueryable<Persona> query = _context.Personas;
        if (rolId is int rid && rid > 0)
            query = query.Where(p => p.RolId == rid);

        var list = await query
            .AsSplitQuery()
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .Include(p => p.CuentaUsuario)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var persona = await _context.Personas
            .AsSplitQuery()
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .Include(p => p.CuentaUsuario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return persona == null ? NotFound() : Ok(persona);
    }
}
