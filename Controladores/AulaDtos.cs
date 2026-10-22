using System.ComponentModel.DataAnnotations;

namespace Controladores;

/// <summary>Alta y edición de aulas (HU22).</summary>
public class GuardarAulaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser un número entero mayor que 0.")]
    public int Capacidad { get; set; }

    [MaxLength(200)]
    public string? Ubicacion { get; set; }
}

public class AulaListadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activa { get; set; }

    /// <summary>Cantidad de asignaciones en estado Abierta (bloquea la inactivación).</summary>
    public int AsignacionesAbiertas { get; set; }
}
