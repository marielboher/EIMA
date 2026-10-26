using AccesoDatos;
using Controladores;
using Entidades;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Eima.Pruebas;

public sealed class BaseCupo : IDisposable
{
    private readonly SqliteConnection _ancla;
    private readonly string _cadena;
    private readonly IBloqueoFilaGrupo _bloqueo = new BloqueoFilaGrupo();
    private readonly DbContextOptions<EimaDbContext> _opciones;

    public int MateriaId { get; private set; }
    public int EstadoActivaId { get; private set; }
    public int EstadoCanceladaId { get; private set; }
    public int EstadoSuspendidaId { get; private set; }
    public int RolAlumnoId { get; private set; }

    public BaseCupo()
    {
        _cadena = $"Data Source=file:cupo-{Guid.NewGuid():N}?mode=memory&cache=shared";
        _ancla = new SqliteConnection(_cadena);
        _ancla.Open();
        _opciones = new DbContextOptionsBuilder<EimaDbContext>().UseSqlite(_cadena).Options;

        using var db = NuevoContexto();
        db.Database.EnsureCreated();
        Sembrar(db);
    }

    public EimaDbContext NuevoContexto() => new(_opciones);

    public ServicioCupoGrupo NuevoServicio(EimaDbContext db) => new(db, _bloqueo);

    public int CrearGrupo(EimaDbContext db, int cupo, string estado = EstadosAsignacionAula.Abierta, DateTime? finVigencia = null)
    {
        var grupo = new AsignacionAula
        {
            AulaId = db.Aulas.Select(a => a.Id).First(),
            MateriaId = MateriaId,
            DocenteId = db.Personas.Where(p => p.Rol.Nombre == RolesSistema.Profesor).Select(p => p.Id).First(),
            Estado = estado,
            CupoHabilitado = cupo,
            CuposOcupados = 0,
            FechaAsignacion = DateTime.UtcNow,
            FinVigencia = finVigencia ?? DateTime.UtcNow.AddDays(30)
        };
        db.AsignacionesAula.Add(grupo);
        db.SaveChanges();
        return grupo.Id;
    }

    public int CrearAlumno(EimaDbContext db, string dni, bool activo = true, bool conInscripcion = true, int? estadoInscripcionId = null)
    {
        var alumno = new Persona
        {
            Nombre = "Alumno",
            Apellido = dni,
            Dni = dni,
            Telefono = "111",
            Direccion = "Calle 1",
            FechaRegistro = DateTime.UtcNow,
            RolId = RolAlumnoId,
            Activo = activo
        };
        db.Personas.Add(alumno);
        db.SaveChanges();

        if (conInscripcion)
        {
            db.Inscripciones.Add(new Inscripciones
            {
                PersonaId = alumno.Id,
                MateriaId = MateriaId,
                FechaInscripcion = DateTime.UtcNow,
                ClasesTotales = 8,
                ClasesTomadas = 0,
                EstadoId = estadoInscripcionId ?? EstadoActivaId,
                MontoPagado = 0
            });
            db.SaveChanges();
        }

        return alumno.Id;
    }

    public void Dispose() => _ancla.Dispose();

    private void Sembrar(EimaDbContext db)
    {
        var alumno = new Rol { Nombre = RolesSistema.Alumno };
        var profesor = new Rol { Nombre = RolesSistema.Profesor };
        db.Roles.AddRange(alumno, profesor);

        var activa = new EstadoInscripcion { Nombre = EstadosInscripcionSistema.Activa };
        var cancelada = new EstadoInscripcion { Nombre = EstadosInscripcionSistema.Cancelada };
        var suspendida = new EstadoInscripcion { Nombre = EstadosInscripcionSistema.Suspendida };
        db.EstadosInscripcion.AddRange(activa, cancelada, suspendida);

        var materia = new Materia { Nombre = "Matemática", Activa = true, DuracionHoras = 10, PrecioPorClase = 1 };
        db.Materias.Add(materia);
        db.Aulas.Add(new Aula { Nombre = "Aula 1", Capacidad = 20, Activa = true });
        db.SaveChanges();

        db.Personas.Add(new Persona
        {
            Nombre = "Docente",
            Apellido = "Uno",
            Dni = "100",
            Telefono = "111",
            Direccion = "Calle 1",
            FechaRegistro = DateTime.UtcNow,
            RolId = profesor.Id,
            Activo = true
        });
        db.SaveChanges();

        MateriaId = materia.Id;
        EstadoActivaId = activa.Id;
        EstadoCanceladaId = cancelada.Id;
        EstadoSuspendidaId = suspendida.Id;
        RolAlumnoId = alumno.Id;
    }
}
