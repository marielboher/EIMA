namespace Entidades;

public class EstadoPago
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
