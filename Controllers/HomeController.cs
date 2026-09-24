using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GRUPAL.Models;
using GRUPAL.Data;

namespace GRUPAL.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var objetosRecientes = await _context.ObjetosPerdidos
            .Include(o => o.Usuario)
            .OrderByDescending(o => o.Fecha)
            .Take(6)
            .ToListAsync();

        // Stats
        ViewBag.TotalObjetos = await _context.ObjetosPerdidos.CountAsync();
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.ObjetosEncontrados = await _context.ObjetosPerdidos.CountAsync(o => o.Estado == EstadoObjeto.Encontrado);

        return View(objetosRecientes);
    }

    public IActionResult ComoFunciona()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
