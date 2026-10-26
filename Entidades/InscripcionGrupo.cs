namespace Entidades;

/// <summary>
/// Vínculo de un alumno con un grupo de aula (HU29).
/// La baja es lógica: <see cref="FechaBaja"/> libera el cupo y conserva el histórico.
/// </summary>
public class InscripcionGrupo
{
    public int Id { get; set; }

    public int AsignacionAulaId { get; set; }
    public AsignacionAula AsignacionAula { get; set; } = null!;

    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    /// <summary>Momento en que se reserva el cupo. No exige pago confirmado.</summary>
    public DateTime FechaAsignacion { get; set; }

    /// <summary>Nula mientras el vínculo está activo.</summary>
    public DateTime? FechaBaja { get; set; }
}
