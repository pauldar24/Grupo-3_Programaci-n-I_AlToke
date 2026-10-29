using Microsoft.AspNetCore.Mvc;
using GRUPAL.Data;
using GRUPAL.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;

namespace GRUPAL.Controllers;

public class ObjetosPerdidosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ObjetosPerdidosController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env)
    {
        _context = context;
        _config = config;
        _env = env;
    }

    public IActionResult Create()
    {
        if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Ingresar", "Cuenta");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjetoPerdido objeto, IFormFile? foto)
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue) return RedirectToAction("Ingresar", "Cuenta");
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");
        if (string.IsNullOrWhiteSpace(objeto.Título) || objeto.Título.Length > 150 ||
            string.IsNullOrWhiteSpace(objeto.Descripcion) || objeto.Descripcion.Length > 3000 ||
            string.IsNullOrWhiteSpace(objeto.Categoría) || objeto.Categoría.Length > 100 ||
            objeto.Fecha == default)
        {
            ModelState.AddModelError("", "Completa los datos del reporte con información válida.");
            return View(objeto);
        }
        if (!ModelState.IsValid) return View(objeto);
        objeto.Id = 0;
        objeto.UsuarioId = usuarioId.Value;
        objeto.Estado = EstadoObjeto.Perdido;
        objeto.Ubicacion = string.IsNullOrWhiteSpace(objeto.Ubicacion) ? "No especificada" : objeto.Ubicacion.Trim();
        if (objeto.Ubicacion.Length > 200) return BadRequest("Ubicación demasiado larga.");
        objeto.FotoUrl = null;
        if (foto is { Length: > 0 })
        {
            if (foto.Length > 5 * 1024 * 1024 ||
                foto.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
            {
                ModelState.AddModelError("", "La foto debe ser JPG, PNG o WebP, de hasta 5 MB.");
                return View(objeto);
            }
            var cloud = _config.GetSection("Cloudinary");
            if (!string.IsNullOrWhiteSpace(cloud["CloudName"]) && !string.IsNullOrWhiteSpace(cloud["ApiKey"])
                && !string.IsNullOrWhiteSpace(cloud["ApiSecret"]))
            {
                var client = new Cloudinary(new Account(cloud["CloudName"], cloud["ApiKey"], cloud["ApiSecret"]));
                using var stream = foto.OpenReadStream();
                var result = await client.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(Path.GetFileName(foto.FileName), stream),
                    Folder = "altoke/objetos",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                });
                if (result.StatusCode == System.Net.HttpStatusCode.OK) objeto.FotoUrl = result.SecureUrl.ToString();
                else ModelState.AddModelError("", "No se pudo subir la fotografía.");
            }
            else
            {
                var extension = foto.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
                var directorio = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(directorio);
                var nombre = Guid.NewGuid().ToString("N") + extension;
                await using var archivo = System.IO.File.Create(Path.Combine(directorio, nombre));
                await foto.CopyToAsync(archivo);
                objeto.FotoUrl = "/uploads/" + nombre;
            }
            if (!ModelState.IsValid) return View(objeto);
        }
        _context.ObjetosPerdidos.Add(objeto);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Editar(int id)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var obj = await _context.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (obj.UsuarioId != usuario) return Forbid();
        return View(obj);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ObjetoPerdido datos)
    {
        var usuario = HttpContext.Session.GetInt32("UsuarioId");
        if (usuario is null) return RedirectToAction("Ingresar", "Cuenta");
        var obj = await _context.ObjetosPerdidos.FindAsync(id);
        if (obj is null) return NotFound();
        if (obj.UsuarioId != usuario) return Forbid();
        if (string.IsNullOrWhiteSpace(datos.Título) || datos.Título.Length > 150 ||
            string.IsNullOrWhiteSpace(datos.Descripcion) || datos.Descripcion.Length > 3000 ||
            string.IsNullOrWhiteSpace(datos.Categoría) || datos.Categoría.Length > 100 ||
            string.IsNullOrWhiteSpace(datos.Ubicacion) || datos.Ubicacion.Length > 200)
        {
            ModelState.AddModelError("", "Completa correctamente los campos.");
            return View(obj);
        }
        obj.Título = datos.Título.Trim();
        obj.Categoría = datos.Categoría.Trim();
        obj.Descripcion = datos.Descripcion.Trim();
        obj.Ubicacion = datos.Ubicacion.Trim();
        obj.Fecha = datos.Fecha == default ? obj.Fecha : datos.Fecha;
        await _context.SaveChangesAsync();
        return RedirectToAction("Detalle", "Explorar", new { id });
    }
}
