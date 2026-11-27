using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Controllers.Api;

[ApiController]
[Authorize(Roles = Roller.Admin)]
[Route("api/raporlar")]
public class RaporApiController : ControllerBase
{
    private readonly RaporService _raporService;

    public RaporApiController(RaporService raporService)
    {
        _raporService = raporService;
    }

    [HttpGet("ciro")]
    public async Task<ActionResult<CiroRaporuOzetDto>> CiroRaporu(
        [FromQuery] DateOnly? baslangic,
        [FromQuery] DateOnly? bitis,
        [FromQuery] int? klinikId)
    {
        if (baslangic.HasValue && bitis.HasValue && bitis.Value < baslangic.Value)
        {
            return BadRequest(new { mesaj = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
        }

        var rapor = await _raporService.HizmetCiroRaporuGetirAsync(baslangic, bitis, klinikId);
        return Ok(rapor);
    }
}
