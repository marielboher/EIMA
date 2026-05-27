using System;
using System.Collections.Generic;

namespace Controladores;

/// <summary>DTO para la creación y edición de personas en el panel administrativo.</summary>
public class GuardarPersonaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;

    /// <summary>Rol proveniente del frontend: "alumno", "profesor" o "administrativo".</summary>
    public string Rol { get; set; } = string.Empty;

    // --- Campos específicos de Alumno ---
    public string? Colegio { get; set; }
    public string? GradoCurso { get; set; }
    public string? NivelEducativo { get; set; }

    // --- Campos específicos de Docente (Profesor) ---
    public string? Titulo { get; set; }
    public DateTime? FechaIngresoDocente { get; set; }

    /// <summary>Materias asignadas al docente con sus valores específicos (valor/hora, cantidad de alumnos y horas).</summary>
    public List<MateriaAsignacionDto> Materias { get; set; } = new();

    // --- Campos específicos de Colaborador (Administrativo) ---
    public string? TipoColaborador { get; set; }
    public DateTime? FechaContratacion { get; set; }
    public DateTime? FechaFinContratacion { get; set; }
    public decimal? Salario { get; set; }
    public bool? ActivoComoColaborador { get; set; }
}

/// <summary>Materia con valores específicos del docente: valor por hora, cantidad de alumnos y horas semanales.</summary>
public class MateriaAsignacionDto
{
    public int MateriaId { get; set; }
    public decimal? ValorHora { get; set; }
    public int? CantAlumnos { get; set; }
    public double? CantHoras { get; set; }
}
