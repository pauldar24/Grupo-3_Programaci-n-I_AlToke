using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models;

public class ArticuloAyuda
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Pregunta { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Respuesta { get; set; } = string.Empty;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
