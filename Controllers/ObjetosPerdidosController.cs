using GRUPAL.Data;
using GRUPAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace GRUPAL.Controllers;

public class ObjetosPerdidosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ObjetosPerdidosController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ObjetoPerdido { Fecha = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjetoPerdido objeto)
    {
        if (ModelState.IsValid)
        {
            objeto.UsuarioId = 1;
            objeto.Estado = EstadoObjeto.Perdido;

            _context.ObjetosPerdidos.Add(objeto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        return View(objeto);
    }
}