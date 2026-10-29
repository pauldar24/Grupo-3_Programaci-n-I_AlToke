using GRUPAL.Data;
using GRUPAL.Filters;
using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

[TypeFilter(typeof(AdminAuthFilter))]
public class ReportesController(ApplicationDbContext db, SentimientoService sentimiento) : Controller
{
    public async Task<IActionResult> Contactos(string? filtro, DateTime? fecha)
    {
        var consulta = db.MensajesContacto.AsNoTracking().AsQueryable();
        if (filtro == "Pendiente") consulta = consulta.Where(x => !x.Atendido);
        if (filtro == "Atendido") consulta = consulta.Where(x => x.Atendido);
        if (fecha.HasValue) consulta = consulta.Where(x => x.CreadoEn.Date == fecha.Value.Date);
        ViewBag.Filtro = filtro;
        ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");
        return View(await consulta.OrderByDescending(x => x.CreadoEn).Take(250).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reentrenar()
    {
        TempData["Mensaje"] = await sentimiento.ReentrenarAsync()
            ? "Modelo ML.NET reentrenado correctamente." : "No se pudo reentrenar el modelo. Revisa la configuración.";
        return RedirectToAction(nameof(Contactos));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarAtendido(int id, bool atendido)
    {
        var contacto = await db.MensajesContacto.FindAsync(id);
        if (contacto is null) return NotFound();
        contacto.Atendido = atendido;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Contactos));
    }
}
