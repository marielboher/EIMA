namespace Entidades;

public class Clase
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public int ProfesorId { get; set; }
    public int AulaId { get; set; }
    public DateTime Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public int CapacidadMaxima { get; set; }

    public Materia Materia { get; set; } = null!;
    public Profesor Profesor { get; set; } = null!;
    public Aula Aula { get; set; } = null!;
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}
