namespace Entidades;

/// <summary>Consulta genérica sin vínculo a una persona (contacto / soporte / mesa de ayuda).</summary>
public class Consulta
{
    public int Id { get; set; }
    /// <summary>Texto libre con la materia o tema de la consulta.</summary>
    public string Materia { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
}
