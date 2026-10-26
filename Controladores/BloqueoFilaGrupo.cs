using System.Collections.Concurrent;
using AccesoDatos;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

public sealed class BloqueoFilaGrupo : IBloqueoFilaGrupo
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Semaforos = new();

    public async Task<IAsyncDisposable> BloquearAsync(
        EimaDbContext context,
        int asignacionAulaId,
        CancellationToken ct)
    {
        var semaforo = Semaforos.GetOrAdd(asignacionAulaId, _ => new SemaphoreSlim(1, 1));
        await semaforo.WaitAsync(ct);

        try
        {
            if (context.Database.IsRelational() &&
                (context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"""SELECT "Id" FROM "AsignacionesAula" WHERE "Id" = {asignacionAulaId} FOR UPDATE""",
                    ct);
            }
        }
        catch
        {
            semaforo.Release();
            throw;
        }

        return new Liberacion(semaforo);
    }

    private sealed class Liberacion : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaforo;

        public Liberacion(SemaphoreSlim semaforo) => _semaforo = semaforo;

        public ValueTask DisposeAsync()
        {
            _semaforo.Release();
            return ValueTask.CompletedTask;
        }
    }
}
