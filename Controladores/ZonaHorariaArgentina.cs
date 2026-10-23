using System.Globalization;

namespace Controladores;

/// <summary>
/// Zona horaria de Argentina usada en los informes y las exportaciones (HU27/HU28).
/// Usa el identificador IANA y cae al de Windows o a UTC-3 fijo según el sistema.
/// </summary>
internal static class ZonaHorariaArgentina
{
    private static readonly TimeZoneInfo Zona = Resolver();

    public static DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    public static DateTime AUtc(DateTime localSinZona) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localSinZona, DateTimeKind.Unspecified), Zona);

    public static string FormatearFechaHora(DateTime utc) =>
        ALocal(utc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string FormatearFecha(DateTime utc) =>
        ALocal(utc).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static TimeZoneInfo Resolver()
    {
        var ids = new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" };

        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }
}
