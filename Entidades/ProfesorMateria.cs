namespace Entidades;

public class ProfesorMateria
{
    public int Id { get; set; }
    public int ProfesorId { get; set; }
    public int MateriaId { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public bool Activo { get; set; }

    public Profesor Profesor { get; set; } = null!;
    public Materia Materia { get; set; } = null!;
}
