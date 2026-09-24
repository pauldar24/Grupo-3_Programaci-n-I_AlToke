using System.ComponentModel.DataAnnotations;

namespace GRUPAL.Models;

public class ObjetoPerdido
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [Display(Name = "Título")]
    public string Título { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public string Categoría { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe el objeto.")]
    [Display(Name = "Descripción detallada")]
    public string Descripcion { get; set; } = string.Empty;

    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; }

    public string Ubicacion { get; set; } = string.Empty;
    public EstadoObjeto Estado { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}