namespace Controladores.Opciones;

public class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public string ClaveFirma { get; set; } = string.Empty;
    /// <summary>Duración del token; criterio de negocio: 24 h (1440 minutos).</summary>
    public int MinutosExpiracion { get; set; } = 1440;
    /// <summary>Si es true, el token no se expone en el cuerpo JSON y se envía solo en cookie HttpOnly (más seguro frente a XSS).</summary>
    public bool UsarCookieHttpOnly { get; set; }
    public string NombreCookieAccessToken { get; set; } = "eima_access_token";
}
