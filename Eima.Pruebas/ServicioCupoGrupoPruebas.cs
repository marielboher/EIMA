using Controladores;
using Entidades;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Eima.Pruebas;

public class ServicioCupoGrupoPruebas : IDisposable
{
    private readonly BaseCupo _base = new();

    [Fact]
    public async Task Alta_crea_vinculo_y_aumenta_cupo()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 3);
        var alumnoId = _base.CrearAlumno(db, "301");

        var resultado = await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.NotNull(resultado.Alumno);
        Assert.True(resultado.Alumno!.FechaAsignacion <= DateTime.UtcNow);

        var grupo = await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId);
        Assert.Equal(1, grupo.CuposOcupados);
        Assert.Equal(2, grupo.CupoHabilitado - grupo.CuposOcupados);
    }

    [Fact]
    public async Task Cupo_lleno_rechaza_sin_modificar_el_contador()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 1);
        var primero = _base.CrearAlumno(db, "311");
        var segundo = _base.CrearAlumno(db, "312");
        Assert.True((await servicio.VincularAsync(grupoId, primero, CancellationToken.None)).Exito);

        var resultado = await servicio.VincularAsync(grupoId, segundo, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal(CodigosCupo.CupoCompleto, resultado.Codigo);
        Assert.Contains("completo", resultado.Mensaje, StringComparison.OrdinalIgnoreCase);
        var grupo = await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId);
        Assert.Equal(1, grupo.CuposOcupados);
    }

    [Fact]
    public async Task Duplicado_se_rechaza_y_el_cupo_no_cambia()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 4);
        var alumnoId = _base.CrearAlumno(db, "321");
        Assert.True((await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None)).Exito);

        var resultado = await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None);

        Assert.Equal(CodigosCupo.Duplicado, resultado.Codigo);
        var grupo = await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId);
        Assert.Equal(1, grupo.CuposOcupados);
        Assert.Equal(1, await db.InscripcionesGrupo.CountAsync(v => v.AsignacionAulaId == grupoId && v.FechaBaja == null));
    }

    [Fact]
    public async Task Alumno_inactivo_o_sin_inscripcion_activa_se_rechaza()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 4);
        var inactivo = _base.CrearAlumno(db, "331", activo: false);
        var suspendido = _base.CrearAlumno(db, "332", estadoInscripcionId: _base.EstadoSuspendidaId);

        var bajaPersona = await servicio.VincularAsync(grupoId, inactivo, CancellationToken.None);
        var sinActiva = await servicio.VincularAsync(grupoId, suspendido, CancellationToken.None);

        Assert.Equal(CodigosCupo.AlumnoInactivo, bajaPersona.Codigo);
        Assert.Contains("inactivo", bajaPersona.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(CodigosCupo.SinInscripcion, sinActiva.Codigo);
        Assert.Contains("inscripción activa", sinActiva.Mensaje, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);
    }

    [Fact]
    public async Task Grupo_cerrado_o_vencido_no_admite_alta()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var cerrado = _base.CrearGrupo(db, cupo: 4, estado: EstadosAsignacionAula.Cerrada);
        var vencido = _base.CrearGrupo(db, cupo: 4, finVigencia: DateTime.UtcNow.AddMinutes(-5));
        var alumnoCerrado = _base.CrearAlumno(db, "341");
        var alumnoVencido = _base.CrearAlumno(db, "342");

        Assert.Equal(CodigosCupo.GrupoCerrado, (await servicio.VincularAsync(cerrado, alumnoCerrado, CancellationToken.None)).Codigo);
        Assert.Equal(CodigosCupo.GrupoVencido, (await servicio.VincularAsync(vencido, alumnoVencido, CancellationToken.None)).Codigo);
    }

    [Fact]
    public async Task Baja_deja_el_vinculo_inactivo_y_libera_el_cupo()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 3);
        var alumnoId = _base.CrearAlumno(db, "351");
        Assert.True((await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None)).Exito);

        var baja = await servicio.DarBajaVinculoAsync(grupoId, alumnoId, CancellationToken.None);

        Assert.True(baja.Exito);
        var vinculo = await db.InscripcionesGrupo.AsNoTracking().SingleAsync();
        Assert.NotNull(vinculo.FechaBaja);
        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);

        var listado = await servicio.ListarInscriptosAsync(grupoId, CancellationToken.None);
        Assert.Empty(listado!.Alumnos);
    }

    [Fact]
    public async Task Baja_de_inscripcion_libera_los_cupos_de_sus_grupos()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoA = _base.CrearGrupo(db, cupo: 3);
        var grupoB = _base.CrearGrupo(db, cupo: 3);
        var alumnoId = _base.CrearAlumno(db, "361");
        Assert.True((await servicio.VincularAsync(grupoA, alumnoId, CancellationToken.None)).Exito);
        Assert.True((await servicio.VincularAsync(grupoB, alumnoId, CancellationToken.None)).Exito);

        var inscripcion = await db.Inscripciones.SingleAsync(i => i.PersonaId == alumnoId);
        inscripcion.EstadoId = _base.EstadoCanceladaId;
        await servicio.DarBajaVinculosDeInscripcionAsync(alumnoId, _base.MateriaId, CancellationToken.None);

        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoA)).CuposOcupados);
        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoB)).CuposOcupados);
        Assert.Equal(2, await db.InscripcionesGrupo.CountAsync(v => v.FechaBaja != null));
    }

    [Fact]
    public async Task Inactivar_a_la_persona_deja_de_ocupar_sin_borrar_el_vinculo()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 3);
        var alumnoId = _base.CrearAlumno(db, "371");
        Assert.True((await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None)).Exito);

        var persona = await db.Personas.SingleAsync(p => p.Id == alumnoId);
        persona.Activo = false;
        await servicio.RecalcularPorPersonaAsync(alumnoId, CancellationToken.None);

        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);
        Assert.Null((await db.InscripcionesGrupo.AsNoTracking().SingleAsync()).FechaBaja);

        var rechazo = await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None);
        Assert.Equal(CodigosCupo.AlumnoInactivo, rechazo.Codigo);
    }

    [Fact]
    public async Task Suspender_la_inscripcion_deja_de_ocupar_y_al_reactivarla_vuelve_a_ocupar()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 3);
        var alumnoId = _base.CrearAlumno(db, "381");
        Assert.True((await servicio.VincularAsync(grupoId, alumnoId, CancellationToken.None)).Exito);

        var inscripcion = await db.Inscripciones.SingleAsync(i => i.PersonaId == alumnoId);
        inscripcion.EstadoId = _base.EstadoSuspendidaId;
        await servicio.RecalcularPorPersonaYMateriaAsync(alumnoId, _base.MateriaId, CancellationToken.None);
        Assert.Equal(0, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);

        inscripcion.EstadoId = _base.EstadoActivaId;
        await servicio.RecalcularPorPersonaYMateriaAsync(alumnoId, _base.MateriaId, CancellationToken.None);
        Assert.Equal(1, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);
    }

    [Fact]
    public async Task Recalcular_concuerda_con_los_vinculos_que_ocupan_cupo()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 5);
        var activo = _base.CrearAlumno(db, "391");
        var inactivo = _base.CrearAlumno(db, "392");
        Assert.True((await servicio.VincularAsync(grupoId, activo, CancellationToken.None)).Exito);
        Assert.True((await servicio.VincularAsync(grupoId, inactivo, CancellationToken.None)).Exito);

        var persona = await db.Personas.SingleAsync(p => p.Id == inactivo);
        persona.Activo = false;
        var grupo = await db.AsignacionesAula.SingleAsync(a => a.Id == grupoId);
        grupo.CuposOcupados = 99;
        await db.SaveChangesAsync();

        await servicio.RecalcularTodosAsync(CancellationToken.None);

        var ocupantes = await db.InscripcionesGrupo.CountAsync(v =>
            v.AsignacionAulaId == grupoId &&
            v.FechaBaja == null &&
            v.Persona.Activo &&
            db.Inscripciones.Any(i =>
                i.PersonaId == v.PersonaId &&
                i.MateriaId == _base.MateriaId &&
                i.Estado.Nombre == EstadosInscripcionSistema.Activa));

        var actualizado = await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId);
        Assert.Equal(1, ocupantes);
        Assert.Equal(ocupantes, actualizado.CuposOcupados);
        Assert.Equal(4, ServicioCupoGrupo.CuposDisponibles(actualizado.CupoHabilitado, actualizado.CuposOcupados));
    }

    [Fact]
    public async Task Dos_altas_simultaneas_sobre_el_ultimo_cupo_dejan_una_sola()
    {
        await using var preparacion = _base.NuevoContexto();
        var grupoId = _base.CrearGrupo(preparacion, cupo: 1);
        var primero = _base.CrearAlumno(preparacion, "401");
        var segundo = _base.CrearAlumno(preparacion, "402");

        async Task<ResultadoOperacionCupo> Intentar(int personaId)
        {
            await using var db = _base.NuevoContexto();
            var servicio = _base.NuevoServicio(db);
            return await servicio.VincularAsync(grupoId, personaId, CancellationToken.None);
        }

        var resultados = await Task.WhenAll(Intentar(primero), Intentar(segundo));

        Assert.Equal(1, resultados.Count(r => r.Exito));
        Assert.Contains(resultados, r => r.Codigo == CodigosCupo.CupoCompleto);

        await using var verificacion = _base.NuevoContexto();
        var grupo = await verificacion.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId);
        Assert.Equal(1, grupo.CuposOcupados);
        Assert.Equal(1, await verificacion.InscripcionesGrupo.CountAsync(v => v.FechaBaja == null));
    }

    [Fact]
    public async Task Cerrar_el_grupo_conserva_vinculos_y_bloquea_altas_y_bajas()
    {
        await using var db = _base.NuevoContexto();
        var servicio = _base.NuevoServicio(db);
        var grupoId = _base.CrearGrupo(db, cupo: 3);
        var vinculado = _base.CrearAlumno(db, "411");
        var otro = _base.CrearAlumno(db, "412");
        Assert.True((await servicio.VincularAsync(grupoId, vinculado, CancellationToken.None)).Exito);

        var grupo = await db.AsignacionesAula.SingleAsync(a => a.Id == grupoId);
        grupo.Estado = EstadosAsignacionAula.Cerrada;
        grupo.FechaCierre = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var listado = await servicio.ListarInscriptosAsync(grupoId, CancellationToken.None);
        Assert.Single(listado!.Alumnos);
        Assert.False(listado.AdmiteAltas);
        Assert.Equal(CodigosCupo.GrupoCerrado, (await servicio.VincularAsync(grupoId, otro, CancellationToken.None)).Codigo);
        Assert.Equal(CodigosCupo.GrupoCerrado, (await servicio.DarBajaVinculoAsync(grupoId, vinculado, CancellationToken.None)).Codigo);
        Assert.Equal(1, (await db.AsignacionesAula.AsNoTracking().SingleAsync(a => a.Id == grupoId)).CuposOcupados);
        Assert.Null((await db.InscripcionesGrupo.AsNoTracking().SingleAsync(v => v.PersonaId == vinculado)).FechaBaja);
    }

    public void Dispose() => _base.Dispose();
}
