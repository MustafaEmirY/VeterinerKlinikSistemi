using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers;

[Authorize(Roles = Roller.Admin)]
public class AdminController : Controller
{
    private readonly AdminService _adminService;
    private readonly KlinikService _klinikService;

    public AdminController(AdminService adminService, KlinikService klinikService)
    {
        _adminService = adminService;
        _klinikService = klinikService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var ozet = await _adminService.OzetGetirAsync();
        return View(ozet);
    }

    [HttpGet]
    public async Task<IActionResult> Raporlar()
    {
        var klinikler = await _klinikService.TumKlinikleriGetirAsync();
        ViewBag.Klinikler = new SelectList(klinikler, nameof(Klinik.Id), nameof(Klinik.Ad));
        return View();
    }
}
