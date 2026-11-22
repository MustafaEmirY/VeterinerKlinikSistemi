using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Services;

public class HizmetService
{
    private readonly ApplicationDbContext _context;

    public HizmetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Hizmet>> TumHizmetleriGetirAsync(int? klinikId = null)
    {
        var sorgu = _context.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .AsQueryable();

        if (klinikId.HasValue)
        {
            sorgu = sorgu.Where(h => h.KlinikId == klinikId.Value);
        }

        return await sorgu
            .OrderBy(h => h.Klinik!.Ad)
            .ThenBy(h => h.Ad)
            .ToListAsync();
    }

    public async Task<Hizmet?> IdIleGetirAsync(int id)
    {
        return await _context.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .Include(h => h.VeterinerHizmetler)
                .ThenInclude(vh => vh.Veteriner)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<IslemSonucu> OlusturAsync(Hizmet hizmet)
    {
        var klinikVarMi = await _context.Klinikler.AnyAsync(k => k.Id == hizmet.KlinikId);
        if (!klinikVarMi)
        {
            return IslemSonucu.Hata("Seçilen klinik bulunamadı.");
        }

        _context.Hizmetler.Add(hizmet);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Hizmet başarıyla eklendi.");
    }

    public async Task<IslemSonucu> GuncelleAsync(Hizmet hizmet)
    {
        var mevcut = await _context.Hizmetler.FindAsync(hizmet.Id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Güncellenecek hizmet bulunamadı.");
        }

        var klinikVarMi = await _context.Klinikler.AnyAsync(k => k.Id == hizmet.KlinikId);
        if (!klinikVarMi)
        {
            return IslemSonucu.Hata("Seçilen klinik bulunamadı.");
        }

        mevcut.Ad = hizmet.Ad;
        mevcut.Aciklama = hizmet.Aciklama;
        mevcut.SureDakika = hizmet.SureDakika;
        mevcut.Ucret = hizmet.Ucret;
        mevcut.KlinikId = hizmet.KlinikId;

        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Hizmet bilgileri güncellendi.");
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var mevcut = await _context.Hizmetler.FindAsync(id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Silinecek hizmet bulunamadı.");
        }

        var randevuVarMi = await _context.Randevular.AnyAsync(r => r.HizmetId == id);
        if (randevuVarMi)
        {
            return IslemSonucu.Hata("Bu hizmete ait randevu kayıtları bulunduğu için silinemez.");
        }

        _context.Hizmetler.Remove(mevcut);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Hizmet silindi.");
    }
}
