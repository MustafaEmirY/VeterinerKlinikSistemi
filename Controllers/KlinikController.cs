using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers;

[Authorize(Roles = Roller.Admin)]
public class KlinikController : Controller
{
    private readonly KlinikService _klinikService;

    public KlinikController(KlinikService klinikService)
    {
        _klinikService = klinikService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var klinikler = await _klinikService.TumKlinikleriGetirAsync();
        return View(klinikler);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var klinik = await _klinikService.IdIleGetirAsync(id);
        if (klinik == null)
        {
            return NotFound();
        }

        return View(klinik);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Klinik());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Klinik klinik)
    {
        if (!ModelState.IsValid)
        {
            return View(klinik);
        }

        var sonuc = await _klinikService.OlusturAsync(klinik);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            return View(klinik);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var klinik = await _klinikService.IdIleGetirAsync(id);
        if (klinik == null)
        {
            return NotFound();
        }

        return View(klinik);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Klinik klinik)
    {
        if (id != klinik.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(klinik);
        }

        var sonuc = await _klinikService.GuncelleAsync(klinik);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            return View(klinik);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var klinik = await _klinikService.IdIleGetirAsync(id);
        if (klinik == null)
        {
            return NotFound();
        }

        return View(klinik);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sonuc = await _klinikService.SilAsync(id);
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }
}
