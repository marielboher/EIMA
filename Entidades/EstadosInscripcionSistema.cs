namespace Entidades;

/// <summary>Nombres de estado de inscripción almacenados en <see cref="EstadoInscripcion.Nombre"/>.</summary>
public static class EstadosInscripcionSistema
{
    public const string Activa = "Activa";
    public const string Finalizada = "Finalizada";
    public const string Suspendida = "Suspendida";
    public const string Cancelada = "Cancelada";

    /// <summary>Estados que bloquean una nueva inscripción del mismo alumno–materia (HU17 CA04 opción B).</summary>
    public static readonly string[] BloqueanDuplicado = { Activa, Suspendida };

    public static readonly string[] YaInactivos = { Finalizada, Cancelada };
}
