using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Controllers;

[Authorize]
public class RandevuController : Controller
{
    private readonly RandevuService _randevuService;
    private readonly EvcilHayvanService _evcilHayvanService;
    private readonly HizmetService _hizmetService;
    private readonly VeterinerService _veterinerService;

    public RandevuController(
        RandevuService randevuService,
        EvcilHayvanService evcilHayvanService,
        HizmetService hizmetService,
        VeterinerService veterinerService)
    {
        _randevuService = randevuService;
        _evcilHayvanService = evcilHayvanService;
        _hizmetService = hizmetService;
        _veterinerService = veterinerService;
    }

    private string AktifKullaniciId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool AdminMi => User.IsInRole(Roller.Admin);
    private bool VeterinerMi => User.IsInRole(Roller.Veteriner);

    [HttpGet]
    public async Task<IActionResult> Index(RandevuDurumu? durum)
    {
        ViewBag.SeciliDurum = durum;
        var randevular = await _randevuService.ListeleAsync(AktifKullaniciId, AdminMi, VeterinerMi, durum);
        return View(randevular);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var (kayit, yetkisiz) = await _randevuService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, AdminMi, VeterinerMi);
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

    [HttpGet]
    public async Task<IActionResult> UygunSaatler(int hizmetId, DateOnly tarih, int? haricRandevuId = null)
    {
        if (hizmetId <= 0)
        {
            return BadRequest(new { mesaj = "Geçerli bir hizmet seçiniz." });
        }

        var saatler = await _randevuService.UygunSaatleriGetirAsync(hizmetId, tarih, haricRandevuId);
        return Json(saatler);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Create(int? evcilHayvanId)
    {
        var model = new RandevuFormViewModel
        {
            EvcilHayvanId = evcilHayvanId ?? 0,
            Tarih = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Saat = new TimeOnly(10, 0)
        };

        await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RandevuFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
            return View(model);
        }

        var baslangicZamani = model.Tarih.ToDateTime(model.Saat);
        var (sonuc, yetkisiz, _) = await _randevuService.OlusturAsync(
            model.EvcilHayvanId,
            model.HizmetId,
            model.VeterinerId,
            baslangicZamani,
            AktifKullaniciId,
            AdminMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (kayit, yetkisiz) = await _randevuService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, AdminMi, false);
        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound();
        }

        if (!AdminMi && kayit.Durum != RandevuDurumu.Bekliyor)
        {
            TempData["Hata"] = "Yalnızca 'Bekliyor' durumundaki randevular düzenlenebilir.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new RandevuFormViewModel
        {
            Id = kayit.Id,
            EvcilHayvanId = kayit.EvcilHayvanId,
            HizmetId = kayit.HizmetId,
            VeterinerId = kayit.VeterinerId,
            Tarih = DateOnly.FromDateTime(kayit.BaslangicZamani),
            Saat = TimeOnly.FromDateTime(kayit.BaslangicZamani)
        };

        await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RandevuFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
            return View(model);
        }

        var baslangicZamani = model.Tarih.ToDateTime(model.Saat);
        var (sonuc, yetkisiz) = await _randevuService.GuncelleAsync(
            model.Id,
            model.EvcilHayvanId,
            model.HizmetId,
            model.VeterinerId,
            baslangicZamani,
            AktifKullaniciId,
            AdminMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            await FormListeleriniYukleAsync(model.EvcilHayvanId, model.HizmetId, model.VeterinerId);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var (kayit, yetkisiz) = await _randevuService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, AdminMi, false);
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
        var (sonuc, yetkisiz) = await _randevuService.SilAsync(id, AktifKullaniciId, AdminMi);
        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            TempData["Hata"] = sonuc.Mesaj;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id)
    {
        var (sonuc, yetkisiz) = await _randevuService.DurumGuncelleAsync(
            id,
            RandevuDurumu.Onaylandi,
            AktifKullaniciId,
            AdminMi,
            VeterinerMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(int id)
    {
        var (sonuc, yetkisiz) = await _randevuService.DurumGuncelleAsync(
            id,
            RandevuDurumu.Reddedildi,
            AktifKullaniciId,
            AdminMi,
            VeterinerMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IptalEt(int id)
    {
        var (sonuc, yetkisiz) = await _randevuService.DurumGuncelleAsync(
            id,
            RandevuDurumu.IptalEdildi,
            AktifKullaniciId,
            AdminMi,
            VeterinerMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        TempData[sonuc.Basarili ? "Basari" : "Hata"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpGet]
    public async Task<IActionResult> MuayeneNotu(int id)
    {
        var (kayit, yetkisiz) = await _randevuService.ErisimKontrolluGetirAsync(id, AktifKullaniciId, AdminMi, VeterinerMi);
        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound();
        }

        if (kayit.Durum is RandevuDurumu.Reddedildi or RandevuDurumu.IptalEdildi)
        {
            TempData["Hata"] = "Reddedilmiş veya iptal edilmiş randevuya muayene notu girilemez.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new MuayeneNotuViewModel
        {
            RandevuId = kayit.Id,
            MuayeneNotu = kayit.MuayeneNotu ?? string.Empty,
            HayvanAdi = kayit.EvcilHayvan?.Ad,
            HizmetAdi = kayit.Hizmet?.Ad,
            VeterinerAdi = kayit.Veteriner?.AdSoyad,
            BaslangicZamani = kayit.BaslangicZamani
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Veteriner}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MuayeneNotu(MuayeneNotuViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (sonuc, yetkisiz) = await _randevuService.MuayeneNotuKaydetAsync(
            model.RandevuId,
            model.MuayeneNotu,
            AktifKullaniciId,
            AdminMi,
            VeterinerMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili)
        {
            ModelState.AddModelError(string.Empty, sonuc.Mesaj);
            return View(model);
        }

        TempData["Basari"] = sonuc.Mesaj;
        return RedirectToAction(nameof(Details), new { id = model.RandevuId });
    }

    private async Task FormListeleriniYukleAsync(int? seciliHayvanId, int? seciliHizmetId, int? seciliVeterinerId)
    {
        var hayvanlar = await _evcilHayvanService.ListeleAsync(AktifKullaniciId, AdminMi);
        var hayvanListesi = hayvanlar.Select(e => new
        {
            e.Id,
            Etiket = AdminMi && e.Sahip != null
                ? $"{e.Ad} ({e.Tur}) — {e.Sahip.AdSoyad}"
                : $"{e.Ad} ({e.Tur})"
        }).ToList();

        var hizmetler = await _hizmetService.TumHizmetleriGetirAsync();
        var hizmetListesi = hizmetler.Select(h => new
        {
            h.Id,
            Etiket = $"{h.Ad} ({h.SureDakika} dk · {h.Ucret:N0} ₺)"
        }).ToList();

        var veterinerler = await _veterinerService.TumVeterinerleriGetirAsync();

        ViewBag.EvcilHayvanlar = new SelectList(hayvanListesi, "Id", "Etiket", seciliHayvanId);
        ViewBag.Hizmetler = new SelectList(hizmetListesi, "Id", "Etiket", seciliHizmetId);
        ViewBag.Veterinerler = new SelectList(veterinerler, nameof(Veteriner.Id), nameof(Veteriner.AdSoyad), seciliVeterinerId);
    }
}
