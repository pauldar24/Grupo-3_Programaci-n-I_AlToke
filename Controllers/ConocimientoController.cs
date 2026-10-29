using GRUPAL.Data;
using GRUPAL.Filters;
using GRUPAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

[TypeFilter(typeof(AdminAuthFilter))]
public class ConocimientoController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await db.ArticulosAyuda.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ArticuloAyuda articulo)
    {
        if (string.IsNullOrWhiteSpace(articulo.Pregunta) || string.IsNullOrWhiteSpace(articulo.Respuesta)
            || articulo.Pregunta.Length > 160 || articulo.Respuesta.Length > 4000)
        {
            TempData["Error"] = "Completa la pregunta y la respuesta dentro de los límites permitidos.";
            return RedirectToAction(nameof(Index));
        }
        if (articulo.Id > 0)
        {
            var actual = await db.ArticulosAyuda.FindAsync(articulo.Id);
            if (actual is null) return NotFound();
            actual.Pregunta = articulo.Pregunta.Trim();
            actual.Respuesta = articulo.Respuesta.Trim();
        }
        else db.ArticulosAyuda.Add(new ArticuloAyuda { Pregunta = articulo.Pregunta.Trim(), Respuesta = articulo.Respuesta.Trim() });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CargarTexto(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0 || archivo.Length > 256 * 1024 ||
            Path.GetExtension(archivo.FileName).ToLowerInvariant() is not (".txt" or ".md"))
        {
            TempData["Error"] = "Sube un archivo .txt o .md de hasta 256 KB.";
            return RedirectToAction(nameof(Index));
        }
        using var lector = new StreamReader(archivo.OpenReadStream());
        var contenido = await lector.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(contenido))
        {
            TempData["Error"] = "El archivo está vacío.";
            return RedirectToAction(nameof(Index));
        }
        // Sin vectorización externa: fragmentos de texto locales consultables por el asistente.
        var nombre = Path.GetFileName(archivo.FileName);
        foreach (var (parte, indice) in contenido.Chunk(3500).Select((c, i) => (new string(c), i)))
        {
            db.ArticulosAyuda.Add(new ArticuloAyuda
            {
                Pregunta = $"{nombre[..Math.Min(nombre.Length, 115)]} · parte {indice + 1}",
                Respuesta = parte
            });
        }
        await db.SaveChangesAsync();
        TempData["Exito"] = "Documento añadido a la base local de ayuda.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var actual = await db.ArticulosAyuda.FindAsync(id);
        if (actual is null) return NotFound();
        db.ArticulosAyuda.Remove(actual);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
