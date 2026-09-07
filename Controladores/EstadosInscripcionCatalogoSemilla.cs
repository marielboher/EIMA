using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

/// <summary>Catálogo de estados de inscripción y siembra idempotente.</summary>
public static class EstadosInscripcionCatalogoSemilla
{
    public static readonly (string Nombre, string Descripcion)[] Filas =
    {
        (EstadosInscripcionSistema.Activa, "Cursada en curso"),
        (EstadosInscripcionSistema.Finalizada, "Cursada completada"),
        (EstadosInscripcionSistema.Suspendida, "Cursada pausada temporalmente"),
        (EstadosInscripcionSistema.Cancelada, "Baja lógica de la inscripción"),
    };

    public static async Task AsegurarEnBdAsync(EimaDbContext db, CancellationToken ct = default)
    {
        var agregados = false;
        foreach (var (nombre, descripcion) in Filas)
        {
            var existe = await db.EstadosInscripcion.AnyAsync(e => e.Nombre == nombre, ct);
            if (existe)
                continue;

            db.EstadosInscripcion.Add(new EstadoInscripcion
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
