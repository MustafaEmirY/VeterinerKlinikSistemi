using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Controllers;

[Authorize]
public class EvcilHayvanController : Controller
{
    private static readonly string[] IzinliUzantilar = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaksimumDosyaBoyutu = 2 * 1024 * 1024; // 2 MB

    private readonly EvcilHayvanService _evcilHayvanService;
    private readonly IWebHostEnvironment _env;

    public EvcilHayvanController(EvcilHayvanService evcilHayvanService, IWebHostEnvironment env)
    {
        _evcilHayvanService = evcilHayvanService;
        _env = env;
    }

    private string AktifKullaniciId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool AdminMi => User.IsInRole(Roller.Admin);
    private bool VeterinerMi => User.IsInRole(Roller.Veteriner);

    [HttpGet]
    public async Task<IActionResult> Index(string? arama)
    {
        var tumKayitlar = AdminMi || VeterinerMi;
        ViewBag.Arama = arama;
        ViewBag.DuzenlemeYetkisi = AdminMi || !VeterinerMi;

        var liste = await _evcilHayvanService.ListeleAsync(AktifKullaniciId, tumKayitlar, arama);
        return View(liste);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (kayit, yetkisiz) = await _evcilHayvanService.ErisimKontrolluGetirAsync(
            id,
            AktifKullaniciId,
            AdminMi || VeterinerMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound();
        }

        ViewBag.DuzenlemeYetkisi = AdminMi || (!VeterinerMi && kayit.SahipId == AktifKullaniciId);
        return View(kayit);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EvcilHayvanFormViewModel
        {
            Kilo = 4.0m
        };

        if (AdminMi)
        {
            model.SahipId = AktifKullaniciId;
            await SahipleriYukleAsync(model.SahipId);
        }

        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EvcilHayvanFormViewModel model)
    {
        if (model.Fotograf != null)
        {
            var dosyaHata = FotografDogrula(model.Fotograf);
            if (dosyaHata != null)
            {
                ModelState.AddModelError(nameof(model.Fotograf), dosyaHata);
            }
        }

        if (!ModelState.IsValid)
        {
            if (AdminMi) await SahipleriYukleAsync(model.SahipId);
            return View(model);
        }

        string? fotografYolu = null;
        if (model.Fotograf != null)
        {
            fotografYolu = await FotografKaydetAsync(model.Fotograf);
        }

        var sonuc = await _evcilHayvanService.OlusturAsync(model, AktifKullaniciId, AdminMi, fotografYolu);
        if (!sonuc.Basarili)
        {
            FotografDosyasiniSil(fotografYolu);
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            if (AdminMi) await SahipleriYukleAsync(model.SahipId);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (kayit, yetkisiz) = await _evcilHayvanService.ErisimKontrolluGetirAsync(
            id,
            AktifKullaniciId,
            AdminMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound();
        }

        var model = new EvcilHayvanFormViewModel
        {
            Id = kayit.Id,
            Ad = kayit.Ad,
            Tur = kayit.Tur,
            Irk = kayit.Irk,
            DogumTarihi = kayit.DogumTarihi,
            Kilo = kayit.Kilo,
            MevcutFotografYolu = kayit.FotografYolu,
            SahipId = kayit.SahipId
        };

        if (AdminMi)
        {
            await SahipleriYukleAsync(model.SahipId);
        }

        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EvcilHayvanFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (model.Fotograf != null)
        {
            var dosyaHata = FotografDogrula(model.Fotograf);
            if (dosyaHata != null)
            {
                ModelState.AddModelError(nameof(model.Fotograf), dosyaHata);
            }
        }

        if (!ModelState.IsValid)
        {
            if (AdminMi) await SahipleriYukleAsync(model.SahipId);
            return View(model);
        }

        string? yeniFotografYolu = null;
        if (model.Fotograf != null)
        {
            yeniFotografYolu = await FotografKaydetAsync(model.Fotograf);
        }

        var (sonuc, yetkisiz, silinecekEskiFotograf) = await _evcilHayvanService.GuncelleAsync(
            model,
            AktifKullaniciId,
            AdminMi,
            yeniFotografYolu);

        if (yetkisiz)
        {
            FotografDosyasiniSil(yeniFotografYolu);
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            FotografDosyasiniSil(yeniFotografYolu);
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            if (AdminMi) await SahipleriYukleAsync(model.SahipId);
            return View(model);
        }

        FotografDosyasiniSil(silinecekEskiFotograf);
        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var (kayit, yetkisiz) = await _evcilHayvanService.ErisimKontrolluGetirAsync(
            id,
            AktifKullaniciId,
            AdminMi);

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

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (sonuc, yetkisiz, silinecekFotograf) = await _evcilHayvanService.SilAsync(
            id,
            AktifKullaniciId,
            AdminMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        FotografDosyasiniSil(silinecekFotograf);
        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    private async Task SahipleriYukleAsync(string? seciliSahipId = null)
    {
        var kullanicilar = await _evcilHayvanService.KullanicilariGetirAsync();
        var liste = kullanicilar.Select(u => new
        {
            u.Id,
            AdEtiket = $"{u.AdSoyad} ({u.Email})"
        }).ToList();

        ViewBag.Sahipler = new SelectList(liste, "Id", "AdEtiket", seciliSahipId);
    }

    private static string? FotografDogrula(IFormFile dosya)
    {
        if (dosya.Length > MaksimumDosyaBoyutu)
        {
            return "Fotoğraf boyutu en fazla 2 MB olabilir.";
        }

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        if (!IzinliUzantilar.Contains(uzanti))
        {
            return "Yalnızca .jpg, .jpeg, .png veya .webp formatında fotoğraf yükleyebilirsiniz.";
        }

        return null;
    }

    private async Task<string> FotografKaydetAsync(IFormFile dosya)
    {
        var yuklemeKlasoru = Path.Combine(_env.WebRootPath, "uploads", "hayvanlar");
        Directory.CreateDirectory(yuklemeKlasoru);

        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        var dosyaAdi = $"{Guid.NewGuid():N}{uzanti}";
        var tamYol = Path.Combine(yuklemeKlasoru, dosyaAdi);

        await using var stream = new FileStream(tamYol, FileMode.Create);
        await dosya.CopyToAsync(stream);

        return $"/uploads/hayvanlar/{dosyaAdi}";
    }

    private void FotografDosyasiniSil(string? goreliYol)
    {
        if (string.IsNullOrWhiteSpace(goreliYol) ||
            !goreliYol.StartsWith("/uploads/hayvanlar/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var dosyaAdi = Path.GetFileName(goreliYol);
        var tamYol = Path.Combine(_env.WebRootPath, "uploads", "hayvanlar", dosyaAdi);
        if (System.IO.File.Exists(tamYol))
        {
            System.IO.File.Delete(tamYol);
        }
    }
}
