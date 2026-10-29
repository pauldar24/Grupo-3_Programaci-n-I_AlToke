using System.ComponentModel.DataAnnotations;
namespace GRUPAL.Models;

public enum EstadoSolicitud { Pendiente, Aceptada, Rechazada }

public class SolicitudRecuperacion
{
    public int Id { get; set; }
    public int ObjetoPerdidoId { get; set; }
    public ObjetoPerdido ObjetoPerdido { get; set; } = null!;
    public int SolicitanteId { get; set; }
    public Usuario Solicitante { get; set; } = null!;
    [Required, StringLength(1000, MinimumLength = 10)] public string Mensaje { get; set; } = string.Empty;
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;
    [StringLength(1000)] public string? Respuesta { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
