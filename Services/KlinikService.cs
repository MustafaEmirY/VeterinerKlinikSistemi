using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Services;

public class KlinikService
{
    private readonly ApplicationDbContext _context;

    public KlinikService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Klinik>> TumKlinikleriGetirAsync()
    {
        return await _context.Klinikler
            .AsNoTracking()
            .Include(k => k.Hizmetler)
            .Include(k => k.Veterinerler)
            .OrderBy(k => k.Ad)
            .ToListAsync();
    }

    public async Task<Klinik?> IdIleGetirAsync(int id)
    {
        return await _context.Klinikler
            .AsNoTracking()
            .Include(k => k.Hizmetler)
            .Include(k => k.Veterinerler)
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<IslemSonucu> OlusturAsync(Klinik klinik)
    {
        if (klinik.KapanisSaati <= klinik.AcilisSaati)
        {
            return IslemSonucu.Hata("Kapanış saati açılış saatinden sonra olmalıdır.");
        }

        _context.Klinikler.Add(klinik);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Klinik başarıyla oluşturuldu.");
    }

    public async Task<IslemSonucu> GuncelleAsync(Klinik klinik)
    {
        if (klinik.KapanisSaati <= klinik.AcilisSaati)
        {
            return IslemSonucu.Hata("Kapanış saati açılış saatinden sonra olmalıdır.");
        }

        var mevcut = await _context.Klinikler.FindAsync(klinik.Id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Güncellenecek klinik bulunamadı.");
        }

        mevcut.Ad = klinik.Ad;
        mevcut.Adres = klinik.Adres;
        mevcut.Telefon = klinik.Telefon;
        mevcut.AcilisSaati = klinik.AcilisSaati;
        mevcut.KapanisSaati = klinik.KapanisSaati;

        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Klinik bilgileri güncellendi.");
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var mevcut = await _context.Klinikler.FindAsync(id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Silinecek klinik bulunamadı.");
        }

        var bagliKayitVarMi =
            await _context.Hizmetler.AnyAsync(h => h.KlinikId == id) ||
            await _context.Veterinerler.AnyAsync(v => v.KlinikId == id);

        if (bagliKayitVarMi)
        {
            return IslemSonucu.Hata("Bu kliniğe bağlı hizmet veya veteriner kayıtları bulunduğu için silinemez.");
        }

        _context.Klinikler.Remove(mevcut);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Klinik silindi.");
    }
}
