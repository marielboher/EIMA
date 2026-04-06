namespace Entidades;

public class Asistencia
{
    public int Id { get; set; }
    public int ClaseId { get; set; }
    public int PersonaId { get; set; }
    public bool Presente { get; set; }
    public DateTime? HoraLlegada { get; set; }
    public string? Observaciones { get; set; }

    public Clase Clase { get; set; } = null!;
    public Persona Persona { get; set; } = null!;
}
