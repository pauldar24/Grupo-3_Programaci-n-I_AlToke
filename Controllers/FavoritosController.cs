using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class FavoritosController(ApplicationDbContext db, RecomendadorObjetos recomendador) : Controller
{
    public async Task<IActionResult> Index()
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue) return RedirectToAction("Ingresar", "Cuenta");
        var favoritos = await db.Favoritos.Where(x => x.UsuarioId == usuarioId.Value)
            .Include(x => x.ObjetoPerdido).ThenInclude(x => x.Usuario)
            .OrderByDescending(x => x.CreadoEn).Select(x => x.ObjetoPerdido).ToListAsync();
        ViewBag.Recomendados = await recomendador.RecomendarAsync(usuarioId);
        return View(favoritos);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Alternar(int id, string? volver)
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue) return RedirectToAction("Ingresar", "Cuenta");
        var objeto = await db.ObjetosPerdidos.FindAsync(id);
        if (objeto is null) return NotFound();
        var guardado = await db.Favoritos.FirstOrDefaultAsync(f => f.UsuarioId == usuarioId && f.ObjetoPerdidoId == id);
        if (guardado is null)
        {
            db.Favoritos.Add(new ObjetoFavorito { UsuarioId = usuarioId.Value, ObjetoPerdidoId = id });
        }
        else db.Favoritos.Remove(guardado);
        await db.SaveChangesAsync();
        return volver == "favoritos" ? RedirectToAction(nameof(Index)) : RedirectToAction("Detalle", "Explorar", new { id });
    }
}
