namespace Entidades;

public class Materia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Descripcion { get; set; }
    public int DuracionHoras { get; set; }
    public decimal PrecioPorClase { get; set; }
    public bool Activa { get; set; }

    public ICollection<ProfesorMateria> ProfesoresMaterias { get; set; } = new List<ProfesorMateria>();
    public ICollection<Inscripciones> Inscripciones { get; set; } = new List<Inscripciones>();
    public ICollection<Clase> Clases { get; set; } = new List<Clase>();
}
