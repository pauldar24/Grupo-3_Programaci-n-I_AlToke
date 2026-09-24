using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;

namespace GRUPAL.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: Admin - Dashboard
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalObjetos = await _context.ObjetosPerdidos.CountAsync();
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.ObjetosPerdidos = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Perdido);
        ViewBag.ObjetosEncontrados = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Encontrado);

        var recientes = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .Take(5)
            .ToListAsync();

        return View(recientes);
    }

    // GET: Admin/Objetos
    public async Task<IActionResult> Objetos()
    {
        var objetos = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();
        return View(objetos);
    }

    // GET: Admin/Usuarios
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
        return View(usuarios);
    }

    // GET: Admin/EditarObjeto/5
    public async Task<IActionResult> EditarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();
        return View(objeto);
    }

    // POST: Admin/EditarObjeto
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarObjeto(ObjetoPerdido objeto)
    {
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");

        var objetoDb = await _context.ObjetosPerdidos.FindAsync(objeto.Id);
        if (objetoDb == null) return NotFound();

        objetoDb.Título = objeto.Título;
        objetoDb.Categoría = objeto.Categoría;
        objetoDb.Descripcion = objeto.Descripcion;
        objetoDb.Fecha = objeto.Fecha;
        objetoDb.Estado = objeto.Estado;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // POST: Admin/EliminarObjeto/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();

        // Delete photo file if exists
        if (!string.IsNullOrEmpty(objeto.FotoUrl))
        {
            var filePath = Path.Combine(_env.WebRootPath, objeto.FotoUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        _context.ObjetosPerdidos.Remove(objeto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // POST: Admin/CambiarEstado/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, EstadoObjeto nuevoEstado)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();

        objeto.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // POST: Admin/EliminarUsuario/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Usuarios));
    }
}
