using Microsoft.AspNetCore.Mvc;
using GRUPAL.Data;
using GRUPAL.Models;

namespace GRUPAL.Controllers;

public class ObjetosPerdidosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ObjetosPerdidosController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: ObjetosPerdidos/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ObjetosPerdidos/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjetoPerdido objeto, IFormFile? foto)
    {
        // Remover validaciones de navegación que no vienen del formulario
        ModelState.Remove("Usuario");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("FotoUrl");

        if (!ModelState.IsValid)
        {
            return View(objeto);
        }

        // --- Usuario hardcodeado temporalmente (sin Login aún) ---
        // Asegurarse de que exista un usuario con Id = 1 en la BD
        var usuarioExiste = await _context.Usuarios.FindAsync(1);
        if (usuarioExiste == null)
        {
            // Crear usuario temporal de prueba
            var usuarioTemporal = new Usuario
            {
                Nombre = "Usuario Temporal",
                Correo = "temp@altoke.pe",
                Contraseña = "temporal123"
            };
            _context.Usuarios.Add(usuarioTemporal);
            await _context.SaveChangesAsync();
        }

        objeto.UsuarioId = 1;
        objeto.Estado = EstadoObjeto.Perdido;
        objeto.Ubicacion = "No especificada"; // Campo requerido en BD, se llenará cuando se implemente

        // Procesar la foto si se subió una
        if (foto != null && foto.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder); // Crear carpeta si no existe

            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(foto.FileName);
            var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            objeto.FotoUrl = "/uploads/" + nombreArchivo;
        }

        _context.ObjetosPerdidos.Add(objeto);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", "Home");
    }
}
