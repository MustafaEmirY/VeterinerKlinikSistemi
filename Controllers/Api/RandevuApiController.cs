using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers.Api;

[ApiController]
[Route("api/randevular")]
public class RandevuApiController : ControllerBase
{
    private readonly RandevuService _randevuService;
    private readonly HizmetService _hizmetService;

    public RandevuApiController(RandevuService randevuService, HizmetService hizmetService)
    {
        _randevuService = randevuService;
        _hizmetService = hizmetService;
    }

    [HttpGet("uygun-saatler")]
    public async Task<ActionResult<List<UygunHekimSaatleriDto>>> UygunSaatler(
        [FromQuery] int hizmetId,
        [FromQuery] DateOnly? tarih,
        [FromQuery] int? haricRandevuId = null)
    {
        if (hizmetId <= 0 || !tarih.HasValue)
        {
            return BadRequest(new { mesaj = "Geçerli bir hizmetId ve tarih (yyyy-MM-dd) parametresi gereklidir." });
        }

        var hizmet = await _hizmetService.IdIleGetirAsync(hizmetId);
        if (hizmet == null)
        {
            return NotFound(new { mesaj = "Seçilen hizmet bulunamadı." });
        }

        var uygunSaatler = await _randevuService.UygunSaatleriGetirAsync(hizmetId, tarih.Value, haricRandevuId);
        return Ok(uygunSaatler);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RandevuApiDto>> DetayGetir(int id)
    {
        var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var adminMi = User.IsInRole(Roller.Admin);
        var veterinerMi = User.IsInRole(Roller.Veteriner);

        var (kayit, yetkisiz) = await _randevuService.ErisimKontrolluGetirAsync(id, kullaniciId, adminMi, veterinerMi);
        if (yetkisiz)
        {
            return Forbid();
        }

        if (kayit == null)
        {
            return NotFound(new { mesaj = "Randevu bulunamadı." });
        }

        return Ok(MapToDto(kayit));
    }

    [Authorize(Roles = $"{Roller.Admin},{Roller.Uye}")]
    [HttpPost]
    public async Task<ActionResult<RandevuApiDto>> Olustur([FromBody] RandevuOlusturApiDto model)
    {
        var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var adminMi = User.IsInRole(Roller.Admin);

        var (sonuc, yetkisiz, randevu) = await _randevuService.OlusturAsync(
            model.EvcilHayvanId,
            model.HizmetId,
            model.VeterinerId,
            model.BaslangicZamani,
            kullaniciId,
            adminMi);

        if (yetkisiz)
        {
            return Forbid();
        }

        if (!sonuc.Basarili || randevu == null)
        {
            return BadRequest(new { mesaj = sonuc.Mesaj });
        }

        var (detay, _) = await _randevuService.ErisimKontrolluGetirAsync(randevu.Id, kullaniciId, true, false);
        var dto = detay != null ? MapToDto(detay) : new RandevuApiDto { Id = randevu.Id };

        return CreatedAtAction(nameof(DetayGetir), new { id = randevu.Id }, dto);
    }

    private static RandevuApiDto MapToDto(Randevu r) => new()
    {
        Id = r.Id,
        EvcilHayvanId = r.EvcilHayvanId,
        EvcilHayvanAdi = r.EvcilHayvan?.Ad ?? string.Empty,
        HizmetId = r.HizmetId,
        HizmetAdi = r.Hizmet?.Ad ?? string.Empty,
        VeterinerId = r.VeterinerId,
        VeterinerAdi = r.Veteriner?.AdSoyad ?? string.Empty,
        BaslangicZamani = r.BaslangicZamani,
        SureDakika = r.SureDakika,
        Ucret = r.Ucret,
        Durum = RandevuService.DurumMetni(r.Durum),
        MuayeneNotu = r.MuayeneNotu
    };
}
