namespace Entidades;

/// <summary>Token de un solo uso para restablecer contraseña (solo se persiste el hash del valor enviado al usuario).</summary>
public class TokenRecuperacionContrasena
{
    public int Id { get; set; }

    public int CuentaUsuarioId { get; set; }
    public CuentaUsuario CuentaUsuario { get; set; } = null!;

    /// <summary>Hash (SHA-256 en Base64) del token transmitido en el enlace.</summary>
    public string HashToken { get; set; } = string.Empty;

    public DateTime CreadoEnUtc { get; set; }
    public DateTime ExpiraEnUtc { get; set; }

    /// <summary>Si tiene valor, el enlace ya no es válido (uso único).</summary>
    public DateTime? ConsumidoEnUtc { get; set; }
}
