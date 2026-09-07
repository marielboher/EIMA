using System.ComponentModel.DataAnnotations;

namespace Controladores;

public class GuardarMateriaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Area { get; set; }

    [MaxLength(2000)]
    public string? Descripcion { get; set; }

    [Range(0, int.MaxValue)]
    public int DuracionHoras { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecioPorClase { get; set; }

    public bool Activa { get; set; } = true;
}

public class MateriaListadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Descripcion { get; set; }
    public int DuracionHoras { get; set; }
    public decimal PrecioPorClase { get; set; }
    public bool Activa { get; set; }
}
