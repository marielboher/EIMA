namespace Controladores;

public sealed record MateriaListadoDto(
    int Id,
    string Nombre,
    string? Area,
    string? Descripcion,
    int DuracionHoras,
    decimal PrecioPorClase,
    bool Activa);

public sealed class GuardarMateriaDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Descripcion { get; set; }
    public int DuracionHoras { get; set; }
    public decimal PrecioPorClase { get; set; }
    public bool Activa { get; set; } = true;
}
