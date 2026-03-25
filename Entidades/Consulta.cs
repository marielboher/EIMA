namespace Entidades;

public class Consulta
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int MateriaId { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }

    public Alumno Alumno { get; set; } = null!;
    public Materia Materia { get; set; } = null!;
}
