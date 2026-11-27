using Microsoft.AspNetCore.Mvc;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers.Api;

[ApiController]
[Route("api/veterinerler")]
public class VeterinerApiController : ControllerBase
{
    private readonly VeterinerService _veterinerService;

    public VeterinerApiController(VeterinerService veterinerService)
    {
        _veterinerService = veterinerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VeterinerApiDto>>> Listele([FromQuery] int? klinikId, [FromQuery] string? uzmanlik)
    {
        var veterinerler = await _veterinerService.TumVeterinerleriGetirAsync(klinikId, uzmanlik);

        var dtoList = veterinerler.Select(MapToDto).ToList();
        return Ok(dtoList);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VeterinerApiDto>> DetayGetir(int id)
    {
        var vet = await _veterinerService.IdIleGetirAsync(id);
        if (vet == null)
        {
            return NotFound(new { mesaj = "Veteriner hekim bulunamadı." });
        }

        return Ok(MapToDto(vet));
    }

    private static VeterinerApiDto MapToDto(Veteriner v) => new()
    {
        Id = v.Id,
        AdSoyad = v.AdSoyad,
        UzmanlikAlanlari = v.UzmanlikAlanlari,
        KlinikAdi = v.Klinik?.Ad ?? string.Empty,
        Hizmetler = v.VeterinerHizmetler
            .Where(vh => vh.Hizmet != null)
            .Select(vh => vh.Hizmet!.Ad)
            .OrderBy(ad => ad)
            .ToList(),
        MusaitGunler = v.Musaitlikler
            .OrderBy(m => GunYardimcisi.SiraNo(m.Gun))
            .ThenBy(m => m.BaslangicSaati)
            .Select(m => $"{GunYardimcisi.TurkceAd(m.Gun)} {m.BaslangicSaati:HH:mm}-{m.BitisSaati:HH:mm}")
            .ToList()
    };
}
