using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers;

public class HizmetController : Controller
{
    private readonly HizmetService _hizmetService;
    private readonly KlinikService _klinikService;

    public HizmetController(HizmetService hizmetService, KlinikService klinikService)
    {
        _hizmetService = hizmetService;
        _klinikService = klinikService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? klinikId)
    {
        await KlinikleriYukleAsync(klinikId);
        ViewBag.SeciliKlinikId = klinikId;
        var hizmetler = await _hizmetService.TumHizmetleriGetirAsync(klinikId);
        return View(hizmetler);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var hizmet = await _hizmetService.IdIleGetirAsync(id);
        if (hizmet == null)
        {
            return NotFound();
        }

        return View(hizmet);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await KlinikleriYukleAsync();
        return View(new Hizmet { SureDakika = 30 });
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Hizmet hizmet)
    {
        if (!ModelState.IsValid)
        {
            await KlinikleriYukleAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        var sonuc = await _hizmetService.OlusturAsync(hizmet);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await KlinikleriYukleAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hizmet = await _hizmetService.IdIleGetirAsync(id);
        if (hizmet == null)
        {
            return NotFound();
        }

        await KlinikleriYukleAsync(hizmet.KlinikId);
        return View(hizmet);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Hizmet hizmet)
    {
        if (id != hizmet.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await KlinikleriYukleAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        var sonuc = await _hizmetService.GuncelleAsync(hizmet);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await KlinikleriYukleAsync(hizmet.KlinikId);
            return View(hizmet);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var hizmet = await _hizmetService.IdIleGetirAsync(id);
        if (hizmet == null)
        {
            return NotFound();
        }

        return View(hizmet);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sonuc = await _hizmetService.SilAsync(id);
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    private async Task KlinikleriYukleAsync(int? seciliKlinikId = null)
    {
        var klinikler = await _klinikService.TumKlinikleriGetirAsync();
        ViewBag.Klinikler = new SelectList(klinikler, nameof(Klinik.Id), nameof(Klinik.Ad), seciliKlinikId);
    }
}
