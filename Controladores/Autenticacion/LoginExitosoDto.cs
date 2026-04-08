namespace Controladores.Autenticacion;

/// <summary>Respuesta exitosa de login; <see cref="AccessToken"/> puede omitirse si el token solo se envía en cookie HttpOnly.</summary>
public sealed class LoginExitosoDto
{
    public string? AccessToken { get; set; }
    public string TipoToken { get; set; } = "Bearer";
    public DateTime ExpiraEnUtc { get; set; }
    public DateTime EmitidoEnUtc { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int PersonaId { get; set; }
    /// <summary>Ruta sugerida para el cliente (SPA) según el rol tras credenciales válidas.</summary>
    public string RedireccionSugerida { get; set; } = string.Empty;
    /// <summary>Indica al cliente cómo debe persistir el token (<c>bearer</c> en memoria/header o <c>cookieHttpOnly</c>).</summary>
    public string AlmacenamientoToken { get; set; } = "bearer";
}
