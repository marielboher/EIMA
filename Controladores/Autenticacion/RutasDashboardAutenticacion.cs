namespace Controladores.Autenticacion;

internal static class RutasDashboardAutenticacion
{
    /// <summary>Ruta inicial del SPA tras login; el cliente muestra secciones según el rol.</summary>
    public static string ParaRol(string nombreRol) => "/dashboard";
}
