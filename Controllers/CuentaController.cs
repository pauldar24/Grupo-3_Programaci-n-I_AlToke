using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using System.Security.Cryptography;
using System.Text;

namespace GRUPAL.Controllers;

public class CuentaController : Controller
{
    private readonly ApplicationDbContext _context;

    public CuentaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Cuenta/CrearCuenta
    public IActionResult CrearCuenta()
    {
        if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Index", "Home");
            
        return View();
    }

    // POST: Cuenta/CrearCuenta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuenta(string nombre, string correo, string contraseña)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Todos los campos son obligatorios.";
            return View();
        }

        var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        if (existeCorreo)
        {
            ViewBag.Error = "El correo ya está registrado.";
            return View();
        }

        var nuevoUsuario = new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            Contraseña = HashPassword(contraseña),
            PublicacionesActivas = 0,
            HistoriasResueltas = 0,
            BuenasAcciones = 0
        };

        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        // Iniciar sesión automáticamente
        HttpContext.Session.SetInt32("UsuarioId", nuevoUsuario.Id);
        HttpContext.Session.SetString("UsuarioNombre", nuevoUsuario.Nombre);

        return RedirectToAction("Index", "Home");
    }

    // GET: Cuenta/Ingresar
    public IActionResult Ingresar()
    {
        if (HttpContext.Session.GetInt32("UsuarioId").HasValue)
            return RedirectToAction("Index", "Home");
            
        return View();
    }

    // POST: Cuenta/Ingresar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(string correo, string contraseña)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Debe ingresar correo y contraseña.";
            return View();
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        
        if (usuario != null)
        {
            // Para soportar las contraseñas planas del DbSeeder y las nuevas hasheadas
            bool esValida = usuario.Contraseña == contraseña || usuario.Contraseña == HashPassword(contraseña);
            
            if (esValida)
            {
                HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
                return RedirectToAction("Index", "Home");
            }
        }

        ViewBag.Error = "Correo o contraseña incorrectos.";
        return View();
    }

    // GET: Cuenta/MiPerfil
    public async Task<IActionResult> MiPerfil()
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
        if (!usuarioId.HasValue)
            return RedirectToAction(nameof(Ingresar));

        var usuario = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .FirstOrDefaultAsync(u => u.Id == usuarioId.Value);

        if (usuario == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Ingresar));
        }

        return View(usuario);
    }

    // POST: Cuenta/CerrarSesion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarSesion()
    {
        HttpContext.Session.Remove("UsuarioId");
        HttpContext.Session.Remove("UsuarioNombre");
        return RedirectToAction("Index", "Home");
    }

    // Helper de encriptación simple (SHA256)
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
