using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers;

[Authorize]
public class AsiController : Controller
{
    private readonly AsiService _asiService;
    private readonly EvcilHayvanService _evcilHayvanService;

    public AsiController(AsiService asiService, EvcilHayvanService evcilHayvanService)
    {
        _asiService = asiService;
        _evcilHayvanService = evcilHayvanService;
    }

    private string AktifKullaniciId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool YetkiliPersonelMi => User.IsInRole(Roller.Admin) || User.IsInRole(Roller.Veteriner);

    [HttpGet]
    public async Task<IActionResult> Index(int? evcilHayvanId, bool sadeceYaklasan = false)
    {
        await HayvanlariYukleAsync(evcilHayvanId);
        ViewBag.SeciliHayvanId = evcilHayvanId;
        ViewBag.SadeceYaklasan = sadeceYaklasan;

        var liste = await _asiService.ListeleAsync(
            AktifKullaniciId,
            YetkiliPersonelMi,
            evcilHayvanId,
            sadeceYaklasan);

        return View(liste);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (kayit, yetkisiz) = await _asiService.ErisimKontrolluGetirAsync(
            id,
            AktifKullaniciId,
            YetkiliPersonelMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound();
        }

        return View(kayit);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpGet]
    public async Task<IActionResult> Create(int? evcilHayvanId)
    {
        await HayvanlariYukleAsync(evcilHayvanId);
        var model = new AsiKaydi
        {
            EvcilHayvanId = evcilHayvanId ?? 0,
            YapildigiTarih = DateOnly.FromDateTime(DateTime.Today),
            SonrakiDozTarihi = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AsiKaydi asi)
    {
        if (!ModelState.IsValid)
        {
            await HayvanlariYukleAsync(asi.EvcilHayvanId);
            return View(asi);
        }

        var sonuc = await _asiService.OlusturAsync(asi);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await HayvanlariYukleAsync(asi.EvcilHayvanId);
            return View(asi);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index), new { evcilHayvanId = asi.EvcilHayvanId });
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (kayit, _) = await _asiService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, true);
        if (kayit == null)
        {
            return NotFound();
        }

        await HayvanlariYukleAsync(kayit.EvcilHayvanId);
        return View(kayit);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AsiKaydi asi)
    {
        if (id != asi.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await HayvanlariYukleAsync(asi.EvcilHayvanId);
            return View(asi);
        }

        var sonuc = await _asiService.GuncelleAsync(asi);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await HayvanlariYukleAsync(asi.EvcilHayvanId);
            return View(asi);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index), new { evcilHayvanId = asi.EvcilHayvanId });
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var (kayit, _) = await _asiService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, true);
        if (kayit == null)
        {
            return NotFound();
        }

        return View(kayit);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sonuc = await _asiService.SilAsync(id);
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    private async Task HayvanlariYukleAsync(int? seciliHayvanId = null)
    {
        var hayvanlar = await _evcilHayvanService.ListeleAsync(AktifKullaniciId, YetkiliPersonelMi);
        var liste = hayvanlar.Select(e => new
        {
            e.Id,
            Etiket = YetkiliPersonelMi && e.Sahip != null
                ? $"{e.Ad} ({e.Tur}) — {e.Sahip.AdSoyad}"
                : $"{e.Ad} ({e.Tur})"
        }).ToList();

        ViewBag.EvcilHayvanlar = new SelectList(liste, "Id", "Etiket", seciliHayvanId);
    }
}
