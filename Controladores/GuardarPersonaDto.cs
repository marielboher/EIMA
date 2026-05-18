using System;

namespace Controladores;

/// <summary>DTO para la creación y edición de personas en el panel administrativo.</summary>
public class GuardarPersonaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    
    /// <summary>Rol proveniente del frontend: "alumno", "docente" o "colaborador".</summary>
    public string Rol { get; set; } = string.Empty;

    // --- Campos específicos de Alumno ---
    public string? Colegio { get; set; }
    public string? GradoCurso { get; set; }
    public string? NivelEducativo { get; set; }

    // --- Campos específicos de Docente (Profesor) ---
    public string? Especialidades { get; set; }
    public string? Titulo { get; set; }
    public DateTime? FechaIngresoDocente { get; set; }

    // --- Campos específicos de Colaborador (Administrativo) ---
    public string? TipoColaborador { get; set; }
    public DateTime? FechaContratacion { get; set; }
    public DateTime? FechaFinContratacion { get; set; }
    public decimal? Salario { get; set; }
    public bool? ActivoComoColaborador { get; set; }
}
