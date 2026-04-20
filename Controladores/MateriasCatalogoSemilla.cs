using AccesoDatos;
using Entidades;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

/// <summary>Catálogo de materias por área (contacto / orientación) y siembra idempotente en BD.</summary>
public static class MateriasCatalogoSemilla
{
    /// <summary>Orden fijo: área y nombres tal como deben mostrarse.</summary>
    public static readonly (string Area, string[] Nombres)[] Filas =
    {
        ("Ciencias exactas", new[] { "Matemática", "Álgebra", "Probabilidad y estadística", "Geometría" }),
        ("Área de Ciencias Sociales", new[] { "Historia", "Lengua", "Geografía", "Filosofía y arte" }),
        ("Área de Ciencias Naturales", new[] { "Microbiología", "Biología", "Química", "Física" }),
    };

    public static async Task AsegurarEnBdAsync(EimaDbContext db, CancellationToken ct = default)
    {
        var agregadas = false;
        foreach (var (area, nombres) in Filas)
        {
            foreach (var nombre in nombres)
            {
                var existe = await db.Materias.AnyAsync(m => m.Area == area && m.Nombre == nombre, ct);
                if (existe)
                    continue;
                db.Materias.Add(new Materia
                {
                    Nombre = nombre,
                    Area = area,
                    Descripcion = null,
                    DuracionHoras = 0,
                    PrecioPorClase = 0,
                    Activa = true
                });
                agregadas = true;
            }
        }

        if (agregadas)
            await db.SaveChangesAsync(ct);
    }
}
