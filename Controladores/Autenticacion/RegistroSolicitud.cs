namespace Controladores.Autenticacion;

public sealed class RegistroSolicitud
{
    /// <summary>Tipo de cuenta pública: <c>alumno</c> (predeterminado) o <c>profesor</c>. Otros roles se asignan por administración.</summary>
    public string? TipoRegistro { get; set; }

    public string Dni { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string ConfirmarContrasena { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
}
