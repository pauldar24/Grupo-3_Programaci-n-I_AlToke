using GRUPAL.Services;
using Microsoft.AspNetCore.Mvc;

namespace GRUPAL.Controllers;

public class AsistenteController(AsistenteAlToke asistente) : Controller
{
    public record Pregunta(string Mensaje);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Preguntar([FromBody] Pregunta? peticion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(peticion?.Mensaje) || peticion.Mensaje.Length > 600)
            return BadRequest(new { error = "Escribe un mensaje de hasta 600 caracteres." });
        var (texto, objetos) = await asistente.ResponderAsync(peticion.Mensaje, cancellationToken);
        return Json(new { respuesta = texto, objetos });
    }
}
