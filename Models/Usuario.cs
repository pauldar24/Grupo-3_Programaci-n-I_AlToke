namespace GRUPAL.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contraseña { get; set; } = string.Empty;
    public int PublicacionesActivas { get; set; }
    public int HistoriasResueltas { get; set; }
    public int BuenasAcciones { get; set; }

    public ICollection<ObjetoPerdido> ObjetosPerdidos { get; set; } = new List<ObjetoPerdido>();
}