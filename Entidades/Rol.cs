namespace Entidades;

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Persona> Personas { get; set; } = new List<Persona>();
}
