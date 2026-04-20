using System.ComponentModel.DataAnnotations;

namespace Controladores;

/// <summary>Alta de consulta desde el sitio público (formulario de contacto).</summary>
public sealed class CrearConsultaPublicaSolicitud
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El apellido no puede superar los 150 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [MaxLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El teléfono no puede superar los 50 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Elegí un área y una materia del catálogo.")]
    public int MateriaId { get; set; }

    [Required(ErrorMessage = "Elegí un asunto de la lista.")]
    [MaxLength(200, ErrorMessage = "El asunto no puede superar los 200 caracteres.")]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [MaxLength(4000, ErrorMessage = "El mensaje no puede superar los 4000 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;
}
