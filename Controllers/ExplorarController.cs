using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class ExplorarController(ApplicationDbContext db, RecomendadorObjetos recomendador) : Controller
{
    private const int PorPagina = 9;

    public async Task<IActionResult> Index(string? buscar, string? categoria, string? estado, int pagina = 1)
    {
        var query = db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim().ToLower();
            query = query.Where(o => o.Título.ToLower().Contains(term) || o.Descripcion.ToLower().Contains(term) || o.Ubicacion.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(categoria)) query = query.Where(o => o.Categoría == categoria);
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoObjeto>(estado, true, out var valor)) query = query.Where(o => o.Estado == valor);
        var total = await query.CountAsync();
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, paginas);
        var objetos = await query.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id).Skip((pagina - 1) * PorPagina).Take(PorPagina).ToListAsync();
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
        var objeto = await db.ObjetosPerdidos.AsNoTracking().Include(o => o.Usuario).FirstOrDefaultAsync(o => o.Id == id);
        if (objeto is null) return NotFound();
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        ViewBag.YaSolicito = usuarioId.HasValue && await db.SolicitudesRecuperacion.AnyAsync(r => r.SolicitanteId == usuarioId.Value && r.ObjetoPerdidoId == id);
        ViewBag.EsFavorito = usuarioId.HasValue && await db.Favoritos.AnyAsync(f => f.UsuarioId == usuarioId.Value && f.ObjetoPerdidoId == id);
        ViewBag.Recomendados = await recomendador.RecomendarAsync(usuarioId, id, 3);
        return View(objeto);
    }

    [HttpGet]
    public async Task<IActionResult> Sugerencias(string? termino)
    {
        if (string.IsNullOrWhiteSpace(termino) || termino.Trim().Length < 2) return Json(Array.Empty<object>());
        var valor = termino.Trim().ToLower();
        var datos = await db.ObjetosPerdidos.AsNoTracking()
            .Where(o => o.Título.ToLower().Contains(valor) || o.Categoría.ToLower().Contains(valor) || o.Ubicacion.ToLower().Contains(valor))
            .OrderByDescending(o => o.Fecha).Take(6)
            .Select(o => new { id = o.Id, titulo = o.Título, categoria = o.Categoría, ubicacion = o.Ubicacion })
            .ToListAsync();
        return Json(datos);
    }
}