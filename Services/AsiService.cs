using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Services;

public class AsiService
{
    private readonly ApplicationDbContext _context;

    public AsiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public static string DozDurumu(DateOnly? sonrakiDozTarihi)
    {
        if (!sonrakiDozTarihi.HasValue)
        {
            return "Tek Doz";
        }

        var bugun = DateOnly.FromDateTime(DateTime.Today);
        var kalanGun = sonrakiDozTarihi.Value.DayNumber - bugun.DayNumber;

        if (kalanGun < 0)
        {
            return "Günü Geçti";
        }

        if (kalanGun <= 30)
        {
            return $"Yaklaşıyor ({kalanGun} gün)";
        }

        return "Planlandı";
    }

    public async Task<List<AsiKaydi>> ListeleAsync(
        string kullaniciId,
        bool tumKayitlar,
        int? evcilHayvanId = null,
        bool sadeceYaklasan = false)
    {
        var sorgu = _context.AsiKayitlari
            .AsNoTracking()
            .Include(a => a.EvcilHayvan)
                .ThenInclude(e => e!.Sahip)
            .AsQueryable();

        if (!tumKayitlar)
        {
            sorgu = sorgu.Where(a => a.EvcilHayvan != null && a.EvcilHayvan.SahipId == kullaniciId);
        }

        if (evcilHayvanId.HasValue)
        {
            sorgu = sorgu.Where(a => a.EvcilHayvanId == evcilHayvanId.Value);
        }

        if (sadeceYaklasan)
        {
            var bugun = DateOnly.FromDateTime(DateTime.Today);
            var otuzGunSonra = bugun.AddDays(30);
            sorgu = sorgu.Where(a => a.SonrakiDozTarihi.HasValue && a.SonrakiDozTarihi.Value <= otuzGunSonra);
        }

        return await sorgu
            .OrderByDescending(a => a.YapildigiTarih)
            .ThenBy(a => a.AsiAdi)
            .ToListAsync();
    }

    public async Task<(AsiKaydi? Kayit, bool Yetkisiz)> ErisimKontrolluGetirAsync(
        int id,
        string kullaniciId,
        bool tumKayitlaraErisim)
    {
        var kayit = await _context.AsiKayitlari
            .AsNoTracking()
            .Include(a => a.EvcilHayvan)
                .ThenInclude(e => e!.Sahip)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (kayit == null)
        {
            return (null, false);
        }

        if (!tumKayitlaraErisim && kayit.EvcilHayvan?.SahipId != kullaniciId)
        {
            return (null, true);
        }

        return (kayit, false);
    }

    public async Task<(AsiTakvimiDto? Takvim, bool Yetkisiz)> AsiTakvimiGetirAsync(
        int evcilHayvanId,
        string kullaniciId,
        bool tumKayitlaraErisim)
    {
        var hayvan = await _context.EvcilHayvanlar
            .AsNoTracking()
            .Include(e => e.AsiKayitlari)
            .FirstOrDefaultAsync(e => e.Id == evcilHayvanId);

        if (hayvan == null)
        {
            return (null, false);
        }

        if (!tumKayitlaraErisim && hayvan.SahipId != kullaniciId)
        {
            return (null, true);
        }

        return (TakvimOlustur(hayvan), false);
    }

    public async Task<AsiTakvimiDto?> AdaGoreAsiTakvimiGetirAsync(string hayvanAdi, string sahipId)
    {
        var aranan = hayvanAdi.Trim().ToLower();
        var hayvan = await _context.EvcilHayvanlar
            .AsNoTracking()
            .Include(e => e.AsiKayitlari)
            .FirstOrDefaultAsync(e =>
                e.SahipId == sahipId &&
                e.Ad.ToLower() == aranan);

        return hayvan == null ? null : TakvimOlustur(hayvan);
    }

