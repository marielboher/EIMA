using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

/// <summary>Roles del sistema y siembra idempotente en BD.</summary>
public static class RolesCatalogoSemilla
{
    public static readonly (string Nombre, string Descripcion)[] Filas =
    {
        (RolesSistema.SuperAdmin, "Super administrador del sistema"),
        (RolesSistema.Administrativo, "Administración / colaboradores"),
        (RolesSistema.Alumno, "Usuario alumno del sistema"),
        (RolesSistema.Profesor, "Docente"),
    };

    public static async Task AsegurarEnBdAsync(EimaDbContext db, CancellationToken ct = default)
    {
        var agregados = false;
        foreach (var (nombre, descripcion) in Filas)
        {
            var existe = await db.Roles.AnyAsync(r => r.Nombre == nombre, ct);
            if (existe)
                continue;

            db.Roles.Add(new Rol
            {
                Nombre = nombre,
                Descripcion = descripcion
            });
            agregados = true;
        }

        if (agregados)
            await db.SaveChangesAsync(ct);
    }
}
