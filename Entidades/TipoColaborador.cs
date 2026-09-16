using System.Text.Json.Serialization;

namespace Entidades;

public class TipoColaborador
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    [JsonIgnore]
    public ICollection<Persona> Personas { get; set; } = new List<Persona>();
}
