using System.ComponentModel.DataAnnotations;

namespace Controladores;

/// <summary>Alta de una asignación de aula + materia + docente (HU23).</summary>
public class GuardarAsignacionAulaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El aula es obligatoria.")]
    public int AulaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La materia es obligatoria.")]
    public int MateriaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El docente es obligatorio.")]
    public int DocenteId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "El cupo habilitado debe ser un entero mayor que 0.")]
    public int CupoHabilitado { get; set; }

    [Required(ErrorMessage = "El fin de vigencia es obligatorio.")]
    public DateTime? FinVigencia { get; set; }
}

public class AsignacionAulaListadoDto
{
    public int Id { get; set; }
    public int AulaId { get; set; }
    public string AulaNombre { get; set; } = string.Empty;
    public int? CapacidadAula { get; set; }
    public int MateriaId { get; set; }
    public string MateriaNombre { get; set; } = string.Empty;
    public int DocenteId { get; set; }
    public string DocenteNombre { get; set; } = string.Empty;
    public string? DocenteDni { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CupoHabilitado { get; set; }
    public int CuposOcupados { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public DateTime FinVigencia { get; set; }
    public DateTime? FechaCierre { get; set; }
}

/// <summary>Opciones para el formulario de asignación (solo aulas y materias activas).</summary>
public class OpcionesAsignacionAulaDto
{
    public IReadOnlyList<AulaOpcionDto> Aulas { get; set; } = Array.Empty<AulaOpcionDto>();
    public IReadOnlyList<MateriaOpcionDto> Materias { get; set; } = Array.Empty<MateriaOpcionDto>();
    public IReadOnlyList<DocenteOpcionDto> Docentes { get; set; } = Array.Empty<DocenteOpcionDto>();
}

public class AulaOpcionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Capacidad { get; set; }
}

public class MateriaOpcionDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class DocenteOpcionDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Dni { get; set; }
}
