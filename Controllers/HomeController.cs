using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            Klinik = await _context.Klinikler.AsNoTracking().FirstOrDefaultAsync(),
            Hizmetler = await _context.Hizmetler.AsNoTracking().OrderBy(h => h.Id).ToListAsync(),
            Veterinerler = await _context.Veterinerler.AsNoTracking().OrderBy(v => v.Id).ToListAsync()
        };

        return View(model);
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
