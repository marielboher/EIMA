namespace Entidades;

public class Aula
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activa { get; set; }

    public ICollection<HorarioAula> HorariosAula { get; set; } = new List<HorarioAula>();
    public ICollection<Clase> Clases { get; set; } = new List<Clase>();
}
