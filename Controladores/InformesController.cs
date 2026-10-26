using System.Security.Claims;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesSistema.SuperAdmin + "," + RolesSistema.Administrativo)]
public class InformesController : ControllerBase
{
    private readonly ServicioInformeAsignaciones _informes;

    public InformesController(ServicioInformeAsignaciones informes)
    {
        _informes = informes;
    }

    /// <summary>
    /// Indicadores de asignaciones de aula (HU28) con periodo, materia, comparativo (HU31/HU33)
    /// y desglose por materia y docente (HU32).
    /// </summary>
    [HttpGet("AsignacionesAula")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InformeAsignacionesAulaDto>> GetAsignacionesAula(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] string? periodo,
        [FromQuery] int? materiaId,
        [FromQuery] bool comparar = false,
        CancellationToken ct = default)
    {
        var referencia = ZonaHorariaArgentina.ALocal(DateTime.UtcNow);
        var (informe, error) = await _informes.GenerarAsync(
            anio ?? referencia.Year,
            mes ?? referencia.Month,
            string.IsNullOrWhiteSpace(periodo) ? PeriodosInformeAsignacion.Mes : periodo,
            materiaId,
            comparar,
            ct);

        return error is null ? Ok(informe) : BadRequest(new { mensaje = error });
    }

    /// <summary>PDF alineado al informe en pantalla (HU34).</summary>
    [HttpGet("AsignacionesAula/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DescargarPdf(
        [FromQuery] int? anio,
        [FromQuery] int? mes,
        [FromQuery] string? periodo,
        [FromQuery] int? materiaId,
        [FromQuery] bool comparar = false,
        CancellationToken ct = default)
    {
        var referencia = ZonaHorariaArgentina.ALocal(DateTime.UtcNow);
        var (informe, error) = await _informes.GenerarAsync(
            anio ?? referencia.Year,
            mes ?? referencia.Month,
            string.IsNullOrWhiteSpace(periodo) ? PeriodosInformeAsignacion.Mes : periodo,
            materiaId,
            comparar,
            ct);

        if (error is not null)
            return BadRequest(new { mensaje = error });

        var pdf = GeneradorPdfInformeAsignaciones.Generar(informe!);
        var nombre = $"informe-asignaciones-{informe!.Anio:D4}-{informe.Mes:D2}.pdf";
        return File(pdf, "application/pdf", nombre);
    }

    /// <summary>Última elección de periodo y materia del usuario (HU33).</summary>
    [HttpGet("AsignacionesAula/preferencias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PreferenciaInformeDto>> GetPreferencia(CancellationToken ct)
    {
        if (!TryObtenerPersonaId(out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        return Ok(await _informes.ObtenerPreferenciaAsync(personaId, ct));
    }

    [HttpPut("AsignacionesAula/preferencias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PreferenciaInformeDto>> GuardarPreferencia(
        [FromBody] GuardarPreferenciaInformeDto dto,
        CancellationToken ct)
    {
        if (!TryObtenerPersonaId(out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var (preferencia, error) = await _informes.GuardarPreferenciaAsync(personaId, dto, ct);
        return error is null ? Ok(preferencia) : BadRequest(new { mensaje = error });
    }

    private bool TryObtenerPersonaId(out int personaId)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(id, out personaId);
    }
}
