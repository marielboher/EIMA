namespace Entidades;

public class HorarioDisponible
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    /// <summary>Día de la semana (texto, p. ej. Lunes) o código según convención del cliente.</summary>
    public string DiaSemana { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; }

    public Persona Docente { get; set; } = null!;
}
