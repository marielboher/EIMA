namespace Controladores.Opciones;

/// <summary>Configuración del flujo de recuperación (enlace al front y exposición del enlace en la respuesta API).</summary>
public sealed class RecuperacionContrasenaOpciones
{
    public const string Seccion = "RecuperacionContrasena";

    /// <summary>Vigencia del token de recuperación (CA02: 30 minutos).</summary>
    public int MinutosValidezToken { get; set; } = 30;

    /// <summary>URL absoluta del cliente con marcador <c>{0}</c> para el token.</summary>
    public string PlantillaEnlace { get; set; } = "https://localhost:5173/restablecer-contrasena?token={0}";

    /// <summary>Si es true, la API incluye <c>enlaceRecuperacion</c> en JSON (útil en desarrollo).</summary>
    public bool IncluirEnlaceEnRespuesta { get; set; }
}
