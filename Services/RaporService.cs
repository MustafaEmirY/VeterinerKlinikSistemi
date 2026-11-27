using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Services;

public class RaporService
{
    private readonly ApplicationDbContext _context;

    public RaporService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CiroRaporuOzetDto> HizmetCiroRaporuGetirAsync(
        DateOnly? baslangicTarihi = null,
        DateOnly? bitisTarihi = null,
        int? klinikId = null)
    {
        var sorgu = _context.Randevular
            .AsNoTracking()
            .Include(r => r.Hizmet)
                .ThenInclude(h => h!.Klinik)
            .AsQueryable();

        if (baslangicTarihi.HasValue)
        {
            var baslangicDt = baslangicTarihi.Value.ToDateTime(TimeOnly.MinValue);
            sorgu = sorgu.Where(r => r.BaslangicZamani >= baslangicDt);
        }

        if (bitisTarihi.HasValue)
        {
            var bitisDt = bitisTarihi.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            sorgu = sorgu.Where(r => r.BaslangicZamani < bitisDt);
        }

        if (klinikId.HasValue)
        {
            sorgu = sorgu.Where(r => r.Hizmet != null && r.Hizmet.KlinikId == klinikId.Value);
        }

        var hizmetBazli = await sorgu
            .GroupBy(r => new
            {
                r.HizmetId,
                HizmetAdi = r.Hizmet != null ? r.Hizmet.Ad : "Bilinmeyen Hizmet",
                KlinikAdi = r.Hizmet != null && r.Hizmet.Klinik != null ? r.Hizmet.Klinik.Ad : ""
            })
            .Select(g => new HizmetCiroRaporuDto
            {
                HizmetId = g.Key.HizmetId,
                HizmetAdi = g.Key.HizmetAdi,
                KlinikAdi = g.Key.KlinikAdi,
                ToplamRandevuSayisi = g.Count(),
                OnayliVeyaTamamlananSayisi = g.Count(r =>
                    r.Durum == RandevuDurumu.Onaylandi || r.Durum == RandevuDurumu.Tamamlandi),
                BekleyenSayisi = g.Count(r => r.Durum == RandevuDurumu.Bekliyor),
                ToplamCiro = g
                    .Where(r => r.Durum == RandevuDurumu.Onaylandi || r.Durum == RandevuDurumu.Tamamlandi)
                    .Sum(r => r.Ucret)
            })
            .OrderByDescending(x => x.ToplamCiro)
            .ThenByDescending(x => x.ToplamRandevuSayisi)
            .ToListAsync();

        return new CiroRaporuOzetDto
        {
            BaslangicTarihi = baslangicTarihi,
            BitisTarihi = bitisTarihi,
            GenelRandevuSayisi = hizmetBazli.Sum(x => x.ToplamRandevuSayisi),
            GenelCiro = hizmetBazli.Sum(x => x.ToplamCiro),
            HizmetBazliRapor = hizmetBazli
        };
    }
}
