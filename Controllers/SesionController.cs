using Microsoft.AspNetCore.Mvc;

namespace GRUPAL.Controllers;

/// <summary>
/// Controlador de ejemplo – Paso 4: demuestra cómo guardar y leer
/// valores en la sesión respaldada por Redis.
/// </summary>
public class SesionController : Controller
{
    private const string ClaveNombreUsuario = "NombreUsuarioTemporal";

    // GET /Sesion/Guardar?nombre=Carlos
    public IActionResult Guardar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest("Debe proporcionar un nombre en el query string (?nombre=Carlos).");

        // ── Guardar en sesión ──────────────────────────────────────
        HttpContext.Session.SetString(ClaveNombreUsuario, nombre);

        ViewBag.Mensaje = $"Nombre «{nombre}» guardado en sesión correctamente.";
        return View("Resultado");
    }

    // GET /Sesion/Leer
    public IActionResult Leer()
    {
        // ── Leer de sesión ─────────────────────────────────────────
        var nombre = HttpContext.Session.GetString(ClaveNombreUsuario);

        ViewBag.Mensaje = nombre is not null
            ? $"Nombre en sesión: {nombre}"
            : "No hay ningún nombre guardado en la sesión.";

        return View("Resultado");
    }

    // GET /Sesion/Borrar
    public IActionResult Borrar()
    {
        HttpContext.Session.Remove(ClaveNombreUsuario);

        ViewBag.Mensaje = "Sesión limpiada correctamente.";
        return View("Resultado");
    }

    // GET /Sesion/Estado  (devuelve JSON para pruebas rápidas)
    public IActionResult Estado()
    {
        var nombre = HttpContext.Session.GetString(ClaveNombreUsuario);
        return Json(new
        {
            sesionId = HttpContext.Session.Id,
            nombreGuardado = nombre,
            hayDatos = nombre is not null
        });
    }
}
