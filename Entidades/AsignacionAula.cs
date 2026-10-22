namespace Entidades;

/// <summary>
/// Asignación de un aula a una materia con un docente, un cupo habilitado y una vigencia (HU23).
/// El cierre es definitivo: no se reabre el mismo registro.
/// </summary>
public class AsignacionAula
{
    public int Id { get; set; }

    public int AulaId { get; set; }
    public Aula Aula { get; set; } = null!;

    public int MateriaId { get; set; }
    public Materia Materia { get; set; } = null!;

    public int DocenteId { get; set; }
    public Persona Docente { get; set; } = null!;

    /// <summary>Estado Abierta | Cerrada.</summary>
    public string Estado { get; set; } = EstadosAsignacionAula.Abierta;

    /// <summary>Cupo máximo habilitado (entero &gt; 0 y ≤ capacidad del aula).</summary>
    public int CupoHabilitado { get; set; }

    /// <summary>Ocupación actual; inicia en 0 al crear la asignación.</summary>
    public int CuposOcupados { get; set; }

    /// <summary>Se registra al crear la asignación.</summary>
    public DateTime FechaAsignacion { get; set; }

    /// <summary>Fecha y hora límite del grupo.</summary>
    public DateTime FinVigencia { get; set; }

    /// <summary>Se registra una sola vez al cerrar la asignación.</summary>
    public DateTime? FechaCierre { get; set; }
}
