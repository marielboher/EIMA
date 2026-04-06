namespace Entidades;

/// <summary>Una sola tabla de personas; el rol y los campos opcionales definen si actúa como alumno, docente o colaborador.</summary>
public class Persona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }

    public int RolId { get; set; }
    public Rol Rol { get; set; } = null!;

    /// <summary>Solo colaboradores administrativos (ex empleados).</summary>
    public int? TipoColaboradorId { get; set; }
    public TipoColaborador? TipoColaborador { get; set; }

    // --- Opcionales típicos de alumno ---
    public string? Colegio { get; set; }
    public string? GradoCurso { get; set; }
    public string? NivelEducativo { get; set; }

    // --- Opcionales típicos de docente ---
    public string? Especialidades { get; set; }
    public string? Titulo { get; set; }
    public DateTime? FechaIngresoDocente { get; set; }

    // --- Opcionales típicos de colaborador administrativo ---
    public DateTime? FechaContratacion { get; set; }
    public DateTime? FechaFinContratacion { get; set; }
    public decimal? Salario { get; set; }
    public bool? ActivoComoColaborador { get; set; }

    public CuentaUsuario? CuentaUsuario { get; set; }

    public ICollection<InscripcionMateria> Inscripciones { get; set; } = new List<InscripcionMateria>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public ICollection<ProfesorMateria> ProfesoresMaterias { get; set; } = new List<ProfesorMateria>();
    public ICollection<HorarioDisponible> HorariosDisponibles { get; set; } = new List<HorarioDisponible>();
    public ICollection<Clase> ClasesComoDocente { get; set; } = new List<Clase>();
}
