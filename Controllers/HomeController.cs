using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Models;
using GRUPAL.Data;
using GRUPAL.Services;

namespace GRUPAL.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly SentimientoService _sentimiento;

    public HomeController(ApplicationDbContext context, SentimientoService sentimiento)
    {
        _context = context;
        _sentimiento = sentimiento;
    }

    public async Task<IActionResult> Index()
    {
        var objetosRecientes = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .Take(6)
            .ToListAsync();

        // Stats
        ViewBag.TotalObjetos = await _context.ObjetosPerdidos.CountAsync();
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.ObjetosEncontrados = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Encontrado);

        return View(objetosRecientes);
    }

    public IActionResult ComoFunciona()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contacto() => View(new MensajeContacto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contacto(MensajeContacto contacto)
    {
        if (!ModelState.IsValid) return View(contacto);
        contacto.Id = 0;
        contacto.Nombre = contacto.Nombre.Trim();
        contacto.Correo = contacto.Correo.Trim();
        contacto.Mensaje = contacto.Mensaje.Trim();
        var (etiqueta, _) = await _sentimiento.EvaluarAsync(contacto.Asunto + ". " + contacto.Mensaje);
        contacto.Sentimiento = etiqueta;
        _context.MensajesContacto.Add(contacto);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "¡Gracias por escribirnos! Tu mensaje fue recibido.";
        return RedirectToAction(nameof(Contacto));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
