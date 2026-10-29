using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Services;

namespace GRUPAL.Controllers;

public class ExplorarController(ApplicationDbContext db, RecomendadorObjetos recomendador, AlgoliaObjetosService algolia) : Controller
{
    private const int PorPagina = 9;

    public async Task<IActionResult> Index(string? buscar, string? categoria, string? estado, int pagina = 1)
    {
        var query = db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLower();
            query = query.Where(o => o.Título.ToLower().Contains(term)
                || o.Descripcion.ToLower().Contains(term) || o.Ubicacion.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(o => o.Categoría == categoria);
        if (Enum.TryParse<EstadoObjeto>(estado, true, out var valor) && !string.IsNullOrWhiteSpace(estado))
            query = query.Where(o => o.Estado == valor);

        var total = await query.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, paginas);
        var objetos = await query.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id)
            .Skip((pagina - 1) * PorPagina).Take(PorPagina).ToListAsync();
        ViewBag.BuscarActual = buscar;
        ViewBag.CategoriaActual = categoria;
        ViewBag.EstadoActual = estado;
        ViewBag.TotalResultados = total;
        ViewBag.Pagina = pagina;
        ViewBag.TotalPaginas = paginas;
        return View(objetos);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var obj = await db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == id);
        if (obj is null) return NotFound();
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        ViewBag.EsFavorito = usuario.HasValue && await db.Favoritos.AnyAsync(f => f.UsuarioId == usuario && f.ObjetoPerdidoId == id);
        ViewBag.YaSolicito = usuario.HasValue && await db.SolicitudesRecuperacion.AnyAsync(s => s.SolicitanteId == usuario && s.ObjetoPerdidoId == id);
        ViewBag.Recomendados = await recomendador.RecomendarAsync(usuario, id, 3);
        return View(obj);
    }

    [HttpGet]
    public async Task<IActionResult> Sugerencias(string? termino)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Trim().Length < 2) return Json(Array.Empty<object>());
        var remoto = await algolia.BuscarAsync(termino.Trim(), HttpContext.RequestAborted);
        if (remoto != null) return Json(remoto);
        var valor = termino.Trim().ToLower();
        var datos = await db.ObjetosPerdidos.AsNoTracking()
            .Where(o => o.Título.ToLower().Contains(valor) || o.Categoría.ToLower().Contains(valor) || o.Ubicacion.ToLower().Contains(valor))
            .OrderByDescending(o => o.Fecha).Take(6)
            .Select(o => new { id = o.Id, titulo = o.Título, categoria = o.Categoría, ubicacion = o.Ubicacion })
            .ToListAsync();
        return Json(datos);
    }
}
