namespace Entidades;

/// <summary>Consulta genérica sin vínculo a una persona (contacto / soporte / mesa de ayuda).</summary>
public class Consulta
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    /// <summary>Texto libre con la materia o tema de la consulta.</summary>
    public string Materia { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public DateTime FechaConsulta { get; set; }

    /// <summary><see langword="true"/> si la consulta ya fue respondida; <see langword="false"/> si sigue pendiente.</summary>
    public bool Estado { get; set; }
}
