namespace Entidades;

/// <summary>
/// Vista guardada del listado de asignaciones de aula (HU25).
/// Combina los filtros y el orden elegidos y pertenece a un único usuario.
/// </summary>
public class VistaListadoAsignacion
{
    public int Id { get; set; }

    /// <summary>Persona (usuario autenticado) dueña de la vista.</summary>
    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    /// <summary>Nombre visible, por ejemplo "Mi vista diaria".</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Filtros aplicados serializados como JSON.</summary>
    public string FiltrosJson { get; set; } = "{}";

    /// <summary>Orden aplicado serializado como JSON (campo y dirección).</summary>
    public string OrdenJson { get; set; } = "{}";

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
