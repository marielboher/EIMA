namespace Controladores;

public static class CodigosCupo
{
    public const string CupoCompleto = "cupo_completo";
    public const string Duplicado = "duplicado";
    public const string AlumnoInactivo = "alumno_inactivo";
    public const string SinInscripcion = "sin_inscripcion";
    public const string GrupoCerrado = "grupo_cerrado";
    public const string GrupoVencido = "grupo_vencido";
    public const string NoEncontrado = "no_encontrado";
    public const string AlumnoInexistente = "alumno_inexistente";
    public const string SinVinculo = "sin_vinculo";
}

public sealed class ResultadoOperacionCupo
{
    public bool Exito { get; init; }
    public string? Codigo { get; init; }
    public string? Mensaje { get; init; }
    public AlumnoGrupoDto? Alumno { get; init; }

    public static ResultadoOperacionCupo Ok(AlumnoGrupoDto? alumno = null) =>
        new() { Exito = true, Alumno = alumno };

    public static ResultadoOperacionCupo Fallo(string codigo, string mensaje) =>
        new() { Exito = false, Codigo = codigo, Mensaje = mensaje };
}

public class AlumnoGrupoDto
{
    public int VinculoId { get; set; }
    public int PersonaId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; }
}

public class CandidatoGrupoDto
{
    public int PersonaId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
}

public class ListadoAlumnosGrupoDto
{
    public int AsignacionAulaId { get; set; }
    public int CupoHabilitado { get; set; }
    public int CuposOcupados { get; set; }
    public int CuposDisponibles { get; set; }
    public bool CupoCompleto { get; set; }
    public bool AdmiteAltas { get; set; }
    public string? MotivoNoAdmiteAltas { get; set; }
    public IReadOnlyList<AlumnoGrupoDto> Alumnos { get; set; } = Array.Empty<AlumnoGrupoDto>();
}
