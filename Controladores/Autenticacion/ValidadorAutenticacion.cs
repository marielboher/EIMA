using System.Text.RegularExpressions;
using Entidades;

namespace Controladores.Autenticacion;

public static class ValidadorAutenticacion
{
    private static readonly Regex CorreoRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Niveles: muyDebil, debil, media, fuerte (útil para UI en tiempo real vía API).</summary>
    public static string CalcularFortalezaContrasena(string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(contrasena))
            return Fortaleza.MuyDebil;

        var cumple = CumplePoliticaContrasena(contrasena, out var mensajes);
        var len = contrasena.Length;
        var tieneMayus = Regex.IsMatch(contrasena, "[A-ZÁÉÍÓÚÑ]");
        var tieneMinus = Regex.IsMatch(contrasena, "[a-záéíóúñ]");
        var tieneDig = Regex.IsMatch(contrasena, "[0-9]");
        var tieneEsp = Regex.IsMatch(contrasena, @"[\W_]");
        var variedad = (tieneMayus ? 1 : 0) + (tieneMinus ? 1 : 0) + (tieneDig ? 1 : 0) + (tieneEsp ? 1 : 0);

        if (len < 6 || variedad <= 1)
            return Fortaleza.MuyDebil;

        if (!cumple)
            return Fortaleza.Debil;

        if (len >= 12 && variedad >= 4 && mensajes.Count == 0)
            return Fortaleza.Fuerte;

        return Fortaleza.Media;
    }

    public static ResultadoValidacion ValidarRegistro(RegistroSolicitud s)
    {
        var r = new ResultadoValidacion();

        if (string.IsNullOrWhiteSpace(s.Dni))
            r.Agregar(nameof(s.Dni), "El DNI o documento es obligatorio. Ingrese su número de documento.");
        else
        {
            var dniNorm = NormalizarDniParaAlmacenamiento(s.Dni);
            if (dniNorm.Length < 6 || dniNorm.Length > 15)
                r.Agregar(nameof(s.Dni),
                    "El DNI debe tener entre 6 y 15 dígitos. Puede usar puntos o guiones; en la base de datos se guardan solo los números, sin cifrar.");
        }

        if (string.IsNullOrWhiteSpace(s.Nombre))
            r.Agregar(nameof(s.Nombre), "El nombre es obligatorio. Ingrese su nombre.");
        if (string.IsNullOrWhiteSpace(s.Apellido))
            r.Agregar(nameof(s.Apellido), "El apellido es obligatorio. Ingrese su apellido.");
        if (string.IsNullOrWhiteSpace(s.Correo))
            r.Agregar(nameof(s.Correo), "El correo electrónico es obligatorio. Ingrese un correo válido (usuario@dominio.com).");
        else if (!CorreoRegex.IsMatch(s.Correo.Trim()))
            r.Agregar(nameof(s.Correo), "El correo no tiene un formato válido. Use el formato usuario@dominio.com (sin espacios).");

        if (string.IsNullOrWhiteSpace(s.Telefono))
            r.Agregar(nameof(s.Telefono), "El teléfono es obligatorio. Ingrese un número de contacto.");
        if (string.IsNullOrWhiteSpace(s.Direccion))
            r.Agregar(nameof(s.Direccion), "La dirección es obligatoria. Ingrese su domicilio.");

        if (string.IsNullOrWhiteSpace(s.Contrasena))
            r.Agregar(nameof(s.Contrasena), "La contraseña es obligatoria.");
        else
        {
            if (!CumplePoliticaContrasena(s.Contrasena, out var reqs))
            {
                foreach (var m in reqs)
                    r.Agregar(nameof(s.Contrasena), m);
            }
        }

        if (string.IsNullOrWhiteSpace(s.ConfirmarContrasena))
            r.Agregar(nameof(s.ConfirmarContrasena), "Debe confirmar la contraseña. Complete el campo de confirmación.");
        else if (!string.Equals(s.Contrasena, s.ConfirmarContrasena, StringComparison.Ordinal))
            r.Agregar(nameof(s.ConfirmarContrasena), "Las contraseñas no coinciden. Asegúrese de escribir la misma contraseña en ambos campos.");

        var tipoNorm = NormalizarTipoRegistroPublico(s.TipoRegistro);
        if (tipoNorm == RolesSistema.SuperAdmin || tipoNorm == RolesSistema.Secretaria)
            r.Agregar(nameof(s.TipoRegistro),
                "Las cuentas de super administrador o secretaría no se crean desde el registro público. Solicite acceso al administrador del sistema.");
        else if (tipoNorm != RolesSistema.Alumno && tipoNorm != RolesSistema.Profesor)
            r.Agregar(nameof(s.TipoRegistro),
                "Tipo de registro no válido. Omita el campo para usar \"alumno\" o envíe \"alumno\" o \"profesor\".");

        return r;
    }

    /// <summary>Solo dígitos, sin separadores; se usa tal cual en base de datos (texto claro, sin hash).</summary>
    public static string NormalizarDniParaAlmacenamiento(string dni) =>
        Regex.Replace(dni.Trim(), @"[^\d]", "");

    /// <summary>Resuelve el tipo de registro autogestionado: vacío → alumno.</summary>
    public static string NormalizarTipoRegistroPublico(string? tipoRegistro)
    {
        if (string.IsNullOrWhiteSpace(tipoRegistro))
            return RolesSistema.Alumno;
        return tipoRegistro.Trim().ToLowerInvariant();
    }

    public static ResultadoValidacion ValidarLogin(LoginSolicitud s)
    {
        var r = new ResultadoValidacion();
        if (string.IsNullOrWhiteSpace(s.Correo))
            r.Agregar(nameof(s.Correo), "El correo electrónico es obligatorio.");
        else if (!CorreoRegex.IsMatch(s.Correo.Trim()))
            r.Agregar(nameof(s.Correo), "El correo no tiene un formato válido. Use usuario@dominio.com.");

        if (string.IsNullOrWhiteSpace(s.Contrasena))
            r.Agregar(nameof(s.Contrasena), "La contraseña es obligatoria.");

        return r;
    }

    private static bool CumplePoliticaContrasena(string contrasena, out List<string> mensajes)
    {
        mensajes = new List<string>();
        if (contrasena.Length < 8)
            mensajes.Add("La contraseña debe tener al menos 8 caracteres.");
        if (!Regex.IsMatch(contrasena, "[A-ZÁÉÍÓÚÑ]"))
            mensajes.Add("La contraseña debe incluir al menos una letra mayúscula.");
        if (!Regex.IsMatch(contrasena, "[0-9]"))
            mensajes.Add("La contraseña debe incluir al menos un número.");
        if (!Regex.IsMatch(contrasena, @"[\W_]"))
            mensajes.Add("La contraseña debe incluir al menos un carácter especial (por ejemplo: !@#$%&*).");

        return mensajes.Count == 0;
    }

    public static class Fortaleza
    {
        public const string MuyDebil = "muyDebil";
        public const string Debil = "debil";
        public const string Media = "media";
        public const string Fuerte = "fuerte";
    }
}
