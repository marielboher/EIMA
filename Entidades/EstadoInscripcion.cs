namespace Entidades;

public class EstadoInscripcion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Inscripciones> Inscripciones { get; set; } = new List<Inscripciones>();
}
