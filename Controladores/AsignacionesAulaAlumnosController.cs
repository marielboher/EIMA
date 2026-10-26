using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

public partial class AsignacionesAulaController
{
    /// <summary>Alumnos con vínculo activo en el grupo y el resumen de cupo (HU29/HU30).</summary>
    [HttpGet("{id:int}/alumnos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListadoAlumnosGrupoDto>> GetAlumnos(int id, CancellationToken ct)
    {
        var listado = await _cupo.ListarInscriptosAsync(id, ct);
        return listado is null
            ? NotFound(new { mensaje = "No se encontró el grupo solicitado." })
            : Ok(listado);
    }

    /// <summary>Alumnos activos con inscripción Activa a la materia que todavía no ocupan el grupo.</summary>
    [HttpGet("{id:int}/alumnos/candidatos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CandidatoGrupoDto>>> GetCandidatos(
        int id,
        [FromQuery] string? buscar,
        CancellationToken ct)
    {
        var candidatos = await _cupo.ListarCandidatosAsync(id, buscar, ct);
        return candidatos is null
            ? NotFound(new { mensaje = "No se encontró el grupo solicitado." })
            : Ok(candidatos);
    }

    /// <summary>Vincula un alumno al grupo y reserva el cupo en la misma transacción.</summary>
    [HttpPost("{id:int}/alumnos")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VincularAlumno(int id, [FromBody] VincularAlumnoGrupoDto dto, CancellationToken ct)
    {
        if (dto.PersonaId <= 0)
            return BadRequest(new { mensaje = "El alumno es obligatorio." });

        var resultado = await _cupo.VincularAsync(id, dto.PersonaId, ct);
        if (!resultado.Exito)
            return ErrorCupo(resultado);

        var grupo = await ProyeccionAsync(id, ct);
        return CreatedAtAction(nameof(GetAlumnos), new { id }, new
        {
            mensaje = "Alumno vinculado al grupo.",
            alumno = resultado.Alumno,
            grupo
        });
    }

    /// <summary>Baja lógica del vínculo. Libera el cupo y conserva el histórico.</summary>
    [HttpDelete("{id:int}/alumnos/{personaId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DarBajaAlumno(int id, int personaId, CancellationToken ct)
    {
        var resultado = await _cupo.DarBajaVinculoAsync(id, personaId, ct);
        if (!resultado.Exito)
            return ErrorCupo(resultado);

        var grupo = await ProyeccionAsync(id, ct);
        return Ok(new
        {
            mensaje = "El alumno fue dado de baja del grupo.",
            alumno = resultado.Alumno,
            grupo
        });
    }

    private async Task<AsignacionAulaListadoDto?> ProyeccionAsync(int id, CancellationToken ct) =>
        await _context.AsignacionesAula
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(MapProjection())
            .FirstOrDefaultAsync(ct);

    private IActionResult ErrorCupo(ResultadoOperacionCupo resultado)
    {
        var cuerpo = new { mensaje = resultado.Mensaje, codigo = resultado.Codigo };
        return resultado.Codigo is CodigosCupo.NoEncontrado or CodigosCupo.SinVinculo or CodigosCupo.AlumnoInexistente
            ? NotFound(cuerpo)
            : BadRequest(cuerpo);
    }
}

public class VincularAlumnoGrupoDto
{
    public int PersonaId { get; set; }
}
