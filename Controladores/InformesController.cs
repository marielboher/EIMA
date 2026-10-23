using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.SuperAdmin + "," + RolesSistema.Administrativo)]
public class InformesController : ControllerBase
{
    private readonly EimaDbContext _context;

    public InformesController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Indicadores del mes de asignaciones de aula (HU28): creadas, cerradas,
    /// abiertas al cierre, SLA y TME. La ventana usa el mes calendario en hora de Argentina.
    /// </summary>
    [HttpGet("AsignacionesAula")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InformeAsignacionesAulaDto>> GetAsignacionesAula(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        CancellationToken ct)
    {
        var referencia = ZonaHorariaArgentina.ALocal(DateTime.UtcNow);
        var anioFinal = anio ?? referencia.Year;
        var mesFinal = mes ?? referencia.Month;

        if (mesFinal is < 1 or > 12)
            return BadRequest(new { mensaje = "El mes debe estar entre 1 y 12." });

        if (anioFinal is < 2000 or > 2100)
            return BadRequest(new { mensaje = "El año debe estar entre 2000 y 2100." });

        var inicioLocal = new DateTime(anioFinal, mesFinal, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioUtc = ZonaHorariaArgentina.AUtc(inicioLocal);
        var finUtc = ZonaHorariaArgentina.AUtc(inicioLocal.AddMonths(1));

        var creadas = await _context.AsignacionesAula
            .AsNoTracking()
            .CountAsync(a => a.FechaAsignacion >= inicioUtc && a.FechaAsignacion < finUtc, ct);

        var cerradasDelMes = await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => a.FechaCierre != null && a.FechaCierre >= inicioUtc && a.FechaCierre < finUtc)
            .Select(a => new
            {
                a.FechaAsignacion,
                a.FechaCierre,
                a.FinVigencia
            })
            .ToListAsync(ct);

        var cerradas = cerradasDelMes.Count;
        var cerradasEnVigencia = cerradasDelMes.Count(a => a.FechaCierre!.Value <= a.FinVigencia);

        var abiertasAlCierre = await _context.AsignacionesAula
            .AsNoTracking()
            .CountAsync(a =>
                a.FechaAsignacion < finUtc &&
                (a.FechaCierre == null || a.FechaCierre >= finUtc), ct);

        var sla = cerradas > 0
            ? Math.Round(cerradasEnVigencia * 100.0 / cerradas, 2)
            : (double?)null;

        var tme = cerradas > 0
            ? Math.Round(cerradasDelMes.Average(a => (a.FechaCierre!.Value - a.FechaAsignacion).TotalDays), 2)
            : (double?)null;

        var informe = new InformeAsignacionesAulaDto
        {
            Anio = anioFinal,
            Mes = mesFinal,
            Periodo = $"{anioFinal:D4}-{mesFinal:D2}",
            FechaActualizacion = DateTime.UtcNow,
            Creadas = creadas,
            Cerradas = cerradas,
            AbiertasAlCierre = abiertasAlCierre,
            CerradasEnVigencia = cerradasEnVigencia,
            SlaPorcentaje = sla,
            TmeDias = tme,
            Definiciones = new DefinicionesInformeDto
            {
                Creadas = "Cantidad de asignaciones de aula con fecha de asignación dentro del mes seleccionado (hora de Argentina).",
                Cerradas = "Cantidad de asignaciones de aula con fecha de cierre dentro del mes seleccionado (hora de Argentina).",
                AbiertasAlCierre = "Asignaciones creadas hasta el fin del mes que seguían abiertas al cierre del mes (fecha de cierre nula o posterior).",
                Sla = "(Cerradas dentro de vigencia / cerradas totales) × 100. Dentro de vigencia: fecha de cierre ≤ fin de vigencia.",
                Tme = "Promedio de (fecha de cierre − fecha de asignación) en días para las asignaciones cerradas del mes."
            }
        };

        return Ok(informe);
    }
}
