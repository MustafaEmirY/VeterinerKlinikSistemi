using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Controllers;

public class VeterinerController : Controller
{
    private readonly VeterinerService _veterinerService;
    private readonly KlinikService _klinikService;
    private readonly HizmetService _hizmetService;

    public VeterinerController(
        VeterinerService veterinerService,
        KlinikService klinikService,
        HizmetService hizmetService)
    {
        _veterinerService = veterinerService;
        _klinikService = klinikService;
        _hizmetService = hizmetService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? klinikId, string? arama)
    {
        await KlinikleriYukleAsync(klinikId);
        ViewBag.SeciliKlinikId = klinikId;
        ViewBag.Arama = arama;

        if (User.Identity?.IsAuthenticated == true && User.IsInRole(Roller.Veteriner))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var kendiVet = await _veterinerService.KullaniciIdIleGetirAsync(userId);
                ViewBag.KendiVeterinerId = kendiVet?.Id;
            }
        }

        var veterinerler = await _veterinerService.TumVeterinerleriGetirAsync(klinikId, arama);
        return View(veterinerler);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var veteriner = await _veterinerService.IdIleGetirAsync(id);
        if (veteriner == null)
        {
            return NotFound();
        }

        ViewBag.MusaitlikDuzenleyebilir = await MusaitlikYetkisiVarMiAsync(id);
        return View(veteriner);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await FormVerileriniYukleAsync();
        return View(new VeterinerFormViewModel());
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VeterinerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FormVerileriniYukleAsync(model.KlinikId);
            return View(model);
        }

        var sonuc = await _veterinerService.OlusturAsync(model);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await FormVerileriniYukleAsync(model.KlinikId);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var veteriner = await _veterinerService.IdIleGetirAsync(id);
        if (veteriner == null)
        {
            return NotFound();
        }

        var model = new VeterinerFormViewModel
        {
            Id = veteriner.Id,
            AdSoyad = veteriner.AdSoyad,
            UzmanlikAlanlari = veteriner.UzmanlikAlanlari,
            Eposta = veteriner.Eposta,
            Telefon = veteriner.Telefon,
            KlinikId = veteriner.KlinikId,
            SeciliHizmetIdleri = veteriner.VeterinerHizmetler.Select(vh => vh.HizmetId).ToList(),
            HesapOlustur = false,
            MevcutKullaniciEposta = veteriner.Kullanici?.Email
        };

        await FormVerileriniYukleAsync(veteriner.KlinikId);
        return View(model);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VeterinerFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FormVerileriniYukleAsync(model.KlinikId);
            return View(model);
        }

        var sonuc = await _veterinerService.GuncelleAsync(model);
        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await FormVerileriniYukleAsync(model.KlinikId);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var veteriner = await _veterinerService.IdIleGetirAsync(id);
        if (veteriner == null)
        {
            return NotFound();
        }

        return View(veteriner);
    }

    [Authorize(Roles = Roller.Admin)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sonuc = await _veterinerService.SilAsync(id);
        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpGet]
    public async Task<IActionResult> Musaitlikler(int id)
    {
        var veteriner = await _veterinerService.IdIleGetirAsync(id);
        if (veteriner == null)
        {
            return NotFound();
        }

        if (!await MusaitlikYetkisiVarMiAsync(id))
        {
            return Forbid();
        }

        ViewBag.Veteriner = veteriner;
        return View(new MusaitlikEkleViewModel { VeterinerId = id });
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikEkle(MusaitlikEkleViewModel model)
    {
        if (!await MusaitlikYetkisiVarMiAsync(model.VeterinerId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var veteriner = await _veterinerService.IdIleGetirAsync(model.VeterinerId);
            if (veteriner == null)
            {
                return NotFound();
            }

            ViewBag.Veteriner = veteriner;
            return View("Musaitlikler", model);
        }

        var sonuc = await _veterinerService.MusaitlikEkleAsync(model);
        if (sonuc.Basarili)
        {
            TempData["Basari"] = sonuc.Mesaj;
        }
        else
        {
            TempData["Hata"] = sonuc.Mesaj;
        }

        return RedirectToAction(nameof(Musaitlikler), new { id = model.VeterinerId });
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MusaitlikSil(int id, int veterinerId)
    {
        if (!await MusaitlikYetkisiVarMiAsync(veterinerId))
        {
            return Forbid();
        }

        var sonuc = await _veterinerService.MusaitlikSilAsync(id, veterinerId);
        if (sonuc.Basarili)
        {
            TempData["Basari"] = sonuc.Mesaj;
        }
        else
        {
            TempData["Hata"] = sonuc.Mesaj;
        }

        return RedirectToAction(nameof(Musaitlikler), new { id = veterinerId });
    }

    private async Task KlinikleriYukleAsync(int? seciliKlinikId = null)
    {
        var klinikler = await _klinikService.TumKlinikleriGetirAsync();
        ViewBag.Klinikler = new SelectList(klinikler, nameof(Klinik.Id), nameof(Klinik.Ad), seciliKlinikId);
    }

    private async Task FormVerileriniYukleAsync(int? seciliKlinikId = null)
    {
        await KlinikleriYukleAsync(seciliKlinikId);
        ViewBag.TumHizmetler = await _hizmetService.TumHizmetleriGetirAsync();
    }

    private async Task<bool> MusaitlikYetkisiVarMiAsync(int veterinerId)
    {
        if (User.IsInRole(Roller.Admin))
        {
            return true;
        }

        if (User.IsInRole(Roller.Veteriner))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var vet = await _veterinerService.KullaniciIdIleGetirAsync(userId);
                return vet != null && vet.Id == veterinerId;
            }
        }

        return false;
    }
}
