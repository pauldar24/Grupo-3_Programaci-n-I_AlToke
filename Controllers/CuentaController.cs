using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;

namespace GRUPAL.Controllers;

public class CuentaController : Controller
{
    private readonly ApplicationDbContext _context;

    public CuentaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /Cuenta/Ingresar
    public IActionResult Ingresar()
    {
        return View();
    }

    // POST /Cuenta/Ingresar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingresar(string correo, string contraseña)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Todos los campos son obligatorios.";
            return View();
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo && u.Contraseña == contraseña);

        if (usuario == null)
        {
            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View();
        }

        // Guardar datos en sesión
        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
        HttpContext.Session.SetString("UsuarioCorreo", usuario.Correo);

        return RedirectToAction("MiPerfil");
    }

    // GET /Cuenta/CrearCuenta
    public IActionResult CrearCuenta()
    {
        return View();
    }

    // POST /Cuenta/CrearCuenta
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCuenta(string nombre, string correo, string contraseña)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contraseña))
        {
            ViewBag.Error = "Todos los campos son obligatorios.";
            return View();
        }

        // Verificar si el correo ya existe
        var existe = await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        if (existe)
        {
            ViewBag.Error = "Ya existe una cuenta con ese correo.";
            return View();
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            Contraseña = contraseña
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        // Iniciar sesión automáticamente
        HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
        HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
        HttpContext.Session.SetString("UsuarioCorreo", usuario.Correo);

        return RedirectToAction("MiPerfil");
    }

    // GET /Cuenta/MiPerfil
    public async Task<IActionResult> MiPerfil()
    {
        var usuarioId = HttpContext.Session.GetInt32("UsuarioId");

        if (usuarioId == null)
        {
            return RedirectToAction("Ingresar");
        }

        var usuario = await _context.Usuarios
            .Include(u => u.ObjetosPerdidos)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Ingresar");
        }

        return View(usuario);
    }

    // POST /Cuenta/CerrarSesion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CerrarSesion()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
