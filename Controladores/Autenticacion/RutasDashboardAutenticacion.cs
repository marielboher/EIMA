using Entidades;

namespace Controladores.Autenticacion;

internal static class RutasDashboardAutenticacion
{
    public static string ParaRol(string nombreRol) => nombreRol switch
    {
        RolesSistema.Alumno => "/dashboard/alumno",
        RolesSistema.Profesor => "/dashboard/profesor",
        RolesSistema.Secretaria => "/dashboard/secretaria",
        RolesSistema.SuperAdmin => "/dashboard/admin",
        _ => "/dashboard"
    };
}
