namespace GRUPAL.Models;

public class ObjetoPerdido
{
    public int Id { get; set; }
    public string Título { get; set; } = string.Empty;
    public string Categoría { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public EstadoObjeto Estado { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}