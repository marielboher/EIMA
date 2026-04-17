using System.Text.Json.Serialization;

namespace Entidades;

/// <summary>Credenciales de acceso (email + contraseña hasheada), una por persona.</summary>
public class CuentaUsuario
{
    public int Id { get; set; }

    /// <summary>Correo normalizado (minúsculas, sin espacios) usado para el inicio de sesión.</summary>
    public string CorreoElectronico { get; set; } = string.Empty;

    [JsonIgnore]
    public string HashContrasena { get; set; } = string.Empty;

    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;
}