    public async Task<IslemSonucu> OlusturAsync(AsiKaydi asi)
    {
        var hayvanVarMi = await _context.EvcilHayvanlar.AnyAsync(e => e.Id == asi.EvcilHayvanId);
        if (!hayvanVarMi)
        {
            return IslemSonucu.Hata("Seçilen evcil hayvan bulunamadı.");
        }

        var dogrulamaHatasi = TarihKurallariniKontrolEt(asi);
        if (dogrulamaHatasi != null)
        {
            return IslemSonucu.Hata(dogrulamaHatasi);
        }

        asi.AsiAdi = asi.AsiAdi.Trim();
        asi.Notlar = string.IsNullOrWhiteSpace(asi.Notlar) ? null : asi.Notlar.Trim();

        _context.AsiKayitlari.Add(asi);
        await _context.SaveChangesAsync();

        return IslemSonucu.Basari("Aşı kaydı başarıyla eklendi.");
    }

    public async Task<IslemSonucu> GuncelleAsync(AsiKaydi asi)
    {
        var mevcut = await _context.AsiKayitlari.FindAsync(asi.Id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Güncellenecek aşı kaydı bulunamadı.");
        }

        var hayvanVarMi = await _context.EvcilHayvanlar.AnyAsync(e => e.Id == asi.EvcilHayvanId);
        if (!hayvanVarMi)
        {
            return IslemSonucu.Hata("Seçilen evcil hayvan bulunamadı.");
        }

        var dogrulamaHatasi = TarihKurallariniKontrolEt(asi);
        if (dogrulamaHatasi != null)
        {
            return IslemSonucu.Hata(dogrulamaHatasi);
        }

        mevcut.EvcilHayvanId = asi.EvcilHayvanId;
        mevcut.AsiAdi = asi.AsiAdi.Trim();
        mevcut.YapildigiTarih = asi.YapildigiTarih;
        mevcut.SonrakiDozTarihi = asi.SonrakiDozTarihi;
        mevcut.Notlar = string.IsNullOrWhiteSpace(asi.Notlar) ? null : asi.Notlar.Trim();

        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Aşı kaydı güncellendi.");
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var mevcut = await _context.AsiKayitlari.FindAsync(id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Silinecek aşı kaydı bulunamadı.");
        }

        _context.AsiKayitlari.Remove(mevcut);
        await _context.SaveChangesAsync();

        return IslemSonucu.Basari("Aşı kaydı silindi.");
    }

    private static string? TarihKurallariniKontrolEt(AsiKaydi asi)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        if (asi.YapildigiTarih > bugun)
        {
            return "Aşının yapıldığı tarih bugünden ileri bir tarih olamaz.";
        }

        if (asi.SonrakiDozTarihi.HasValue && asi.SonrakiDozTarihi.Value <= asi.YapildigiTarih)
        {
            return "Sonraki doz tarihi, aşının yapıldığı tarihten sonra olmalıdır.";
        }

        return null;
    }

    private static AsiTakvimiDto TakvimOlustur(EvcilHayvan hayvan)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);

        var gecmis = hayvan.AsiKayitlari
            .OrderByDescending(a => a.YapildigiTarih)
            .Select(a => new AsiKaydiDto
            {
                Id = a.Id,
                AsiAdi = a.AsiAdi,
                YapildigiTarih = a.YapildigiTarih,
                SonrakiDozTarihi = a.SonrakiDozTarihi,
                Notlar = a.Notlar,
                Durum = DozDurumu(a.SonrakiDozTarihi)
            })
            .ToList();

        var yaklasan = hayvan.AsiKayitlari
            .Where(a => a.SonrakiDozTarihi.HasValue && a.SonrakiDozTarihi.Value >= bugun)
            .OrderBy(a => a.SonrakiDozTarihi)
            .Select(a => new AsiKaydiDto
            {
                Id = a.Id,
                AsiAdi = a.AsiAdi,
                YapildigiTarih = a.YapildigiTarih,
                SonrakiDozTarihi = a.SonrakiDozTarihi,
                Notlar = a.Notlar,
                Durum = DozDurumu(a.SonrakiDozTarihi)
            })
            .ToList();

        return new AsiTakvimiDto
        {
            EvcilHayvanId = hayvan.Id,
            HayvanAdi = hayvan.Ad,
            Tur = hayvan.Tur,
            Irk = hayvan.Irk,
            GecmisAsilar = gecmis,
            YaklasanAsilar = yaklasan
        };
    }
}
