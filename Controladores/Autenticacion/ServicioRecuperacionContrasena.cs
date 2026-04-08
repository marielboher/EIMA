using System.Security.Cryptography;
using System.Text;
using AccesoDatos;
using Controladores.Opciones;
using Entidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Controladores.Autenticacion;

public sealed class ServicioRecuperacionContrasena
{
    private readonly EimaDbContext _db;
    private readonly IPasswordHasher<CuentaUsuario> _passwordHasher;
    private readonly RecuperacionContrasenaOpciones _opciones;

    public ServicioRecuperacionContrasena(
        EimaDbContext db,
        IPasswordHasher<CuentaUsuario> passwordHasher,
        IOptions<RecuperacionContrasenaOpciones> opciones)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _opciones = opciones.Value;
    }

    public async Task<(bool ok, object body, int status)> SolicitarRecuperacionAsync(
        RecuperacionContrasenaSolicitud solicitud,
        CancellationToken ct)
    {
        var validacion = ValidadorAutenticacion.ValidarCorreoRecuperacion(solicitud);
        if (!validacion.EsValido)
            return (false, new { errores = validacion.Errores }, StatusCodes.Status400BadRequest);

        var correoNorm = NormalizarCorreo(solicitud.Correo);
        var cuenta = await _db.CuentasUsuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CorreoElectronico == correoNorm, ct);

        if (cuenta == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.Correo),
                        "No hay ninguna cuenta registrada con este correo electrónico. Verifique la dirección o regístrese.")
                }
            }, StatusCodes.Status404NotFound);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var pendientes = await _db.TokensRecuperacionContrasena
            .Where(t => t.CuentaUsuarioId == cuenta.Id && t.ConsumidoEnUtc == null)
            .ToListAsync(ct);
        if (pendientes.Count > 0)
            _db.TokensRecuperacionContrasena.RemoveRange(pendientes);

        var tokenPlano = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var hashToken = HashTokenPlano(tokenPlano);
        var ahora = DateTime.UtcNow;
        var minutos = Math.Clamp(_opciones.MinutosValidezToken, 1, 1440);

        _db.TokensRecuperacionContrasena.Add(new TokenRecuperacionContrasena
        {
            CuentaUsuarioId = cuenta.Id,
            HashToken = hashToken,
            CreadoEnUtc = ahora,
            ExpiraEnUtc = ahora.AddMinutes(minutos)
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var plantilla = string.IsNullOrWhiteSpace(_opciones.PlantillaEnlace)
            ? "{0}"
            : _opciones.PlantillaEnlace;
        var enlace = string.Format(plantilla, tokenPlano);

        return (true, new
        {
            mensaje =
                "Se ha generado el enlace de recuperación. Si configuró el envío por correo, recibirá las instrucciones en su bandeja.",
            enlaceRecuperacion = _opciones.IncluirEnlaceEnRespuesta ? enlace : null,
            expiraEnMinutos = minutos
        }, StatusCodes.Status200OK);
    }

    public async Task<(bool ok, object body, int status)> RestablecerContrasenaAsync(
        RestablecerContrasenaSolicitud solicitud,
        CancellationToken ct)
    {
        var validacion = ValidadorAutenticacion.ValidarRestablecerContrasena(solicitud);
        if (!validacion.EsValido)
            return (false, new { errores = validacion.Errores }, StatusCodes.Status400BadRequest);

        var hash = HashTokenPlano(solicitud.Token.Trim());
        var registro = await _db.TokensRecuperacionContrasena
            .Include(t => t.CuentaUsuario)
            .FirstOrDefaultAsync(
                t => t.HashToken == hash && t.ExpiraEnUtc > DateTime.UtcNow && t.ConsumidoEnUtc == null,
                ct);

        if (registro == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.Token),
                        "El enlace de recuperación no es válido, expiró o ya fue utilizado. Solicite uno nuevo.")
                }
            }, StatusCodes.Status400BadRequest);
        }

        registro.CuentaUsuario.HashContrasena =
            _passwordHasher.HashPassword(registro.CuentaUsuario, solicitud.NuevaContrasena);
        registro.ConsumidoEnUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (true, new
        {
            mensaje = "Su contraseña se actualizó correctamente. Ya puede iniciar sesión.",
            redireccionSugerida = "/login"
        }, StatusCodes.Status200OK);
    }

    private static string HashTokenPlano(string tokenPlano)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tokenPlano));
        return Convert.ToBase64String(bytes);
    }

    private static string NormalizarCorreo(string correo) =>
        correo.Trim().ToLowerInvariant();
}
