using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/evcilhayvanlar")]
public class AsiApiController : ControllerBase
{
    private readonly AsiService _asiService;

    public AsiApiController(AsiService asiService)
    {
        _asiService = asiService;
    }

    [HttpGet("{id:int}/asilar")]
    public async Task<ActionResult<AsiTakvimiDto>> AsiTakvimiGetir(int id)
    {
        var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var personelMi = User.IsInRole(Roller.Admin) || User.IsInRole(Roller.Veteriner);

        var (takvim, yetkisiz) = await _asiService.AsiTakvimiGetirAsync(id, kullaniciId, personelMi);
        if (yetkisiz)
        {
            return Forbid();
        }

        if (takvim == null)
        {
            return NotFound(new { mesaj = "Evcil hayvan bulunamadı." });
        }

        return Ok(takvim);
    }
}
