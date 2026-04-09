using System.Security.Claims;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Controladores.Admin;

/// <summary>Operaciones de administración (solo <c>super_admin</c>).</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AdminController : ControllerBase
{
    private readonly ServicioCambioRolAdmin _cambioRol;

    public AdminController(ServicioCambioRolAdmin cambioRol)
    {
        _cambioRol = cambioRol;
    }

    /// <summary>Cambia el rol de la persona asociada a una cuenta por correo (ej. juan@gmail.com → profesor).</summary>
    [Authorize(Roles = RolesSistema.SuperAdmin)]
    [HttpPatch("usuarios/rol")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarRolUsuario(
        [FromBody] CambiarRolUsuarioSolicitud solicitud,
        CancellationToken ct)
    {
        var idActor = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idActor, out var realizadoPorPersonaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario que realiza la operación." });

        var (ok, body, status) =
            await _cambioRol.CambiarRolPorCorreoAsync(solicitud, realizadoPorPersonaId, ct);
        return StatusCode(status, body);
    }
}
