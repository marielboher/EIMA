namespace Entidades;

public class Empleado : Persona
{
    public override int Id { get; set; }

    public int TipoColaboradorId { get; set; }
    public int RolId { get; set; }
    public DateTime FechaContratacion { get; set; }
    public DateTime? FechaFinContratacion { get; set; }
    public decimal Salario { get; set; }
    public bool Activo { get; set; }

    public TipoColaborador TipoColaborador { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}
