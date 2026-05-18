using System.Security.Claims;
using AccesoDatos;
using Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace Controladores;

[ApiController]
[Route("api/[controller]")]
public class PersonasController : ControllerBase
{
    private readonly EimaDbContext _context;

    public PersonasController(EimaDbContext context)
    {
        _context = context;
    }

    /// <summary>Datos de la persona autenticada (perfil): nombre, apellido, DNI, contacto y correo de cuenta.</summary>
    [Authorize]
    [HttpGet("mi-perfil")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MiPerfil(CancellationToken ct)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idStr, out var personaId))
            return Unauthorized(new { mensaje = "No se pudo identificar al usuario." });

        var persona = await _context.Personas
            .AsNoTracking()
            .Include(p => p.Rol)
            .Include(p => p.CuentaUsuario)
            .FirstOrDefaultAsync(p => p.Id == personaId, ct);

        if (persona == null)
            return NotFound(new { mensaje = "No se encontró el perfil asociado a la cuenta." });

        return Ok(new
        {
            personaId = persona.Id,
            nombre = persona.Nombre,
            apellido = persona.Apellido,
            dni = persona.Dni,
            telefono = persona.Telefono,
            direccion = persona.Direccion,
            correoElectronico = persona.CuentaUsuario?.CorreoElectronico ?? string.Empty,
            rol = persona.Rol?.Nombre ?? string.Empty,
            activo = persona.Activo
        });
    }

    /// <summary>Lista personas con rol, tipo de colaborador y cuenta, con filtros de rol, estado, búsqueda y paginación en el servidor de a 20 registros (HU15).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? rol,
        [FromQuery] string? estado,
        [FromQuery] string? buscar,
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 20,
        CancellationToken ct = default)
    {
        IQueryable<Persona> query = _context.Personas;

        // 1. Filtrar por búsqueda
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLowerInvariant();
            query = query.Where(p => p.Nombre.ToLower().Contains(term) || 
                                     p.Apellido.ToLower().Contains(term) || 
                                     p.Dni.Contains(term));
        }

        // 2. Filtrar por rol (UI friendly names)
        if (!string.IsNullOrWhiteSpace(rol) && rol.ToLowerInvariant() != "todos")
        {
            var rolNombreNorm = rol.Trim().ToLowerInvariant() switch
            {
                "alumno" => RolesSistema.Alumno,
                "docente" => RolesSistema.Profesor,
                "colaborador" => RolesSistema.Secretaria,
                _ => rol.Trim()
            };
            query = query.Where(p => p.Rol.Nombre == rolNombreNorm);
        }

        // 3. Filtrar por estado (Baja lógica)
        if (!string.IsNullOrWhiteSpace(estado) && estado.ToLowerInvariant() != "todos")
        {
            var esActivo = estado.Trim().ToLowerInvariant() == "activo";
            query = query.Where(p => p.Activo == esActivo);
        }

        // 4. Obtener totales para la paginación
        var totalRegistros = await query.CountAsync(ct);
        var paginasTotales = (int)Math.Ceiling((double)totalRegistros / limite);
        if (paginasTotales < 1) paginasTotales = 1;
        if (pagina < 1) pagina = 1;
        if (pagina > paginasTotales) pagina = paginasTotales;

        // 5. Paginar y obtener datos ordenados alfabéticamente
        var list = await query
            .AsSplitQuery()
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .Include(p => p.CuentaUsuario)
            .OrderBy(p => p.Apellido)
            .ThenBy(p => p.Nombre)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync(ct);

        return Ok(new
        {
            datos = list,
            paginaActual = pagina,
            limite = limite,
            totalRegistros = totalRegistros,
            paginasTotales = paginasTotales
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetById(int id, CancellationToken ct)
    {
        var persona = await _context.Personas
            .AsSplitQuery()
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .Include(p => p.CuentaUsuario)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return persona == null ? NotFound() : Ok(persona);
    }

    /// <summary>Alterna el estado Activo de una persona (Baja/Alta lógica) con reglas especiales para colaboradores (HU14).</summary>
    [HttpPatch("{id:int}/cambiar-estado")]
    public async Task<IActionResult> CambiarEstado(int id, CancellationToken ct)
    {
        var persona = await _context.Personas
            .Include(p => p.Rol)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (persona == null) return NotFound();

        persona.Activo = !persona.Activo;
        persona.FechaBaja = persona.Activo ? null : DateTime.UtcNow;

        // Lógica especial de baja/alta para colaboradores administrativos (HU14)
        if (persona.Rol?.Nombre == RolesSistema.Secretaria)
        {
            if (!persona.Activo)
            {
                persona.ActivoComoColaborador = false;
                persona.FechaFinContratacion = DateTime.UtcNow;
            }
            else
            {
                persona.ActivoComoColaborador = true;
                persona.FechaFinContratacion = null;
            }
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new 
        { 
            id = persona.Id, 
            activo = persona.Activo, 
            fechaBaja = persona.FechaBaja,
            activoComoColaborador = persona.ActivoComoColaborador,
            fechaFinContratacion = persona.FechaFinContratacion
        });
    }

    /// <summary>Crea una nueva persona con validaciones de campos obligatorios, formatos y asignación dinámica de rol (HU08, HU09, HU10, HU11).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear([FromBody] GuardarPersonaDto dto, CancellationToken ct)
    {
        var validacion = new Controladores.Autenticacion.ResultadoValidacion();

        // 1. Validaciones de campos obligatorios y formatos (HU08, HU09)
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            validacion.Agregar(nameof(dto.Nombre), "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            validacion.Agregar(nameof(dto.Apellido), "El apellido es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Dni))
            validacion.Agregar(nameof(dto.Dni), "El DNI es obligatorio.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Dni.Trim(), @"^\d+$"))
            validacion.Agregar(nameof(dto.Dni), "El DNI debe contener solo números.");

        if (string.IsNullOrWhiteSpace(dto.Telefono))
            validacion.Agregar(nameof(dto.Telefono), "El teléfono es obligatorio.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Telefono.Trim(), @"^\d{7,15}$"))
            validacion.Agregar(nameof(dto.Telefono), "Formato de teléfono inválido. Solo dígitos, longitud 7-15.");

        if (string.IsNullOrWhiteSpace(dto.Rol))
            validacion.Agregar(nameof(dto.Rol), "El rol es obligatorio.");

        // Validar salario positivo para colaborador (HU11 - CA03)
        if (dto.Rol != null && dto.Rol.Trim().ToLowerInvariant() == "colaborador" && dto.Salario.HasValue && dto.Salario.Value <= 0)
        {
            validacion.Agregar(nameof(dto.Salario), "El salario debe ser un número positivo.");
        }

        if (!validacion.EsValido)
            return BadRequest(new { errores = validacion.Errores });

        var dniNormalizado = dto.Dni.Trim();

        // 2. Verificar DNI duplicado (HU08 - CA02)
        if (await _context.Personas.AnyAsync(p => p.Dni == dniNormalizado, ct))
        {
            return BadRequest(new
            {
                errores = new[]
                {
                    new Controladores.Autenticacion.ErrorCampo(nameof(dto.Dni), "El DNI ya se encuentra registrado.")
                }
            });
        }

        // 3. Mapeo de Roles de UI a Roles del Sistema (HU10)
        var rolNombre = (dto.Rol ?? "").Trim().ToLowerInvariant() switch
        {
            "alumno" => RolesSistema.Alumno,
            "docente" => RolesSistema.Profesor,
            "colaborador" => RolesSistema.Secretaria,
            _ => (dto.Rol ?? "").Trim().ToLowerInvariant()
        };

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre, ct);
        if (rol == null)
        {
            return BadRequest(new
            {
                errores = new[]
                {
                    new Controladores.Autenticacion.ErrorCampo(nameof(dto.Rol), $"El rol '{dto.Rol}' no existe en el sistema.")
                }
            });
        }

        // 4. Resolver Tipo de Colaborador para Administrativos si aplica
        int? tipoColaboradorId = null;
        if (rolNombre == RolesSistema.Secretaria && !string.IsNullOrWhiteSpace(dto.TipoColaborador))
        {
            var tipoNorm = dto.TipoColaborador.Trim();
            var tipo = await _context.TiposColaborador.FirstOrDefaultAsync(t => t.Tipo == tipoNorm, ct);
            if (tipo == null)
            {
                tipo = new TipoColaborador
                {
                    Tipo = tipoNorm,
                    Descripcion = $"Tipo de colaborador '{tipoNorm}' creado automáticamente desde el panel de administración."
                };
                _context.TiposColaborador.Add(tipo);
                await _context.SaveChangesAsync(ct);
            }
            tipoColaboradorId = tipo.Id;
        }

        // 5. Crear la Persona
        var persona = new Persona
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dniNormalizado,
            Telefono = dto.Telefono.Trim(),
            Direccion = dto.Direccion.Trim(),
            FechaRegistro = DateTime.UtcNow,
            RolId = rol.Id,
            Activo = true,

            // Campos de Alumno
            Colegio = rolNombre == RolesSistema.Alumno ? dto.Colegio?.Trim() : null,
            GradoCurso = rolNombre == RolesSistema.Alumno ? dto.GradoCurso?.Trim() : null,
            NivelEducativo = rolNombre == RolesSistema.Alumno ? dto.NivelEducativo?.Trim() : null,

            // Campos de Docente
            Especialidades = rolNombre == RolesSistema.Profesor ? dto.Especialidades?.Trim() : null,
            Titulo = rolNombre == RolesSistema.Profesor ? dto.Titulo?.Trim() : null,
            FechaIngresoDocente = rolNombre == RolesSistema.Profesor ? (dto.FechaIngresoDocente ?? DateTime.UtcNow) : null,

            // Campos de Colaborador
            TipoColaboradorId = tipoColaboradorId,
            FechaContratacion = rolNombre == RolesSistema.Secretaria ? (dto.FechaContratacion ?? DateTime.UtcNow) : null,
            Salario = rolNombre == RolesSistema.Secretaria ? dto.Salario : null,
            ActivoComoColaborador = rolNombre == RolesSistema.Secretaria ? true : null
        };

        _context.Personas.Add(persona);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = persona.Id }, persona);
    }

    /// <summary>Edita los datos de una persona con re-validación de DNI único (excluyendo a la propia persona) y formatos. (HU11, HU13)</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar(int id, [FromBody] GuardarPersonaDto dto, CancellationToken ct)
    {
        var persona = await _context.Personas
            .Include(p => p.Rol)
            .Include(p => p.TipoColaborador)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (persona == null)
            return NotFound(new { mensaje = "No se encontró la persona a editar." });

        // 1. Validaciones básicas manuales (iguales a las de Crear)
        var validacion = new Controladores.Autenticacion.ResultadoValidacion();

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            validacion.Agregar(nameof(dto.Nombre), "El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            validacion.Agregar(nameof(dto.Apellido), "El apellido es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Dni))
            validacion.Agregar(nameof(dto.Dni), "El DNI es obligatorio.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Dni.Trim(), @"^\d+$"))
            validacion.Agregar(nameof(dto.Dni), "El DNI debe contener solo números.");

        if (string.IsNullOrWhiteSpace(dto.Telefono))
            validacion.Agregar(nameof(dto.Telefono), "El teléfono es obligatorio.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(dto.Telefono.Trim(), @"^\d+$"))
            validacion.Agregar(nameof(dto.Telefono), "El teléfono debe contener solo números.");
        else if (dto.Telefono.Trim().Length < 7 || dto.Telefono.Trim().Length > 15)
            validacion.Agregar(nameof(dto.Telefono), "El teléfono debe tener entre 7 y 15 dígitos.");

        if (string.IsNullOrWhiteSpace(dto.Direccion))
            validacion.Agregar(nameof(dto.Direccion), "La dirección es obligatoria.");

        if (string.IsNullOrWhiteSpace(dto.Rol))
            validacion.Agregar(nameof(dto.Rol), "El rol es obligatorio.");

        // Validar salario positivo para colaborador (HU11 - CA03)
        if (dto.Rol != null && dto.Rol.Trim().ToLowerInvariant() == "colaborador" && dto.Salario.HasValue && dto.Salario.Value <= 0)
        {
            validacion.Agregar(nameof(dto.Salario), "El salario debe ser un número positivo.");
        }

        if (!validacion.EsValido)
            return BadRequest(new { errores = validacion.Errores });

        var dniNormalizado = dto.Dni.Trim();

        // 2. Verificar DNI duplicado (excluyendo a la propia persona)
        if (await _context.Personas.AnyAsync(p => p.Dni == dniNormalizado && p.Id != id, ct))
        {
            return BadRequest(new
            {
                errores = new[]
                {
                    new Controladores.Autenticacion.ErrorCampo(nameof(dto.Dni), "El DNI ya se encuentra registrado en otra persona.")
                }
            });
        }

        // 3. Mapeo de Roles de UI a Roles del Sistema
        var rolNombre = (dto.Rol ?? "").Trim().ToLowerInvariant() switch
        {
            "alumno" => RolesSistema.Alumno,
            "docente" => RolesSistema.Profesor,
            "colaborador" => RolesSistema.Secretaria,
            _ => (dto.Rol ?? "").Trim().ToLowerInvariant()
        };

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre, ct);
        if (rol == null)
        {
            return BadRequest(new
            {
                errores = new[]
                {
                    new Controladores.Autenticacion.ErrorCampo(nameof(dto.Rol), $"El rol especificado '{rolNombre}' no está configurado.")
                }
            });
        }

        // 4. Actualizar datos básicos
        persona.Nombre = dto.Nombre.Trim();
        persona.Apellido = dto.Apellido.Trim();
        persona.Dni = dniNormalizado;
        persona.Telefono = dto.Telefono.Trim();
        persona.Direccion = dto.Direccion.Trim();
        persona.RolId = rol.Id;

        // 5. HU11 - CA02: Limpieza de campos opcionales del resto de roles para evitar orphan data (evitar contaminación cruzada)
        var rolUI = dto.Rol.Trim().ToLowerInvariant();
        if (rolUI == "alumno")
        {
            // Opcionales de Alumno
            persona.Colegio = dto.Colegio?.Trim();
            persona.GradoCurso = dto.GradoCurso?.Trim();
            persona.NivelEducativo = dto.NivelEducativo?.Trim();

            // Limpiar Docente
            persona.Especialidades = null;
            persona.Titulo = null;
            persona.FechaIngresoDocente = null;

            // Limpiar Colaborador
            persona.FechaContratacion = null;
            persona.FechaFinContratacion = null;
            persona.Salario = null;
            persona.ActivoComoColaborador = null;
            persona.TipoColaboradorId = null;
        }
        else if (rolUI == "docente")
        {
            // Limpiar Alumno
            persona.Colegio = null;
            persona.GradoCurso = null;
            persona.NivelEducativo = null;

            // Opcionales de Docente
            persona.Especialidades = dto.Especialidades?.Trim();
            persona.Titulo = dto.Titulo?.Trim();
            persona.FechaIngresoDocente = dto.FechaIngresoDocente ?? DateTime.UtcNow;

            // Limpiar Colaborador
            persona.FechaContratacion = null;
            persona.FechaFinContratacion = null;
            persona.Salario = null;
            persona.ActivoComoColaborador = null;
            persona.TipoColaboradorId = null;
        }
        else if (rolUI == "colaborador")
        {
            // Limpiar Alumno
            persona.Colegio = null;
            persona.GradoCurso = null;
            persona.NivelEducativo = null;

            // Limpiar Docente
            persona.Especialidades = null;
            persona.Titulo = null;
            persona.FechaIngresoDocente = null;

            // Opcionales de Colaborador
            persona.FechaContratacion = dto.FechaContratacion ?? DateTime.UtcNow;
            persona.FechaFinContratacion = dto.FechaFinContratacion;
            persona.Salario = dto.Salario;
            persona.ActivoComoColaborador = dto.ActivoComoColaborador ?? true;

            if (!string.IsNullOrWhiteSpace(dto.TipoColaborador))
            {
                var tipoNorm = dto.TipoColaborador.Trim();
                var tipoEntidad = await _context.TiposColaborador.FirstOrDefaultAsync(t => t.Tipo == tipoNorm, ct);
                if (tipoEntidad == null)
                {
                    tipoEntidad = new TipoColaborador { Tipo = tipoNorm, Descripcion = $"Tipo de colaborador creado durante edición de: {tipoNorm}" };
                    _context.TiposColaborador.Add(tipoEntidad);
                    await _context.SaveChangesAsync(ct);
                }
                persona.TipoColaboradorId = tipoEntidad.Id;
            }
            else
            {
                persona.TipoColaboradorId = null;
            }
        }

        await _context.SaveChangesAsync(ct);
        return Ok(persona);
    }
}
