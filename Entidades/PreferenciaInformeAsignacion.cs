namespace Entidades;

/// <summary>
/// Último periodo, materia y comparativo elegidos por un administrador en el informe (HU33).
/// </summary>
public class PreferenciaInformeAsignacion
{
    public int Id { get; set; }

    public int PersonaId { get; set; }
    public Persona Persona { get; set; } = null!;

    /// <summary>mes | ultimos3meses.</summary>
    public string PeriodoTipo { get; set; } = PeriodosInformeAsignacion.Mes;

    /// <summary>Año del mes ancla (el mes final cuando el periodo abarca tres meses).</summary>
    public int Anio { get; set; }

    /// <summary>Mes ancla, de 1 a 12.</summary>
    public int Mes { get; set; }

    public int? MateriaId { get; set; }

    public bool Comparar { get; set; }

    public DateTime FechaActualizacion { get; set; }
}

public static class PeriodosInformeAsignacion
{
    public const string Mes = "mes";
    public const string Ultimos3Meses = "ultimos3meses";

    public static bool IntentarNormalizar(string? valor, out string normalizado)
    {
        if (string.IsNullOrWhiteSpace(valor) ||
            valor.Trim().Equals(Mes, StringComparison.OrdinalIgnoreCase))
        {
            normalizado = Mes;
            return true;
        }

        if (valor.Trim().Equals(Ultimos3Meses, StringComparison.OrdinalIgnoreCase))
        {
            normalizado = Ultimos3Meses;
            return true;
        }

        normalizado = string.Empty;
        return false;
    }
}
