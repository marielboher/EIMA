using Entidades;

namespace Controladores;

/// <summary>Indicadores del mes sobre asignaciones de aula (HU28).</summary>
public class InformeAsignacionesAulaDto
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string Periodo { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";
    public DateTime FechaActualizacion { get; set; }

    public int Creadas { get; set; }
    public int Cerradas { get; set; }
    public int AbiertasAlCierre { get; set; }

    public int CerradasEnVigencia { get; set; }
    public double? SlaPorcentaje { get; set; }
    public double? TmeDias { get; set; }

    /// <summary>mes | ultimos3meses. El mes ancla es <see cref="Mes"/> de <see cref="Anio"/>.</summary>
    public string PeriodoTipo { get; set; } = "mes";
    public string EtiquetaPeriodo { get; set; } = string.Empty;
    public string? EtiquetaPeriodoAnterior { get; set; }
    public bool Comparar { get; set; }
    public int? MateriaId { get; set; }
    public string? MateriaNombre { get; set; }

    public double? OcupacionMedia { get; set; }
    public double? OcupacionP50 { get; set; }
    public double? OcupacionP90 { get; set; }

    public VariacionesInformeDto? Variaciones { get; set; }
    public IReadOnlyList<SerieMateriaInformeDto> SeriesPorMateria { get; set; } = Array.Empty<SerieMateriaInformeDto>();
    public IReadOnlyList<FilaInformeDto> Tabla { get; set; } = Array.Empty<FilaInformeDto>();
    public FilaInformeDto Total { get; set; } = new() { Nombre = "Total" };
    public IReadOnlyList<MateriaOpcionDto> MateriasDisponibles { get; set; } = Array.Empty<MateriaOpcionDto>();

    public DefinicionesInformeDto Definiciones { get; set; } = new();
}

/// <summary>Definición funcional de cada indicador (HU28, CA01).</summary>
public class DefinicionesInformeDto
{
    public string Creadas { get; set; } = string.Empty;
    public string Cerradas { get; set; } = string.Empty;
    public string AbiertasAlCierre { get; set; } = string.Empty;
    public string Sla { get; set; } = string.Empty;
    public string Tme { get; set; } = string.Empty;
    public string Ocupacion { get; set; } = string.Empty;
}

public class VariacionIndicadorDto
{
    public double? Absoluta { get; set; }
    public double? Porcentaje { get; set; }
}

public class VariacionesInformeDto
{
    public VariacionIndicadorDto Creadas { get; set; } = new();
    public VariacionIndicadorDto Cerradas { get; set; } = new();
    public VariacionIndicadorDto AbiertasAlCierre { get; set; } = new();
    public VariacionIndicadorDto Sla { get; set; } = new();
    public VariacionIndicadorDto Tme { get; set; } = new();
}

public class SerieMateriaInformeDto
{
    public int MateriaId { get; set; }
    public string MateriaNombre { get; set; } = string.Empty;
    public int Creadas { get; set; }
    public int? CreadasAnterior { get; set; }
}

public class FilaInformeDto
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Creadas { get; set; }
    public int Cerradas { get; set; }
    public double? OcupacionMedia { get; set; }
    public double? SlaPorcentaje { get; set; }
    public IReadOnlyList<FilaInformeDto> Docentes { get; set; } = Array.Empty<FilaInformeDto>();
}

public class PreferenciaInformeDto
{
    public string PeriodoTipo { get; set; } = PeriodosInformeAsignacion.Mes;
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int? MateriaId { get; set; }
    public string? MateriaNombre { get; set; }
    public bool Comparar { get; set; }
    public bool Guardada { get; set; }
}

public class GuardarPreferenciaInformeDto
{
    public string? PeriodoTipo { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int? MateriaId { get; set; }
    public bool Comparar { get; set; }
}
