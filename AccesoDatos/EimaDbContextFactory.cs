using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccesoDatos;

/// <summary>
/// Permite generar migraciones sin levantar el host de la API.
/// </summary>
public class EimaDbContextFactory : IDesignTimeDbContextFactory<EimaDbContext>
{
    public EimaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EimaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=eima;Username=postgres;Password=postgres")
            .Options;

        return new EimaDbContext(options);
    }
}
