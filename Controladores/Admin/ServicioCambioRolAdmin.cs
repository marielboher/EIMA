using System.Text.RegularExpressions;
using AccesoDatos;
using Controladores.Autenticacion;
using Entidades;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Controladores.Admin;

public sealed class ServicioCambioRolAdmin
{
    private static readonly Regex CorreoRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly EimaDbContext _db;

    public ServicioCambioRolAdmin(EimaDbContext db)
    {
        _db = db;
    }

    public async Task<(bool ok, object body, int status)> CambiarRolPorCorreoAsync(
        CambiarRolUsuarioSolicitud solicitud,
        int realizadoPorPersonaId,
        CancellationToken ct)
    {
        var errores = new List<ErrorCampo>();
        if (string.IsNullOrWhiteSpace(solicitud.Correo))
            errores.Add(new ErrorCampo(nameof(solicitud.Correo), "El correo electrónico es obligatorio."));
        else if (!CorreoRegex.IsMatch(solicitud.Correo.Trim()))
            errores.Add(new ErrorCampo(nameof(solicitud.Correo), "El correo no tiene un formato válido."));

        if (string.IsNullOrWhiteSpace(solicitud.NuevoRol))
            errores.Add(new ErrorCampo(nameof(solicitud.NuevoRol), "Debe indicar el nuevo rol."));
        else if (solicitud.NuevoRol.Trim().Equals(RolesSistema.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            errores.Add(new ErrorCampo(nameof(solicitud.NuevoRol),
                "No se puede asignar el rol de super administrador por esta operación."));

        if (errores.Count > 0)
            return (false, new { errores }, StatusCodes.Status400BadRequest);

        var correoNorm = solicitud.Correo.Trim().ToLowerInvariant();
        var nombreRolNorm = solicitud.NuevoRol.Trim().ToLowerInvariant();

        var cuenta = await _db.CuentasUsuarios
            .Include(c => c.Persona).ThenInclude(p => p.Rol)
        .FirstOrDefaultAsync(c => c.CorreoElectronico == correoNorm, ct);

        if (cuenta == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.Correo), "No existe ninguna cuenta con este correo electrónico.")
                }
            }, StatusCodes.Status404NotFound);
        }

        var rolDestino = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Nombre == nombreRolNorm, ct);
        if (rolDestino == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.NuevoRol),
                        $"No existe el rol \"{solicitud.NuevoRol.Trim()}\". Use un nombre válido del sistema (por ejemplo: profesor, alumno, secretaria).")
                }
            }, StatusCodes.Status400BadRequest);
        }

        var rolAnterior = cuenta.Persona.Rol.Nombre;
        cuenta.Persona.RolId = rolDestino.Id;

        if (nombreRolNorm == RolesSistema.Profesor && cuenta.Persona.FechaIngresoDocente == null)
            cuenta.Persona.FechaIngresoDocente = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return (true, new
        {
            mensaje = $"El rol del usuario se actualizó correctamente a \"{rolDestino.Nombre}\".",
            realizadoPorPersonaId = realizadoPorPersonaId,
            personaId = cuenta.PersonaId,
            correo = cuenta.CorreoElectronico,
            rolAnterior,
            rolNuevo = rolDestino.Nombre
        }, StatusCodes.Status200OK);
    }
}
