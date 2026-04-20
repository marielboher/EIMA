using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class ConsultasController : ControllerBase
{
    private const int MaxMateriaConsulta = 200;

    private readonly EimaDbContext _context;

    public ConsultasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Recibe consultas del formulario público de contacto y las persiste en <see cref="Consulta"/>.</summary>
    [AllowAnonymous]
    [HttpPost("publico")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearPublica([FromBody] CrearConsultaPublicaSolicitud solicitud, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var materia = await _context.Materias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == solicitud.MateriaId && m.Activa, ct);

        if (materia == null)
        {
            return BadRequest(new
            {
                mensaje = "La materia seleccionada no existe o no está disponible."
            });
        }

        var area = string.IsNullOrWhiteSpace(materia.Area) ? "Sin área" : materia.Area.Trim();
        var nombre = materia.Nombre.Trim();
        var textoMateria = $"{area} — {nombre}";
        if (textoMateria.Length > MaxMateriaConsulta)
            textoMateria = textoMateria[..MaxMateriaConsulta];

        var entidad = new Consulta
        {
            Nombre = solicitud.Nombre.Trim(),
            Apellido = solicitud.Apellido.Trim(),
            Email = solicitud.Email.Trim(),
            Telefono = solicitud.Telefono.Trim(),
            Materia = textoMateria,
            Asunto = solicitud.Asunto.Trim(),
            Mensaje = solicitud.Mensaje.Trim(),
            FechaConsulta = DateTime.UtcNow,
            Estado = false
        };

        _context.Consultas.Add(entidad);
        await _context.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new { id = entidad.Id });
    }
}
