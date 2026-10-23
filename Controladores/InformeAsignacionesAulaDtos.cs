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
}
