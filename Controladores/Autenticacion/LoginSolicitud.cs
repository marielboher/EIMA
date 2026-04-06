namespace Controladores.Autenticacion;

public sealed class LoginSolicitud
{
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
