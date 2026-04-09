using Controladores.Opciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Controladores.Autenticacion;

/// <summary>Registro de alumnos e inicio de sesión con JWT.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ServicioAutenticacion _auth;
    private readonly ServicioRecuperacionContrasena _recuperacion;
    private readonly JwtOpciones _jwt;

    public AuthController(
        ServicioAutenticacion auth,
        ServicioRecuperacionContrasena recuperacion,
        IOptions<JwtOpciones> jwtOpciones)
    {
        _auth = auth;
        _recuperacion = recuperacion;
        _jwt = jwtOpciones.Value;
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
        if (ok && body is LoginExitosoDto exito && _jwt.UsarCookieHttpOnly && !string.IsNullOrEmpty(exito.AccessToken))
        {
            var minutos = Math.Clamp(_jwt.MinutosExpiracion, 1, 1440);
            var opcionesCookie = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(minutos),
                Path = "/"
            };
            Response.Cookies.Append(_jwt.NombreCookieAccessToken, exito.AccessToken, opcionesCookie);
            exito.AccessToken = null;
        }

        return StatusCode(status, body);
    }

    /// <summary>Cierra sesión: elimina la cookie JWT si aplica; el cliente debe borrar el token Bearer en memoria y redirigir al login.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        var nombreCookie = string.IsNullOrWhiteSpace(_jwt.NombreCookieAccessToken)
            ? "eima_access_token"
            : _jwt.NombreCookieAccessToken;
        if (_jwt.UsarCookieHttpOnly || Request.Cookies.ContainsKey(nombreCookie))
        {
            Response.Cookies.Delete(nombreCookie, new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
        }

        return Ok(new
        {
            mensaje = "Sesión cerrada correctamente.",
            redireccionSugerida = "/login",
            debeEliminarTokenEnCliente = true
        });
    }

    /// <summary>Solicita recuperación: CA01 correo inexistente; CA02 token único, un solo uso, 30 min (configurable).</summary>
    [HttpPost("recuperar-contrasena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecuperarContrasena(
        [FromBody] RecuperacionContrasenaSolicitud solicitud,
        CancellationToken ct)
    {
        var (ok, body, status) = await _recuperacion.SolicitarRecuperacionAsync(solicitud, ct);
        return StatusCode(status, body);
    }

    /// <summary>Restablece contraseña con el token del enlace: CA03 política HU02; CA04 enlace ya usado o inválido; CA05 mensaje y ruta al login.</summary>
    [HttpPost("restablecer-contrasena")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RestablecerContrasena(
        [FromBody] RestablecerContrasenaSolicitud solicitud,
        CancellationToken ct)
    {
        var (ok, body, status) = await _recuperacion.RestablecerContrasenaAsync(solicitud, ct);
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
