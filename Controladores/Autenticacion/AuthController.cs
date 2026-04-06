using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Controladores.Autenticacion;

/// <summary>Registro de alumnos e inicio de sesión con JWT.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ServicioAutenticacion _auth;

    public AuthController(ServicioAutenticacion auth)
    {
        _auth = auth;
    }

    /// <summary>Alta autogestionada: incluye DNI real (solo dígitos en BD); por defecto <c>alumno</c>; <c>tipoRegistro: profesor</c> para docentes.</summary>
    [HttpPost("registro")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registro([FromBody] RegistroSolicitud solicitud, CancellationToken ct)
    {
        var (ok, body, status) = await _auth.RegistrarAsync(solicitud, ct);
        if (!ok)
            return StatusCode(status, body);

        return StatusCode(status, body);
    }

    /// <summary>Obtiene un JWT con email y contraseña.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginSolicitud solicitud, CancellationToken ct)
    {
        var (ok, body, status) = await _auth.IniciarSesionAsync(solicitud, ct);
        return StatusCode(status, body);
    }

    /// <summary>Evalúa la fortaleza de la contraseña para indicadores en tiempo real en el cliente (cuerpo POST para no exponer la clave en la URL).</summary>
    [HttpPost("fortaleza-contrasena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult FortalezaContrasena([FromBody] FortalezaSolicitud solicitud)
    {
        var nivel = ValidadorAutenticacion.CalcularFortalezaContrasena(solicitud.Contrasena);
        return Ok(new
        {
            nivel,
            descripcion = nivel switch
            {
                ValidadorAutenticacion.Fortaleza.MuyDebil => "Muy débil",
                ValidadorAutenticacion.Fortaleza.Debil => "Débil",
                ValidadorAutenticacion.Fortaleza.Media => "Media",
                ValidadorAutenticacion.Fortaleza.Fuerte => "Fuerte",
                _ => nivel
            }
        });
    }
}
