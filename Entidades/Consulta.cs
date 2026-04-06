namespace Entidades;

public class Consulta
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    /// <summary>Texto libre con la materia o tema de la consulta (sin FK a la tabla Materias).</summary>
    public string Materia { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }

    public Persona Persona { get; set; } = null!;
}
