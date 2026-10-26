using AccesoDatos;

namespace Controladores;

/// <summary>
/// Serializa el alta y la baja de un grupo.
/// En PostgreSQL además toma la fila con <c>FOR UPDATE</c> para que dos procesos
/// no reserven el último cupo a la vez (HU29, CA09).
/// </summary>
public interface IBloqueoFilaGrupo
{
    Task<IAsyncDisposable> BloquearAsync(EimaDbContext context, int asignacionAulaId, CancellationToken ct);
}
