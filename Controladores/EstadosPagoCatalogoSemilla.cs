using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

/// <summary>Catálogo de estados de pago y siembra idempotente.</summary>
public static class EstadosPagoCatalogoSemilla
{
    public static readonly (string Nombre, string Descripcion)[] Filas =
    {
        (EstadosPagoSistema.Pendiente, "Pago registrado pendiente de confirmación"),
        (EstadosPagoSistema.Confirmado, "Pago confirmado; suma al monto pagado de la inscripción"),
    };

    public static async Task AsegurarEnBdAsync(EimaDbContext db, CancellationToken ct = default)
    {
        var agregados = false;
        foreach (var (nombre, descripcion) in Filas)
        {
            var existe = await db.EstadosPago.AnyAsync(e => e.Nombre == nombre, ct);
            if (existe)
                continue;

            db.EstadosPago.Add(new EstadoPago
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
