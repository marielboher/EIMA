namespace Entidades;

public abstract class Persona
{
    public virtual int Id { get; set; }

    /// <summary>Datos de empleado si esta persona es colaborador administrativo (TPT).</summary>
    public Empleado? Empleado { get; set; }

    /// <summary>Datos de alumno si esta persona está inscripta como tal (TPT).</summary>
    public Alumno? Alumno { get; set; }

    /// <summary>Datos de docente si esta persona da clases (TPT).</summary>
    public Profesor? Profesor { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public DateTime FechaRegistro { get; set; }
}
