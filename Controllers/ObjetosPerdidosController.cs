using Microsoft.AspNetCore.Mvc;
using GRUPAL.Data;
using GRUPAL.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace GRUPAL.Controllers;

public class ObjetosPerdidosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly Cloudinary _cloudinary;

    public ObjetosPerdidosController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;

        var cloud = config.GetSection("Cloudinary");
        var account = new Account(cloud["CloudName"], cloud["ApiKey"], cloud["ApiSecret"]);
        _cloudinary = new Cloudinary(account);
    }

    // GET: ObjetosPerdidos/Create
    public IActionResult Create()
    {
        if (!HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Ingresar", "Cuenta");
            
        return View();
    }

    // POST: ObjetosPerdidos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjetoPerdido objeto, IFormFile? foto)
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
            return RedirectToAction("Ingresar", "Cuenta");

        // Remover validaciones de navegación que no vienen del formulario
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");

        if (!ModelState.IsValid)
        {
            return View(objeto);
        }

        objeto.UsuarioId = usuarioId.Value;
        objeto.Estado = EstadoObjeto.Perdido;
        objeto.Ubicacion = "No especificada";

        // Subir foto a Cloudinary
        if (foto != null && foto.Length > 0)
        {
            using var stream = foto.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(foto.FileName, stream),
                Folder = "altoke/objetos",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                objeto.FotoUrl = result.SecureUrl.ToString();
            }
        }

        _context.ObjetosPerdidos.Add(objeto);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
}
