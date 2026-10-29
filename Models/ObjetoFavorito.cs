namespace GRUPAL.Models;

public class ObjetoFavorito
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int ObjetoPerdidoId { get; set; }
    public ObjetoPerdido ObjetoPerdido { get; set; } = null!;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
