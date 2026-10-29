using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace GRUPAL.Controllers;

public class CuentaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();

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

        nombre = nombre.Trim();
        correo = correo.Trim().ToLowerInvariant();
        if (nombre.Length > 100 || correo.Length > 150 || contraseña.Length < 8 || contraseña.Length > 128)
        {
            ViewBag.Error = "Usa un nombre y correo válidos y una contraseña de entre 8 y 128 caracteres.";
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
            Contraseña = string.Empty,
            PublicacionesActivas = 0,
            HistoriasResueltas = 0,
            BuenasAcciones = 0
        };

        nuevoUsuario.Contraseña = _hasher.HashPassword(nuevoUsuario, contraseña);
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

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo.Trim().ToLowerInvariant());
        
        if (usuario != null)
        {
            var verificacion = usuario.Contraseña.StartsWith("AQAAAA", StringComparison.Ordinal)
                ? _hasher.VerifyHashedPassword(usuario, usuario.Contraseña, contraseña)
                : PasswordVerificationResult.Failed;
            var legado = verificacion == PasswordVerificationResult.Failed &&
                (VerificarLegado(usuario.Contraseña, HashPassword(contraseña)) ||
                 VerificarLegado(usuario.Contraseña, contraseña));
            if (verificacion != PasswordVerificationResult.Failed || legado)
            {
                // Actualizar hashes antiguos (SHA256 o texto de datos demostrativos) al iniciar sesión.
                if (legado || verificacion == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    usuario.Contraseña = _hasher.HashPassword(usuario, contraseña);
                    await _context.SaveChangesAsync();
                }
            }
            if (verificacion != PasswordVerificationResult.Failed || legado)
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

        ViewBag.Resueltas = usuario.ObjetosPerdidos.Count(o => o.Estado == EstadoObjeto.Encontrado);
        ViewBag.BuenasAcciones = await _context.SolicitudesRecuperacion
            .CountAsync(s => s.ObjetoPerdido.UsuarioId == usuario.Id && s.Estado == EstadoSolicitud.Aceptada);
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

    private static bool VerificarLegado(string almacenada, string candidata)
    {
        var a = Encoding.UTF8.GetBytes(almacenada);
        var b = Encoding.UTF8.GetBytes(candidata);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    // Sólo compatibilidad de migración: las contraseñas nuevas usan PasswordHasher (PBKDF2).
    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}
