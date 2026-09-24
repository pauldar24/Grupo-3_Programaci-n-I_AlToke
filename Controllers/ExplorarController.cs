using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;

namespace GRUPAL.Controllers;

public class ExplorarController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExplorarController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Explorar
    public async Task<IActionResult> Index(string? buscar, string? categoria, string? estado)
    {
        // Consulta base: todos los objetos con su usuario, ordenados por fecha
        var query = _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .AsQueryable();

        // Filtro por búsqueda de texto (título o descripción)
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.ToLower();
            query = query.Where(o =>
                o.Título.ToLower().Contains(term) ||
                o.Descripcion.ToLower().Contains(term));
        }

        // Filtro por categoría
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            query = query.Where(o => o.Categoría == categoria);
        }

        // Filtro por estado
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (Enum.TryParse<EstadoObjeto>(estado, out var estadoEnum))
            {
                query = query.Where(o => o.Estado == estadoEnum);
            }
        }

        var objetos = await query
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();

        // Pasar los filtros actuales a la vista para mantener el estado
        ViewBag.BuscarActual = buscar;
        ViewBag.CategoriaActual = categoria;
        ViewBag.EstadoActual = estado;
        ViewBag.TotalResultados = objetos.Count;

        return View(objetos);
    }
}
