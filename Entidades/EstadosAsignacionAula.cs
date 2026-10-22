namespace Entidades;

/// <summary>Valores válidos para <see cref="AsignacionAula.Estado"/>.</summary>
public static class EstadosAsignacionAula
{
    public const string Abierta = "Abierta";
    public const string Cerrada = "Cerrada";

    public static readonly IReadOnlyList<string> Todos = new[] { Abierta, Cerrada };
}
