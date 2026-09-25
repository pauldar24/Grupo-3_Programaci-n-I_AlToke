using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using GRUPAL.Filters;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Text.RegularExpressions;

namespace GRUPAL.Controllers;

[TypeFilter(typeof(AdminAuthFilter))]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly Cloudinary _cloudinary;

    public AdminController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env)
    {
        _context = context;
        _config = config;
        _env = env;

        var cloud = config.GetSection("Cloudinary");
        var account = new Account(cloud["CloudName"], cloud["ApiKey"], cloud["ApiSecret"]);
        _cloudinary = new Cloudinary(account);
    }

    // ═══════════════════════════════════════════════════════════
    //  LOGIN / LOGOUT
    // ═══════════════════════════════════════════════════════════

    [SkipAdminAuth]
    public IActionResult Login()
    {
        // If already authenticated, go straight to dashboard
        if (HttpContext.Session.GetString("IsAdmin") == "true")
            return RedirectToAction(nameof(Index));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SkipAdminAuth]
    public IActionResult Login(string password)
    {
        var adminPassword = _config["AdminSettings:Password"];
        if (password == adminPassword)
        {
            HttpContext.Session.SetString("IsAdmin", "true");
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Error = "Contraseña incorrecta. Inténtalo de nuevo.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // ═══════════════════════════════════════════════════════════
    //  DASHBOARD
    // ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    //  OBJETOS
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> Objetos()
    {
        var objetos = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .ToListAsync();
        return View(objetos);
    }

    // ═══════════════════════════════════════════════════════════
    //  USUARIOS
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
        return View(usuarios);
    }

    // ═══════════════════════════════════════════════════════════
    //  EDITAR OBJETO
    // ═══════════════════════════════════════════════════════════

    public async Task<IActionResult> EditarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();
        return View(objeto);
    }

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

    // ═══════════════════════════════════════════════════════════
    //  ELIMINAR OBJETO
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarObjeto(int id)
    {
        var objeto = await _context.ObjetosPerdidos.FindAsync(id);
        if (objeto == null) return NotFound();

        // Limpiar foto (Cloudinary o local)
        await EliminarFotoAsync(objeto.FotoUrl);

        _context.ObjetosPerdidos.Remove(objeto);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Objetos));
    }

    // ═══════════════════════════════════════════════════════════
    //  CAMBIAR ESTADO
    // ═══════════════════════════════════════════════════════════

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

    // ═══════════════════════════════════════════════════════════
    //  ELIMINAR USUARIO (con limpieza de fotos)
    // ═══════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario == null) return NotFound();

        // Limpiar fotos de todos los objetos del usuario antes del cascade delete
        if (usuario.ObjetosPerdidos != null)
        {
            foreach (var obj in usuario.ObjetosPerdidos)
            {
                await EliminarFotoAsync(obj.FotoUrl);
            }
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Usuarios));
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPER: Eliminar foto (Cloudinary o disco local)
    // ═══════════════════════════════════════════════════════════

    private async Task EliminarFotoAsync(string? fotoUrl)
    {
        if (string.IsNullOrEmpty(fotoUrl)) return;

        if (fotoUrl.Contains("cloudinary", StringComparison.OrdinalIgnoreCase))
        {
            // Extraer public_id de la URL de Cloudinary
            var match = Regex.Match(fotoUrl, @"/upload/(?:v\d+/)?(.+)\.\w+$");
            if (match.Success)
            {
                var publicId = match.Groups[1].Value;
                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            }
        }
        else if (fotoUrl.StartsWith("/uploads/"))
        {
            // Archivo local (legacy) — intentar eliminar
            var filePath = Path.Combine(_env.WebRootPath, fotoUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
