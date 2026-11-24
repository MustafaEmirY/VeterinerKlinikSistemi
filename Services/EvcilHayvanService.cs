using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Services;

public class EvcilHayvanService
{
    private readonly ApplicationDbContext _context;

    public EvcilHayvanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public static string YasMetni(DateOnly dogumTarihi)
    {
        var bugun = DateOnly.FromDateTime(DateTime.Today);
        if (dogumTarihi > bugun)
        {
            return "—";
        }

        var toplamAy = (bugun.Year - dogumTarihi.Year) * 12 + (bugun.Month - dogumTarihi.Month);
        if (bugun.Day < dogumTarihi.Day)
        {
            toplamAy--;
        }

        if (toplamAy < 1)
        {
            return "1 aydan küçük";
        }

        var yil = toplamAy / 12;
        var ay = toplamAy % 12;

        if (yil == 0)
        {
            return $"{ay} aylık";
        }

        return ay == 0 ? $"{yil} yaş" : $"{yil} yaş {ay} ay";
    }

    public async Task<List<EvcilHayvan>> ListeleAsync(string kullaniciId, bool tumKayitlar, string? arama = null)
    {
        var sorgu = _context.EvcilHayvanlar
            .AsNoTracking()
            .Include(e => e.Sahip)
            .Include(e => e.AsiKayitlari)
            .Include(e => e.Randevular)
            .AsQueryable();

        if (!tumKayitlar)
        {
            sorgu = sorgu.Where(e => e.SahipId == kullaniciId);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var terim = arama.Trim();
            sorgu = sorgu.Where(e =>
                e.Ad.Contains(terim) ||
                e.Tur.Contains(terim) ||
                e.Irk.Contains(terim));
        }

        return await sorgu
            .OrderBy(e => e.Ad)
            .ToListAsync();
    }

    public async Task<(EvcilHayvan? Kayit, bool Yetkisiz)> ErisimKontrolluGetirAsync(
        int id,
        string kullaniciId,
        bool tumKayitlaraErisim)
    {
        var kayit = await _context.EvcilHayvanlar
            .AsNoTracking()
            .Include(e => e.Sahip)
            .Include(e => e.AsiKayitlari)
            .Include(e => e.Randevular)
                .ThenInclude(r => r.Veteriner)
            .Include(e => e.Randevular)
                .ThenInclude(r => r.Hizmet)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (kayit == null)
        {
            return (null, false);
        }

        if (!tumKayitlaraErisim && kayit.SahipId != kullaniciId)
        {
            return (null, true);
        }

        return (kayit, false);
    }

    public async Task<EvcilHayvan?> AdaGoreSahipHayvaniGetirAsync(string hayvanAdi, string sahipId)
    {
        var aranan = hayvanAdi.Trim().ToLower();
        return await _context.EvcilHayvanlar
            .AsNoTracking()
            .Include(e => e.AsiKayitlari)
            .Include(e => e.Randevular)
                .ThenInclude(r => r.Veteriner)
            .Include(e => e.Randevular)
                .ThenInclude(r => r.Hizmet)
            .FirstOrDefaultAsync(e =>
                e.SahipId == sahipId &&
                e.Ad.ToLower() == aranan);
    }

    public async Task<List<ApplicationUser>> KullanicilariGetirAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.AdSoyad)
            .ToListAsync();
    }

    public async Task<IslemSonucu> OlusturAsync(
        EvcilHayvanFormViewModel model,
        string aktifKullaniciId,
        bool adminMi,
        string? fotografYolu)
    {
        if (model.DogumTarihi > DateOnly.FromDateTime(DateTime.Today))
        {
            return IslemSonucu.Hata("Doğum tarihi bugünden ileri bir tarih olamaz.");
        }

        var sahipId = (adminMi && !string.IsNullOrWhiteSpace(model.SahipId))
            ? model.SahipId
            : aktifKullaniciId;

        var sahipVarMi = await _context.Users.AnyAsync(u => u.Id == sahipId);
        if (!sahipVarMi)
        {
            return IslemSonucu.Hata("Seçilen hayvan sahibi bulunamadı.");
        }

        var evcilHayvan = new EvcilHayvan
        {
            Ad = model.Ad.Trim(),
            Tur = model.Tur.Trim(),
            Irk = model.Irk.Trim(),
            DogumTarihi = model.DogumTarihi,
            Kilo = model.Kilo,
            FotografYolu = fotografYolu,
            SahipId = sahipId
        };

        _context.EvcilHayvanlar.Add(evcilHayvan);
        await _context.SaveChangesAsync();

        return IslemSonucu.Basari("Evcil hayvan başarıyla kaydedildi.");
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz, string? SilinecekEskiFotograf)> GuncelleAsync(
        EvcilHayvanFormViewModel model,
        string aktifKullaniciId,
        bool adminMi,
        string? yeniFotografYolu)
    {
        var mevcut = await _context.EvcilHayvanlar.FindAsync(model.Id);
        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Güncellenecek evcil hayvan bulunamadı."), false, null);
        }

        if (!adminMi && mevcut.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Bu evcil hayvanı güncelleme yetkiniz yok."), true, null);
        }

        if (model.DogumTarihi > DateOnly.FromDateTime(DateTime.Today))
        {
            return (IslemSonucu.Hata("Doğum tarihi bugünden ileri bir tarih olamaz."), false, null);
        }

        if (adminMi && !string.IsNullOrWhiteSpace(model.SahipId) && model.SahipId != mevcut.SahipId)
        {
            var sahipVarMi = await _context.Users.AnyAsync(u => u.Id == model.SahipId);
            if (!sahipVarMi)
            {
                return (IslemSonucu.Hata("Seçilen hayvan sahibi bulunamadı."), false, null);
            }

            mevcut.SahipId = model.SahipId;
        }

        mevcut.Ad = model.Ad.Trim();
        mevcut.Tur = model.Tur.Trim();
        mevcut.Irk = model.Irk.Trim();
        mevcut.DogumTarihi = model.DogumTarihi;
        mevcut.Kilo = model.Kilo;

        string? silinecekEskiFotograf = null;
        if (!string.IsNullOrEmpty(yeniFotografYolu))
        {
            silinecekEskiFotograf = mevcut.FotografYolu;
            mevcut.FotografYolu = yeniFotografYolu;
        }
        else if (model.FotografiKaldir && !string.IsNullOrEmpty(mevcut.FotografYolu))
        {
            silinecekEskiFotograf = mevcut.FotografYolu;
            mevcut.FotografYolu = null;
        }

        await _context.SaveChangesAsync();
        return (IslemSonucu.Basari("Evcil hayvan bilgileri güncellendi."), false, silinecekEskiFotograf);
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz, string? SilinecekFotograf)> SilAsync(
        int id,
        string aktifKullaniciId,
        bool adminMi)
    {
        var mevcut = await _context.EvcilHayvanlar.FindAsync(id);
        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Silinecek evcil hayvan bulunamadı."), false, null);
        }

        if (!adminMi && mevcut.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Bu evcil hayvanı silme yetkiniz yok."), true, null);
        }

        var randevuVarMi = await _context.Randevular.AnyAsync(r => r.EvcilHayvanId == id);
        if (randevuVarMi)
        {
            return (IslemSonucu.Hata("Bu evcil hayvana ait randevu kayıtları bulunduğu için silinemez."), false, null);
        }

        var eskiFotograf = mevcut.FotografYolu;
        _context.EvcilHayvanlar.Remove(mevcut);
        await _context.SaveChangesAsync();

        return (IslemSonucu.Basari("Evcil hayvan kaydı silindi."), false, eskiFotograf);
    }
}
