namespace Entidades;

public class ProfesorMateria
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public int MateriaId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public bool Activo { get; set; }

    public Persona Docente { get; set; } = null!;
    public Materia Materia { get; set; } = null!;
}
