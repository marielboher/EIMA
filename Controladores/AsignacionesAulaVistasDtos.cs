namespace Controladores;

/// <summary>Filtros del listado de asignaciones que se guardan dentro de una vista (HU25).</summary>
public class FiltrosListadoAsignacionesDto
{
    public string? Estado { get; set; }
    public string? Buscar { get; set; }
    public int? MateriaId { get; set; }
    public int? DocenteId { get; set; }
    public DateTime? VigenciaDesde { get; set; }
    public DateTime? VigenciaHasta { get; set; }
}

/// <summary>Orden del listado que se guarda dentro de una vista (HU25).</summary>
public class OrdenListadoAsignacionesDto
{
    public string Campo { get; set; } = "fechaAsignacion";
    public string Direccion { get; set; } = "desc";
}

public class GuardarVistaListadoDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "El nombre de la vista es obligatorio.")]
    [System.ComponentModel.DataAnnotations.MaxLength(80, ErrorMessage = "El nombre de la vista no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public FiltrosListadoAsignacionesDto Filtros { get; set; } = new();

    public OrdenListadoAsignacionesDto Orden { get; set; } = new();
}

public class VistaListadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public FiltrosListadoAsignacionesDto Filtros { get; set; } = new();
    public OrdenListadoAsignacionesDto Orden { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
