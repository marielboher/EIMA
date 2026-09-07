using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Controladores;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class InscripcionesMateriaController : ControllerBase
{
    private static readonly HashSet<string> ExtensionesComprobantePermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly EimaDbContext _context;
    private readonly IWebHostEnvironment _env;

    public InscripcionesMateriaController(EimaDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    /// <summary>Listado paginado con filtros (HU16).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] string? buscar,
        [FromQuery] int? materiaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 5,
        CancellationToken ct = default)
    {
        if (limite < 1) limite = 5;
        if (limite > 100) limite = 100;
        if (pagina < 1) pagina = 1;

        IQueryable<Inscripciones> query = _context.Inscripciones
            .AsNoTracking()
            .Include(i => i.Persona)
            .Include(i => i.Materia)
            .Include(i => i.Estado);

        if (!string.IsNullOrWhiteSpace(estado) &&
            !estado.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            var estadoNorm = estado.Trim();
            query = query.Where(i => i.Estado.Nombre == estadoNorm);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLowerInvariant();
            query = query.Where(i =>
                i.Persona.Nombre.ToLower().Contains(term) ||
                i.Persona.Apellido.ToLower().Contains(term) ||
                i.Persona.Dni.Contains(term));
        }

        if (materiaId is > 0)
            query = query.Where(i => i.MateriaId == materiaId.Value);

        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling(totalRegistros / (double)limite);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pagina > paginasTotales) pagina = paginasTotales;

        var list = await query
            .OrderByDescending(i => i.FechaInscripcion)
            .ThenBy(i => i.Persona.Apellido)
            .ThenBy(i => i.Persona.Nombre)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync(ct);

        var datos = list.Select(MapListado).ToList();

        return Ok(new
        {
            datos,
            paginaActual = pagina,
            limite,
            totalRegistros,
            paginasTotales
        });
    }

    /// <summary>Detalle de inscripción con historial de pagos (HU18).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var inscripcion = await CargarDetalleAsync(id, ct);
        if (inscripcion == null)
            return NotFound(new { mensaje = "No se encontró la inscripción solicitada." });

        return Ok(MapDetalle(inscripcion));
    }

    /// <summary>Alta de inscripción (HU17).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] GuardarInscripcionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (dto.ClasesTotales <= 0)
            return BadRequest(new { mensaje = "Las clases contratadas deben ser mayores a cero." });

        var alumno = await _context.Personas
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.Id == dto.PersonaId, ct);

        if (alumno == null || alumno.Rol?.Nombre != RolesSistema.Alumno)
            return BadRequest(new { mensaje = "El alumno indicado no existe." });

        if (!alumno.Activo)
            return BadRequest(new { mensaje = "Solo se puede inscribir a alumnos activos." });

        var materia = await _context.Materias.FirstOrDefaultAsync(m => m.Id == dto.MateriaId, ct);
        if (materia == null)
            return BadRequest(new { mensaje = "La materia indicada no existe." });

        if (!materia.Activa)
            return BadRequest(new { mensaje = "Solo se puede inscribir en materias activas." });

        var estadosBloqueo = EstadosInscripcionSistema.BloqueanDuplicado;
        var duplicado = await _context.Inscripciones
            .Include(i => i.Estado)
            .AnyAsync(i =>
                i.PersonaId == dto.PersonaId &&
                i.MateriaId == dto.MateriaId &&
                estadosBloqueo.Contains(i.Estado.Nombre), ct);

        if (duplicado)
        {
            return BadRequest(new
            {
                mensaje = "Ya existe una inscripción activa o suspendida para ese alumno en la misma materia."
            });
        }

        var estadoActiva = await ObtenerEstadoInscripcionAsync(EstadosInscripcionSistema.Activa, ct);
        if (estadoActiva == null)
            return StatusCode(500, new { mensaje = "No está configurado el catálogo de estados de inscripción." });

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entidad = new Inscripciones
            {
                PersonaId = dto.PersonaId,
                MateriaId = dto.MateriaId,
                FechaInscripcion = DateTime.UtcNow,
                ClasesTotales = dto.ClasesTotales,
                ClasesTomadas = 0,
                EstadoId = estadoActiva.Id,
                MontoPagado = 0
            };

            _context.Inscripciones.Add(entidad);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var creada = await CargarDetalleAsync(entidad.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = entidad.Id }, MapDetalle(creada!));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Edición de estado y clases (HU19).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarInscripcionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var inscripcion = await _context.Inscripciones
            .Include(i => i.Estado)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inscripcion == null)
            return NotFound(new { mensaje = "No se encontró la inscripción solicitada." });

        var clasesTomadas = dto.ClasesTomadas ?? inscripcion.ClasesTomadas;
        if (dto.ClasesTotales < clasesTomadas)
        {
            return BadRequest(new
            {
                mensaje = $"Las clases totales no pueden ser menores a las ya tomadas ({clasesTomadas})."
            });
        }

        var estadoSolicitado = dto.Estado?.Trim() ?? string.Empty;
        var estadoEntidad = await ObtenerEstadoInscripcionAsync(estadoSolicitado, ct);
        if (estadoEntidad == null)
            return BadRequest(new { mensaje = "El estado indicado no es válido." });

        // CA03 — finalización automática
        if (clasesTomadas == dto.ClasesTotales)
        {
            estadoEntidad = await ObtenerEstadoInscripcionAsync(EstadosInscripcionSistema.Finalizada, ct)
                            ?? estadoEntidad;
        }

        inscripcion.ClasesTotales = dto.ClasesTotales;
        inscripcion.ClasesTomadas = clasesTomadas;
        inscripcion.EstadoId = estadoEntidad.Id;

        await _context.SaveChangesAsync(ct);

        var actualizada = await CargarDetalleAsync(id, ct);
        return Ok(MapDetalle(actualizada!));
    }

    /// <summary>Baja lógica: Estado = Cancelada (HU21).</summary>
    [HttpPatch("{id:int}/baja")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DarDeBaja(int id, CancellationToken ct)
    {
        var inscripcion = await _context.Inscripciones
            .Include(i => i.Estado)
            .Include(i => i.Pagos).ThenInclude(p => p.Estado)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inscripcion == null)
            return NotFound(new { mensaje = "No se encontró la inscripción solicitada." });

        var estadoActual = inscripcion.Estado.Nombre;
        if (EstadosInscripcionSistema.YaInactivos.Contains(estadoActual, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { mensaje = "La inscripción ya se encuentra inactiva." });
        }

        var estadoCancelada = await ObtenerEstadoInscripcionAsync(EstadosInscripcionSistema.Cancelada, ct);
        if (estadoCancelada == null)
            return StatusCode(500, new { mensaje = "No está configurado el estado Cancelada." });

        var montoConfirmado = inscripcion.Pagos
            .Where(p => p.Estado.Nombre == EstadosPagoSistema.Confirmado)
            .Sum(p => p.Monto);

        inscripcion.EstadoId = estadoCancelada.Id;
        await _context.SaveChangesAsync(ct);

        var actualizada = await CargarDetalleAsync(id, ct);
        return Ok(new
        {
            inscripcion = MapDetalle(actualizada!),
            advertenciaPagos = montoConfirmado > 0
                ? $"La inscripción tiene pagos confirmados por {montoConfirmado:C}."
                : null,
            montoConfirmado
        });
    }

    /// <summary>Registra un pago asociado a la inscripción (HU20). Comprobante opcional (imagen).</summary>
    [HttpPost("{id:int}/pagos")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarPago(
        int id,
        [FromForm] decimal monto,
        [FromForm] string metodoPago,
        [FromForm] DateTime fechaPago,
        [FromForm] string? estado,
        [FromForm] string? observaciones,
        [FromForm] IFormFile? comprobante,
        CancellationToken ct)
    {
        if (monto <= 0)
            return BadRequest(new { mensaje = "El monto debe ser positivo." });

        if (string.IsNullOrWhiteSpace(metodoPago))
            return BadRequest(new { mensaje = "El método de pago es obligatorio." });

        if (fechaPago == default)
            return BadRequest(new { mensaje = "La fecha de pago es obligatoria." });

        var inscripcion = await _context.Inscripciones
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (inscripcion == null)
            return NotFound(new { mensaje = "No se encontró la inscripción solicitada." });

        var nombreEstadoPago = string.IsNullOrWhiteSpace(estado)
            ? EstadosPagoSistema.Pendiente
            : estado.Trim();

        var estadoPago = await ObtenerEstadoPagoAsync(nombreEstadoPago, ct);
        if (estadoPago == null)
            return BadRequest(new { mensaje = "El estado de pago indicado no es válido." });

        string? rutaComprobante = null;
        if (comprobante is { Length: > 0 })
        {
            var (ok, pathOrError) = await GuardarComprobanteAsync(comprobante, id, ct);
            if (!ok)
                return BadRequest(new { mensaje = pathOrError });
            rutaComprobante = pathOrError;
        }

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var fechaUtc = fechaPago.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(fechaPago, DateTimeKind.Utc)
                : fechaPago.ToUniversalTime();

            var pago = new Pago
            {
                PersonaId = inscripcion.PersonaId,
                InscripcionMateriaId = inscripcion.Id,
                FechaPago = fechaUtc,
                Monto = monto,
                MetodoPago = metodoPago.Trim(),
                EstadoId = estadoPago.Id,
                Comprobante = rutaComprobante,
                Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim()
            };

            _context.Pagos.Add(pago);

            if (estadoPago.Nombre == EstadosPagoSistema.Confirmado)
                inscripcion.MontoPagado += monto;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var actualizada = await CargarDetalleAsync(id, ct);
            return StatusCode(StatusCodes.Status201Created, MapDetalle(actualizada!));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Inscripciones?> CargarDetalleAsync(int id, CancellationToken ct)
    {
        return await _context.Inscripciones
            .AsNoTracking()
            .AsSplitQuery()
            .Include(i => i.Persona)
            .Include(i => i.Materia)
            .Include(i => i.Estado)
            .Include(i => i.Pagos).ThenInclude(p => p.Estado)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    private async Task<EstadoInscripcion?> ObtenerEstadoInscripcionAsync(string nombre, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;
        return await _context.EstadosInscripcion
            .FirstOrDefaultAsync(e => e.Nombre == nombre.Trim(), ct);
    }

    private async Task<EstadoPago?> ObtenerEstadoPagoAsync(string nombre, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;
        return await _context.EstadosPago
            .FirstOrDefaultAsync(e => e.Nombre == nombre.Trim(), ct);
    }

    private async Task<(bool ok, string pathOrError)> GuardarComprobanteAsync(
        IFormFile archivo,
        int inscripcionId,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(archivo.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !ExtensionesComprobantePermitidas.Contains(ext))
            return (false, "El comprobante debe ser una imagen (jpg, jpeg, png, webp o gif).");

        if (archivo.Length > 5 * 1024 * 1024)
            return (false, "El comprobante no puede superar los 5 MB.");

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        var carpetaRelativa = Path.Combine("uploads", "comprobantes");
        var carpetaAbsoluta = Path.Combine(webRoot, carpetaRelativa);
        Directory.CreateDirectory(carpetaAbsoluta);

        var nombreArchivo = $"insc-{inscripcionId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext.ToLowerInvariant()}";
        var rutaAbsoluta = Path.Combine(carpetaAbsoluta, nombreArchivo);

        await using (var stream = System.IO.File.Create(rutaAbsoluta))
            await archivo.CopyToAsync(stream, ct);

        var rutaPublica = $"/{carpetaRelativa.Replace('\\', '/')}/{nombreArchivo}";
        return (true, rutaPublica);
    }

    private static InscripcionListadoDto MapListado(Inscripciones i) => new()
    {
        Id = i.Id,
        PersonaId = i.PersonaId,
        MateriaId = i.MateriaId,
        Alumno = $"{i.Persona.Apellido}, {i.Persona.Nombre}".Trim(' ', ','),
        AlumnoDni = i.Persona.Dni,
        Materia = i.Materia.Nombre,
        FechaInscripcion = i.FechaInscripcion,
        ClasesTomadas = i.ClasesTomadas,
        ClasesTotales = i.ClasesTotales,
        Estado = i.Estado.Nombre,
        EstadoId = i.EstadoId,
        MontoPagado = i.MontoPagado
    };

    private static InscripcionDetalleDto MapDetalle(Inscripciones i)
    {
        var dto = new InscripcionDetalleDto
        {
            Id = i.Id,
            PersonaId = i.PersonaId,
            MateriaId = i.MateriaId,
            Alumno = $"{i.Persona.Apellido}, {i.Persona.Nombre}".Trim(' ', ','),
            AlumnoDni = i.Persona.Dni,
            Materia = i.Materia.Nombre,
            FechaInscripcion = i.FechaInscripcion,
            ClasesTomadas = i.ClasesTomadas,
            ClasesTotales = i.ClasesTotales,
            Estado = i.Estado.Nombre,
            EstadoId = i.EstadoId,
            MontoPagado = i.MontoPagado,
            Pagos = i.Pagos
                .OrderByDescending(p => p.FechaPago)
                .Select(p => new PagoDetalleDto
                {
                    Id = p.Id,
                    Monto = p.Monto,
                    MetodoPago = p.MetodoPago,
                    FechaPago = p.FechaPago,
                    Estado = p.Estado.Nombre,
                    Comprobante = p.Comprobante,
                    Observaciones = p.Observaciones
                })
                .ToList()
        };

        dto.MontoConfirmadoAdvertencia = i.Pagos
            .Where(p => p.Estado.Nombre == EstadosPagoSistema.Confirmado)
            .Sum(p => p.Monto);

        return dto;
    }
}
