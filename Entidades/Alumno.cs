namespace Entidades;

public class Alumno : Persona
{
    public override int Id { get; set; }

    public string? Colegio { get; set; }
    public string? GradoCurso { get; set; }
    public string? NivelEducativo { get; set; }

    public ICollection<InscripcionMateria> Inscripciones { get; set; } = new List<InscripcionMateria>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
}
