using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Controladores.Opciones;

namespace Controladores.Autenticacion;

public sealed class ServicioAutenticacion
{
    private readonly EimaDbContext _db;
    private readonly IPasswordHasher<CuentaUsuario> _passwordHasher;
    private readonly JwtOpciones _jwt;

    public ServicioAutenticacion(
        EimaDbContext db,
        IPasswordHasher<CuentaUsuario> passwordHasher,
        IOptions<JwtOpciones> jwtOpciones)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwtOpciones.Value;
    }

    public async Task<(bool ok, object body, int status)> RegistrarAsync(RegistroSolicitud solicitud, CancellationToken ct)
    {
        var validacion = ValidadorAutenticacion.ValidarRegistro(solicitud);
        if (!validacion.EsValido)
            return (false, new { errores = validacion.Errores }, StatusCodes.Status400BadRequest);

        var tipo = ValidadorAutenticacion.NormalizarTipoRegistroPublico(solicitud.TipoRegistro);

        var correoNorm = NormalizarCorreo(solicitud.Correo);
        if (await _db.CuentasUsuarios.AnyAsync(c => c.CorreoElectronico == correoNorm, ct))
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.Correo),
                        "Ya existe una cuenta registrada con este correo electrónico. Inicie sesión o use otro correo.")
                }
            }, StatusCodes.Status400BadRequest);
        }

        var rolEntidad = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == tipo, ct);
        if (rolEntidad == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo("Sistema",
                        $"No está configurado el rol \"{tipo}\". Ejecute las migraciones o contacte al administrador.")
                }
            }, StatusCodes.Status500InternalServerError);
        }

        var dni = ValidadorAutenticacion.NormalizarDniParaAlmacenamiento(solicitud.Dni);
        if (await _db.Personas.AnyAsync(p => p.Dni == dni, ct))
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo(nameof(solicitud.Dni),
                        "Ya existe una persona registrada con este DNI. Verifique el número o contacte soporte.")
                }
            }, StatusCodes.Status400BadRequest);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var persona = new Persona
            {
                Nombre = solicitud.Nombre.Trim(),
                Apellido = solicitud.Apellido.Trim(),
                Dni = dni,
                Telefono = solicitud.Telefono.Trim(),
                Direccion = solicitud.Direccion.Trim(),
                FechaRegistro = DateTime.UtcNow,
                RolId = rolEntidad.Id,
                FechaIngresoDocente = tipo == RolesSistema.Profesor ? DateTime.UtcNow : null
            };
            _db.Personas.Add(persona);
            await _db.SaveChangesAsync(ct);

            var personaId = persona.Id;
            if (personaId == 0)
            {
                personaId = await _db.Personas.AsNoTracking()
                    .Where(p => p.Dni == dni)
                    .Select(p => p.Id)
                    .SingleOrDefaultAsync(ct);
            }

            if (personaId == 0)
                throw new InvalidOperationException("No se obtuvo el Id de la persona tras el alta.");

            var cuenta = new CuentaUsuario
            {
                PersonaId = personaId,
                CorreoElectronico = correoNorm,
                HashContrasena = _passwordHasher.HashPassword(new CuentaUsuario(), solicitud.Contrasena)
            };
            _db.CuentasUsuarios.Add(cuenta);
            await _db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return (true, new
        {
            mensaje = "Registro completado correctamente. Puede iniciar sesión con su correo y contraseña.",
            redireccionSugerida = "/login",
            tipoRegistro = tipo
        }, StatusCodes.Status201Created);
    }

    public async Task<(bool ok, object body, int status)> IniciarSesionAsync(LoginSolicitud solicitud, CancellationToken ct)
    {
        var validacion = ValidadorAutenticacion.ValidarLogin(solicitud);
        if (!validacion.EsValido)
            return (false, new { errores = validacion.Errores }, StatusCodes.Status400BadRequest);

        var correoNorm = NormalizarCorreo(solicitud.Correo);
        var cuenta = await _db.CuentasUsuarios
            .Include(c => c.Persona).ThenInclude(p => p!.Rol)
            .FirstOrDefaultAsync(c => c.CorreoElectronico == correoNorm, ct);

        if (cuenta == null)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo("Credenciales",
                        "Correo o contraseña incorrectos. Verifique los datos o registre una cuenta nueva.")
                }
            }, StatusCodes.Status401Unauthorized);
        }

        var resultado = _passwordHasher.VerifyHashedPassword(cuenta, cuenta.HashContrasena, solicitud.Contrasena);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return (false, new
            {
                errores = new[]
                {
                    new ErrorCampo("Credenciales",
                        "Correo o contraseña incorrectos. Verifique los datos o registre una cuenta nueva.")
                }
            }, StatusCodes.Status401Unauthorized);
        }

        var nombreRol = cuenta.Persona.Rol.Nombre;
        var token = CrearTokenJwt(cuenta.PersonaId, cuenta.CorreoElectronico, nombreRol);
        return (true, new LoginExitosoDto
        {
            AccessToken = token.token,
            TipoToken = "Bearer",
            ExpiraEnUtc = token.expiraEnUtc,
            EmitidoEnUtc = token.emitidoEnUtc,
            Mensaje = "Inicio de sesión correcto.",
            Rol = nombreRol,
            PersonaId = cuenta.PersonaId,
            RedireccionSugerida = RutasDashboardAutenticacion.ParaRol(nombreRol),
            AlmacenamientoToken = _jwt.UsarCookieHttpOnly ? "cookieHttpOnly" : "bearer"
        }, StatusCodes.Status200OK);
    }

    private (string token, DateTime expiraEnUtc, DateTime emitidoEnUtc) CrearTokenJwt(int personaId, string correo, string nombreRol)
    {
        if (string.IsNullOrWhiteSpace(_jwt.ClaveFirma) || _jwt.ClaveFirma.Length < 32)
            throw new InvalidOperationException("Jwt:ClaveFirma debe tener al menos 32 caracteres.");

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.ClaveFirma));
        var creds = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
        var ahora = DateTime.UtcNow;
        var expira = ahora.AddMinutes(Math.Clamp(_jwt.MinutosExpiracion, 1, 1440));

        var iatUnix = new DateTimeOffset(ahora).ToUnixTimeSeconds();
        var claims = new List<Claim>
        {
            new("id", personaId.ToString()),
            new(JwtRegisteredClaimNames.Sub, personaId.ToString()),
            new(JwtRegisteredClaimNames.Email, correo),
            new(JwtRegisteredClaimNames.Iat, iatUnix.ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, personaId.ToString()),
            new(ClaimTypes.Email, correo),
            new(ClaimTypes.Role, nombreRol)
        };

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Emisor,
            audience: _jwt.Audiencia,
            claims: claims,
            notBefore: ahora,
            expires: expira,
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expira, ahora);
    }

    private static string NormalizarCorreo(string correo) =>
        correo.Trim().ToLowerInvariant();
}
