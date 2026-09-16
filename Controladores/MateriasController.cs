using System.Globalization;
using System.Text;
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
public class MateriasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public MateriasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Materias agrupadas por área para el formulario de contacto (público). Solo activas.</summary>
    [AllowAnonymous]
    [HttpGet("catalogo-por-area")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MateriaCatalogoAreaDto>>> GetCatalogoPorArea(CancellationToken ct)
    {
        var materias = await _context.Materias
            .AsNoTracking()
            .Where(m => m.Activa && m.Area != null && m.Area != "")
            .OrderBy(m => m.Area)
            .ThenBy(m => m.Nombre)
            .Select(m => new { m.Id, m.Nombre, Area = m.Area! })
            .ToListAsync(ct);

        // Orden preferido del catálogo semilla; el resto de áreas al final.
        var ordenAreas = MateriasCatalogoSemilla.Filas
            .Select((f, i) => (f.Area, i))
            .ToDictionary(x => x.Area, x => x.i, StringComparer.OrdinalIgnoreCase);

        var resultado = materias
            .GroupBy(m => m.Area)
            .OrderBy(g => ordenAreas.TryGetValue(g.Key, out var idx) ? idx : int.MaxValue)
            .ThenBy(g => g.Key)
            .Select(g => new MateriaCatalogoAreaDto(
                g.Key,
                g.OrderBy(m => m.Nombre)
                    .Select(m => new MateriaCatalogoItemDto(m.Id, m.Nombre))
                    .ToList()))
            .ToList();

        return Ok(resultado);
    }

    /// <summary>Listado de materias con filtros (admin / combos).</summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? buscar,
        [FromQuery] string? area,
        [FromQuery] string? estado,
        [FromQuery] int? pagina,
        [FromQuery] int? limite,
        CancellationToken ct = default)
    {
        IQueryable<Materia> query = _context.Materias.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLowerInvariant();
            query = query.Where(m =>
                m.Nombre.ToLower().Contains(term) ||
                (m.Area != null && m.Area.ToLower().Contains(term)) ||
                (m.Descripcion != null && m.Descripcion.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(area) && !area.Equals("todas", StringComparison.OrdinalIgnoreCase))
            query = query.Where(m => m.Area == area.Trim());

        if (!string.IsNullOrWhiteSpace(estado) && !estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var activa = estado.Trim().Equals("activo", StringComparison.OrdinalIgnoreCase)
                         || estado.Trim().Equals("activa", StringComparison.OrdinalIgnoreCase);
            query = query.Where(m => m.Activa == activa);
        }

        query = query.OrderBy(m => m.Area).ThenBy(m => m.Nombre);

        // Sin paginación: respuesta plana (combos / listados simples)
        if (pagina is null && limite is null)
        {
            var todas = await query
                .Select(m => new MateriaListadoDto
                {
                    Id = m.Id,
                    Nombre = m.Nombre,
                    Area = m.Area,
                    Descripcion = m.Descripcion,
                    DuracionHoras = m.DuracionHoras,
                    PrecioPorClase = m.PrecioPorClase,
                    Activa = m.Activa
                })
                .ToListAsync(ct);
            return Ok(todas);
        }

        var lim = limite is null or < 1 ? 10 : Math.Min(limite.Value, 100);
        var pag = pagina is null or < 1 ? 1 : pagina.Value;

        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling(totalRegistros / (double)lim);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pag > paginasTotales) pag = paginasTotales;

        var datos = await query
            .Skip((pag - 1) * lim)
            .Take(lim)
            .Select(m => new MateriaListadoDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Area = m.Area,
                Descripcion = m.Descripcion,
                DuracionHoras = m.DuracionHoras,
                PrecioPorClase = m.PrecioPorClase,
                Activa = m.Activa
            })
            .ToListAsync(ct);

        return Ok(new
        {
            datos,
            paginaActual = pag,
            limite = lim,
            totalRegistros,
            paginasTotales
        });
    }

    [Authorize]
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var materia = await _context.Materias
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MateriaListadoDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Area = m.Area,
                Descripcion = m.Descripcion,
                DuracionHoras = m.DuracionHoras,
                PrecioPorClase = m.PrecioPorClase,
                Activa = m.Activa
            })
            .FirstOrDefaultAsync(ct);

        return materia == null
            ? NotFound(new { mensaje = "No se encontró la materia solicitada." })
            : Ok(materia);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] GuardarMateriaDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { mensaje = "El nombre es obligatorio." });

        var area = string.IsNullOrWhiteSpace(dto.Area) ? null : dto.Area.Trim();

        if (await ExisteNombreDuplicadoAsync(nombre, excluirId: null, ct))
        {
            return BadRequest(new
            {
                mensaje = "Ya existe una materia con ese nombre (sin distinguir mayúsculas, minúsculas ni acentos)."
            });
        }

        var entidad = new Materia
        {
            Nombre = nombre,
            Area = area,
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            DuracionHoras = dto.DuracionHoras,
            PrecioPorClase = dto.PrecioPorClase,
            Activa = dto.Activa
        };

        _context.Materias.Add(entidad);
        await _context.SaveChangesAsync(ct);

        var result = Map(entidad);
        return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, result);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int id, [FromBody] GuardarMateriaDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia == null)
            return NotFound(new { mensaje = "No se encontró la materia solicitada." });

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { mensaje = "El nombre es obligatorio." });

        var area = string.IsNullOrWhiteSpace(dto.Area) ? null : dto.Area.Trim();

        if (await ExisteNombreDuplicadoAsync(nombre, excluirId: id, ct))
        {
            return BadRequest(new
            {
                mensaje = "Ya existe una materia con ese nombre (sin distinguir mayúsculas, minúsculas ni acentos)."
            });
        }

        materia.Nombre = nombre;
        materia.Area = area;
        materia.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        materia.DuracionHoras = dto.DuracionHoras;
        materia.PrecioPorClase = dto.PrecioPorClase;
        materia.Activa = dto.Activa;

        await _context.SaveChangesAsync(ct);
        return Ok(Map(materia));
    }

    /// <summary>Alterna Activa (baja/alta lógica).</summary>
    [Authorize]
    [HttpPatch("{id:int}/cambiar-estado")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(int id, CancellationToken ct)
    {
        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (materia == null)
            return NotFound(new { mensaje = "No se encontró la materia solicitada." });

        materia.Activa = !materia.Activa;
        await _context.SaveChangesAsync(ct);

        return Ok(new { id = materia.Id, activa = materia.Activa });
    }

    /// <summary>
    /// Compara nombres ignorando mayúsculas/minúsculas y acentos
    /// (p. ej. "Geometría" ≡ "geometria").
    /// </summary>
    private async Task<bool> ExisteNombreDuplicadoAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        var nombreNorm = NormalizarParaComparacion(nombre);
        var nombres = await _context.Materias
            .AsNoTracking()
            .Where(m => excluirId == null || m.Id != excluirId.Value)
            .Select(m => m.Nombre)
            .ToListAsync(ct);

        return nombres.Any(n => NormalizarParaComparacion(n) == nombreNorm);
    }

    /// <summary>Minúsculas + sin diacríticos (á→a, é→e); conserva ñ.</summary>
    private static string NormalizarParaComparacion(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        // Preservar ñ (no es un acento: "año" ≠ "ano").
        const char marcadorEne = '\u0001';
        var preparado = valor.Trim()
            .ToLowerInvariant()
            .Replace('ñ', marcadorEne);

        var formD = preparado.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace(marcadorEne, 'ñ');
    }

    private static MateriaListadoDto Map(Materia m) => new()
    {
        Id = m.Id,
        Nombre = m.Nombre,
        Area = m.Area,
        Descripcion = m.Descripcion,
        DuracionHoras = m.DuracionHoras,
        PrecioPorClase = m.PrecioPorClase,
        Activa = m.Activa
    };
}
