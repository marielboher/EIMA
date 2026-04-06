using Entidades;
using Microsoft.EntityFrameworkCore;

namespace AccesoDatos;

public class EimaDbContext : DbContext
{
    public EimaDbContext(DbContextOptions<EimaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<TipoColaborador> TiposColaborador => Set<TipoColaborador>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<ProfesorMateria> ProfesoresMaterias => Set<ProfesorMateria>();
    public DbSet<HorarioDisponible> HorariosDisponibles => Set<HorarioDisponible>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<HorarioAula> HorariosAula => Set<HorarioAula>();
    public DbSet<InscripcionMateria> InscripcionesMateria => Set<InscripcionMateria>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<CuentaUsuario> CuentasUsuarios => Set<CuentaUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.ToTable("Personas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Apellido).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Dni).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Telefono).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Direccion).HasMaxLength(300).IsRequired();
            entity.HasIndex(e => e.Dni).IsUnique();

            entity.Property(e => e.Colegio).HasMaxLength(200);
            entity.Property(e => e.GradoCurso).HasMaxLength(100);
            entity.Property(e => e.NivelEducativo).HasMaxLength(100);
            entity.Property(e => e.Especialidades).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(200);
            entity.Property(e => e.Salario).HasPrecision(18, 2);

            entity.HasOne(p => p.Rol)
                .WithMany(r => r.Personas)
                .HasForeignKey(p => p.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.TipoColaborador)
                .WithMany(t => t.Personas)
                .HasForeignKey(p => p.TipoColaboradorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CuentaUsuario>(entity =>
        {
            entity.ToTable("CuentasUsuario");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorreoElectronico).HasMaxLength(256).IsRequired();
            entity.Property(e => e.HashContrasena).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.CorreoElectronico).IsUnique();
            entity.HasIndex(e => e.PersonaId).IsUnique();
            entity.HasOne(e => e.Persona)
                .WithOne(p => p.CuentaUsuario)
                .HasForeignKey<CuentaUsuario>(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
        });

        modelBuilder.Entity<TipoColaborador>(entity =>
        {
            entity.ToTable("TiposColaborador");
            entity.Property(e => e.Tipo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasMaxLength(500);
        });

        modelBuilder.Entity<Materia>(entity =>
        {
            entity.ToTable("Materias");
            entity.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Area).HasMaxLength(150);
            entity.Property(e => e.Descripcion).HasMaxLength(2000);
            entity.Property(e => e.PrecioPorClase).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProfesorMateria>(entity =>
        {
            entity.ToTable("ProfesoresMaterias");
            entity.HasOne(e => e.Docente)
                .WithMany(p => p.ProfesoresMaterias)
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Materia)
                .WithMany(m => m.ProfesoresMaterias)
                .HasForeignKey(e => e.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HorarioDisponible>(entity =>
        {
            entity.ToTable("HorariosDisponibles");
            entity.Property(e => e.DiaSemana).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Docente)
                .WithMany(p => p.HorariosDisponibles)
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Aula>(entity =>
        {
            entity.ToTable("Aulas");
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Ubicacion).HasMaxLength(200);
        });

        modelBuilder.Entity<HorarioAula>(entity =>
        {
            entity.ToTable("HorariosAula");
            entity.Property(e => e.DiaSemana).HasMaxLength(50).IsRequired();
            entity.HasOne(e => e.Aula)
                .WithMany(a => a.HorariosAula)
                .HasForeignKey(e => e.AulaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InscripcionMateria>(entity =>
        {
            entity.ToTable("InscripcionesMateria");
            entity.Property(e => e.Estado).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MontoPagado).HasPrecision(18, 2);
            entity.HasOne(e => e.Persona)
                .WithMany(a => a.Inscripciones)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Materia)
                .WithMany(m => m.Inscripciones)
                .HasForeignKey(e => e.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pagos");
            entity.Property(e => e.Monto).HasPrecision(18, 2);
            entity.Property(e => e.MetodoPago).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Comprobante).HasMaxLength(200);
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            entity.HasOne(e => e.Persona)
                .WithMany(a => a.Pagos)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InscripcionMateria)
                .WithMany(i => i.Pagos)
                .HasForeignKey(e => e.InscripcionMateriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Clase>(entity =>
        {
            entity.ToTable("Clases");
            entity.Property(e => e.Estado).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            entity.HasOne(e => e.Materia)
                .WithMany(m => m.Clases)
                .HasForeignKey(e => e.MateriaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Docente)
                .WithMany(p => p.ClasesComoDocente)
                .HasForeignKey(e => e.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Aula)
                .WithMany(a => a.Clases)
                .HasForeignKey(e => e.AulaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Asistencia>(entity =>
        {
            entity.ToTable("Asistencias");
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.HasOne(e => e.Clase)
                .WithMany(c => c.Asistencias)
                .HasForeignKey(e => e.ClaseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Persona)
                .WithMany(a => a.Asistencias)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ClaseId, e.PersonaId }).IsUnique();
        });

        modelBuilder.Entity<Consulta>(entity =>
        {
            entity.ToTable("Consultas");
            entity.Property(e => e.Materia).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Asunto).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Mensaje).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Respuesta).HasMaxLength(4000);
            entity.HasOne(e => e.Persona)
                .WithMany(a => a.Consultas)
                .HasForeignKey(e => e.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
