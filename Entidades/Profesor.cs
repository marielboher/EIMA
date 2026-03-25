namespace Entidades;

public class Profesor : Persona
{
    public override int Id { get; set; }

    public string? Especialidades { get; set; }
    public string? Titulo { get; set; }
    public DateTime FechaIngreso { get; set; }

    public ICollection<ProfesorMateria> ProfesoresMaterias { get; set; } = new List<ProfesorMateria>();
    public ICollection<HorarioDisponible> HorariosDisponibles { get; set; } = new List<HorarioDisponible>();
    public ICollection<Clase> Clases { get; set; } = new List<Clase>();
}
