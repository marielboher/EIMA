using System.ComponentModel.DataAnnotations;

namespace Controladores;

public class GuardarInscripcionDto
{
    [Required]
    public int PersonaId { get; set; }

    [Required]
    public int MateriaId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Las clases contratadas deben ser mayores a cero.")]
    public int ClasesTotales { get; set; }
}

public class EditarInscripcionDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Las clases contratadas deben ser mayores a cero.")]
    public int ClasesTotales { get; set; }

    /// <summary>Opcional: si se omite, se conserva el valor actual.</summary>
    [Range(0, int.MaxValue)]
    public int? ClasesTomadas { get; set; }

    /// <summary>Nombre del estado (Activa, Finalizada, Suspendida, Cancelada).</summary>
    [Required]
    public string Estado { get; set; } = string.Empty;
}

public class GuardarPagoDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser positivo.")]
    public decimal Monto { get; set; }

    [Required]
    [MaxLength(50)]
    public string MetodoPago { get; set; } = string.Empty;

    [Required]
    public DateTime FechaPago { get; set; }

    /// <summary>Nombre del estado. Por defecto Pendiente si se omite.</summary>
    [MaxLength(50)]
    public string? Estado { get; set; }

    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}

public class InscripcionListadoDto
{
    public int Id { get; set; }
    public int PersonaId { get; set; }
    public int MateriaId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public string AlumnoDni { get; set; } = string.Empty;
    public string Materia { get; set; } = string.Empty;
    public DateTime FechaInscripcion { get; set; }
    public int ClasesTomadas { get; set; }
    public int ClasesTotales { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int EstadoId { get; set; }
    public decimal MontoPagado { get; set; }
}

public class PagoDetalleDto
{
    public int Id { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Comprobante { get; set; }
    public string? Observaciones { get; set; }
}

public class InscripcionDetalleDto : InscripcionListadoDto
{
    public List<PagoDetalleDto> Pagos { get; set; } = new();
    public decimal MontoConfirmadoAdvertencia { get; set; }
}
