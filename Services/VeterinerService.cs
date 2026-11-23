using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Services;

public class VeterinerService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VeterinerService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<Veteriner>> TumVeterinerleriGetirAsync(int? klinikId = null, string? arama = null)
    {
        var sorgu = _context.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.VeterinerHizmetler)
                .ThenInclude(vh => vh.Hizmet)
            .Include(v => v.Musaitlikler)
            .AsQueryable();

        if (klinikId.HasValue)
        {
            sorgu = sorgu.Where(v => v.KlinikId == klinikId.Value);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            var terim = arama.Trim();
            sorgu = sorgu.Where(v =>
                v.AdSoyad.Contains(terim) ||
                v.UzmanlikAlanlari.Contains(terim));
        }

        return await sorgu
            .OrderBy(v => v.AdSoyad)
            .ToListAsync();
    }

    public async Task<Veteriner?> IdIleGetirAsync(int id)
    {
        return await _context.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.Kullanici)
            .Include(v => v.VeterinerHizmetler)
                .ThenInclude(vh => vh.Hizmet)
            .Include(v => v.Musaitlikler)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Veteriner?> KullaniciIdIleGetirAsync(string kullaniciId)
    {
        return await _context.Veterinerler
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.KullaniciId == kullaniciId);
    }

    public async Task<IslemSonucu> OlusturAsync(VeterinerFormViewModel model)
    {
        var klinikVarMi = await _context.Klinikler.AnyAsync(k => k.Id == model.KlinikId);
        if (!klinikVarMi)
        {
            return IslemSonucu.Hata("Seçilen klinik bulunamadı.");
        }

        string? kullaniciId = null;

        if (model.HesapOlustur)
        {
            if (string.IsNullOrWhiteSpace(model.Eposta))
            {
                return IslemSonucu.Hata("Veteriner giriş hesabı oluşturmak için e-posta adresi zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.GirisSifresi))
            {
                return IslemSonucu.Hata("Veteriner giriş hesabı için en az 3 karakterlik şifre giriniz.");
            }

            var eposta = model.Eposta.Trim();
            var mevcutKullanici = await _userManager.FindByEmailAsync(eposta);

            if (mevcutKullanici != null)
            {
                var baskaVetBagliMi = await _context.Veterinerler.AnyAsync(v => v.KullaniciId == mevcutKullanici.Id);
                if (baskaVetBagliMi)
                {
                    return IslemSonucu.Hata("Bu e-posta adresi başka bir veteriner hekime zaten bağlı.");
                }

                if (!await _userManager.IsInRoleAsync(mevcutKullanici, Roller.Veteriner))
                {
                    await _userManager.AddToRoleAsync(mevcutKullanici, Roller.Veteriner);
                }

                kullaniciId = mevcutKullanici.Id;
            }
            else
            {
                var yeniKullanici = new ApplicationUser
                {
                    UserName = eposta,
                    Email = eposta,
                    AdSoyad = model.AdSoyad.Trim(),
                    EmailConfirmed = true
                };

                var olusturma = await _userManager.CreateAsync(yeniKullanici, model.GirisSifresi);
                if (!olusturma.Succeeded)
                {
                    var hatalar = string.Join(" ", olusturma.Errors.Select(e => e.Description));
                    return IslemSonucu.Hata($"Giriş hesabı oluşturulamadı: {hatalar}");
                }

                await _userManager.AddToRoleAsync(yeniKullanici, Roller.Veteriner);
                kullaniciId = yeniKullanici.Id;
            }
        }

        var gecerliHizmetIdleri = await _context.Hizmetler
            .Where(h => h.KlinikId == model.KlinikId && model.SeciliHizmetIdleri.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();

        var veteriner = new Veteriner
        {
            AdSoyad = model.AdSoyad.Trim(),
            UzmanlikAlanlari = model.UzmanlikAlanlari.Trim(),
            Eposta = string.IsNullOrWhiteSpace(model.Eposta) ? null : model.Eposta.Trim(),
            Telefon = string.IsNullOrWhiteSpace(model.Telefon) ? null : model.Telefon.Trim(),
            KlinikId = model.KlinikId,
            KullaniciId = kullaniciId
        };

        foreach (var hizmetId in gecerliHizmetIdleri)
        {
            veteriner.VeterinerHizmetler.Add(new VeterinerHizmet
            {
                HizmetId = hizmetId
            });
        }

        _context.Veterinerler.Add(veteriner);
        await _context.SaveChangesAsync();

        return IslemSonucu.Basari("Veteriner hekim başarıyla eklendi.");
    }

    public async Task<IslemSonucu> GuncelleAsync(VeterinerFormViewModel model)
    {
        var mevcut = await _context.Veterinerler
            .Include(v => v.VeterinerHizmetler)
            .FirstOrDefaultAsync(v => v.Id == model.Id);

        if (mevcut == null)
        {
            return IslemSonucu.Hata("Güncellenecek veteriner hekim bulunamadı.");
        }

        var klinikVarMi = await _context.Klinikler.AnyAsync(k => k.Id == model.KlinikId);
        if (!klinikVarMi)
        {
            return IslemSonucu.Hata("Seçilen klinik bulunamadı.");
        }

        mevcut.AdSoyad = model.AdSoyad.Trim();
        mevcut.UzmanlikAlanlari = model.UzmanlikAlanlari.Trim();
        mevcut.Eposta = string.IsNullOrWhiteSpace(model.Eposta) ? null : model.Eposta.Trim();
        mevcut.Telefon = string.IsNullOrWhiteSpace(model.Telefon) ? null : model.Telefon.Trim();
        mevcut.KlinikId = model.KlinikId;

        var gecerliHizmetIdleri = await _context.Hizmetler
            .Where(h => h.KlinikId == model.KlinikId && model.SeciliHizmetIdleri.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();

        var silinecekler = mevcut.VeterinerHizmetler
            .Where(vh => !gecerliHizmetIdleri.Contains(vh.HizmetId))
            .ToList();

        foreach (var silinecek in silinecekler)
        {
            mevcut.VeterinerHizmetler.Remove(silinecek);
        }

        var mevcutHizmetIdleri = mevcut.VeterinerHizmetler.Select(vh => vh.HizmetId).ToHashSet();
        foreach (var hizmetId in gecerliHizmetIdleri)
        {
            if (!mevcutHizmetIdleri.Contains(hizmetId))
            {
                mevcut.VeterinerHizmetler.Add(new VeterinerHizmet
                {
                    VeterinerId = mevcut.Id,
                    HizmetId = hizmetId
                });
            }
        }

        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Veteriner hekim bilgileri güncellendi.");
    }

    public async Task<IslemSonucu> SilAsync(int id)
    {
        var mevcut = await _context.Veterinerler.FindAsync(id);
        if (mevcut == null)
        {
            return IslemSonucu.Hata("Silinecek veteriner hekim bulunamadı.");
        }

        var randevuVarMi = await _context.Randevular.AnyAsync(r => r.VeterinerId == id);
        if (randevuVarMi)
        {
            return IslemSonucu.Hata("Bu veteriner hekime ait randevu kayıtları bulunduğu için silinemez.");
        }

        _context.Veterinerler.Remove(mevcut);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Veteriner hekim kaydı silindi.");
    }

    public async Task<IslemSonucu> MusaitlikEkleAsync(MusaitlikEkleViewModel model)
    {
        var vet = await _context.Veterinerler
            .Include(v => v.Klinik)
            .Include(v => v.Musaitlikler)
            .FirstOrDefaultAsync(v => v.Id == model.VeterinerId);

        if (vet == null)
        {
            return IslemSonucu.Hata("Veteriner hekim bulunamadı.");
        }

        if (model.BitisSaati <= model.BaslangicSaati)
        {
            return IslemSonucu.Hata("Bitiş saati başlangıç saatinden sonra olmalıdır.");
        }

        if (vet.Klinik != null &&
            (model.BaslangicSaati < vet.Klinik.AcilisSaati || model.BitisSaati > vet.Klinik.KapanisSaati))
        {
            return IslemSonucu.Hata(
                $"Müsaitlik saatleri kliniğin çalışma saatleri ({vet.Klinik.AcilisSaati:HH:mm} – {vet.Klinik.KapanisSaati:HH:mm}) içinde olmalıdır.");
        }

        var cakisiyorMu = vet.Musaitlikler.Any(m =>
            m.Gun == model.Gun &&
            model.BaslangicSaati < m.BitisSaati &&
            model.BitisSaati > m.BaslangicSaati);

        if (cakisiyorMu)
        {
            return IslemSonucu.Hata("Seçilen gün ve saat aralığı mevcut bir müsaitlik kaydıyla çakışıyor.");
        }

        _context.VeterinerMusaitlikler.Add(new VeterinerMusaitlik
        {
            VeterinerId = vet.Id,
            Gun = model.Gun,
            BaslangicSaati = model.BaslangicSaati,
            BitisSaati = model.BitisSaati
        });

        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Müsaitlik saati eklendi.");
    }

    public async Task<IslemSonucu> MusaitlikSilAsync(int musaitlikId, int veterinerId)
    {
        var kayit = await _context.VeterinerMusaitlikler
            .FirstOrDefaultAsync(m => m.Id == musaitlikId && m.VeterinerId == veterinerId);

        if (kayit == null)
        {
            return IslemSonucu.Hata("Silinecek müsaitlik kaydı bulunamadı.");
        }

        _context.VeterinerMusaitlikler.Remove(kayit);
        await _context.SaveChangesAsync();
        return IslemSonucu.Basari("Müsaitlik saati silindi.");
    }
}
