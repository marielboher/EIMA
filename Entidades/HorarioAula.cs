namespace Entidades;

public class HorarioAula
{
    public int Id { get; set; }
    public int AulaId { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Ocupada { get; set; }

    public Aula Aula { get; set; } = null!;
}
