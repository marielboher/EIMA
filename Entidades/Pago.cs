namespace Entidades;

public class Pago
{
    public int Id { get; set; }
    public int AlumnoId { get; set; }
    public int InscripcionMateriaId { get; set; }
    public DateTime FechaPago { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Comprobante { get; set; }
    public string? Observaciones { get; set; }

    public Alumno Alumno { get; set; } = null!;
    public InscripcionMateria InscripcionMateria { get; set; } = null!;
}
