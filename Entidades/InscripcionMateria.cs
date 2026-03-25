namespace Entidades;

public class InscripcionMateria
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int MateriaId { get; set; }
    public DateTime FechaInscripcion { get; set; }
    public int ClasesTotales { get; set; }
    public int ClasesTomadas { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal MontoPagado { get; set; }

    public Alumno Alumno { get; set; } = null!;
    public Materia Materia { get; set; } = null!;
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
