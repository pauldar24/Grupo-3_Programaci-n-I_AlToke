using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class RecuperacionesController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Pendientes()
    {
        Response.Headers.CacheControl = "no-store";
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuario.HasValue) return Unauthorized();
        var cantidad = await db.SolicitudesRecuperacion.CountAsync(s =>
            (s.ObjetoPerdido.UsuarioId == usuario && s.Estado == EstadoSolicitud.Pendiente)
            || (s.SolicitanteId == usuario && s.Estado == EstadoSolicitud.Aceptada));
        return Json(new { cantidad });
    }

    public async Task<IActionResult> Index()
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var solicitudes = await db.SolicitudesRecuperacion.Include(s => s.ObjetoPerdido)
            .Include(s => s.Solicitante).Where(s => s.SolicitanteId == usuario || s.ObjetoPerdido.UsuarioId == usuario)
            .OrderByDescending(s => s.CreadoEn).ToListAsync();
        ViewBag.UsuarioId = usuario.Value;
        return View(solicitudes);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(int id, string mensaje)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var objeto = await db.ObjetosPerdidos.FindAsync(id);
        if (objeto is null) return NotFound();
        if (objeto.UsuarioId == usuario || objeto.Estado != EstadoObjeto.Perdido) return BadRequest("No se puede solicitar este objeto.");
        if (string.IsNullOrWhiteSpace(mensaje) || mensaje.Trim().Length < 10 || mensaje.Length > 1000)
        {
            TempData["Error"] = "Explica tu solicitud entre 10 y 1000 caracteres.";
            return RedirectToAction("Detalle", "Explorar", new { id });
        }
        if (await db.SolicitudesRecuperacion.AnyAsync(s => s.ObjetoPerdidoId == id && s.SolicitanteId == usuario))
        {
            TempData["Error"] = "Ya enviaste una solicitud para este aviso.";
            return RedirectToAction("Detalle", "Explorar", new { id });
        }
        db.SolicitudesRecuperacion.Add(new SolicitudRecuperacion { ObjetoPerdidoId = id, SolicitanteId = usuario.Value, Mensaje = mensaje.Trim() });
        await db.SaveChangesAsync();
        TempData["Exito"] = "Solicitud enviada al autor. Puedes seguir su estado en Recuperaciones.";
        return RedirectToAction("Detalle", "Explorar", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolver(int id, EstadoSolicitud estado, string? respuesta)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var solicitud = await db.SolicitudesRecuperacion.Include(s => s.ObjetoPerdido).FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud is null) return NotFound();
        if (solicitud.ObjetoPerdido.UsuarioId != usuario) return Forbid();
        if (solicitud.Estado != EstadoSolicitud.Pendiente || estado is not (EstadoSolicitud.Aceptada or EstadoSolicitud.Rechazada))
            return BadRequest("Cambio de estado no permitido.");
        if (estado == EstadoSolicitud.Aceptada && (string.IsNullOrWhiteSpace(respuesta) || respuesta.Length > 1000))
        {
            TempData["Error"] = "Indica cómo coordinar la entrega (máximo 1000 caracteres).";
            return RedirectToAction(nameof(Index));
        }
        if (estado == EstadoSolicitud.Aceptada && await db.SolicitudesRecuperacion.AnyAsync(s =>
            s.ObjetoPerdidoId == solicitud.ObjetoPerdidoId && s.Estado == EstadoSolicitud.Aceptada && s.Id != id))
        {
            TempData["Error"] = "Ya existe una solicitud aceptada para este objeto.";
            return RedirectToAction(nameof(Index));
        }
        solicitud.Estado = estado;
        solicitud.Respuesta = string.IsNullOrWhiteSpace(respuesta) ? null : respuesta.Trim();
        // Una aceptación no implica entrega completada: el propietario decide cuándo cerrar el aviso.
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
