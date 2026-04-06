namespace Controladores.Opciones;

public class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public string ClaveFirma { get; set; } = string.Empty;
    public int MinutosExpiracion { get; set; } = 120;
}
