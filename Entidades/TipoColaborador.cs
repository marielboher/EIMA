namespace Entidades;

public class TipoColaborador
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}
