using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.DTOs;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Services;

public class RandevuService
{
    private readonly ApplicationDbContext _context;

    public RandevuService(ApplicationDbContext context)
    {
        _context = context;
    }

    public static string DurumMetni(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Bekliyor => "Bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        RandevuDurumu.IptalEdildi => "İptal Edildi",
        _ => durum.ToString()
    };

    public static string DurumRozetSinifi(RandevuDurumu durum) => durum switch
    {
        RandevuDurumu.Bekliyor => "bg-warning-subtle text-warning-emphasis border border-warning-subtle",
        RandevuDurumu.Onaylandi => "bg-success-subtle text-success border border-success-subtle",
        RandevuDurumu.Reddedildi => "bg-danger-subtle text-danger border border-danger-subtle",
        RandevuDurumu.Tamamlandi => "pill-tag",
        RandevuDurumu.IptalEdildi => "bg-light text-secondary border",
        _ => "bg-light text-secondary border"
    };

    public async Task<List<Randevu>> ListeleAsync(
        string kullaniciId,
        bool adminMi,
        bool veterinerMi,
        RandevuDurumu? durum = null)
    {
        var sorgu = _context.Randevular
            .AsNoTracking()
            .Include(r => r.EvcilHayvan)
                .ThenInclude(e => e!.Sahip)
            .Include(r => r.Veteriner)
                .ThenInclude(v => v!.Klinik)
            .Include(r => r.Hizmet)
            .AsQueryable();

        if (!adminMi)
        {
            if (veterinerMi)
            {
                sorgu = sorgu.Where(r => r.Veteriner != null && r.Veteriner.KullaniciId == kullaniciId);
            }
            else
            {
                sorgu = sorgu.Where(r => r.EvcilHayvan != null && r.EvcilHayvan.SahipId == kullaniciId);
            }
        }

        if (durum.HasValue)
        {
            sorgu = sorgu.Where(r => r.Durum == durum.Value);
        }

        return await sorgu
            .OrderByDescending(r => r.BaslangicZamani)
            .ToListAsync();
    }

    public async Task<(Randevu? Kayit, bool Yetkisiz)> ErisimKontrolluGetirAsync(
        int id,
        string kullaniciId,
        bool adminMi,
        bool veterinerMi)
    {
        var kayit = await _context.Randevular
            .AsNoTracking()
            .Include(r => r.EvcilHayvan)
                .ThenInclude(e => e!.Sahip)
            .Include(r => r.Veteriner)
                .ThenInclude(v => v!.Klinik)
            .Include(r => r.Hizmet)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (kayit == null)
        {
            return (null, false);
        }

        if (!YetkiliMi(kayit, kullaniciId, adminMi, veterinerMi))
        {
            return (null, true);
        }

        return (kayit, false);
    }

    public async Task<List<UygunHekimSaatleriDto>> UygunSaatleriGetirAsync(
        int hizmetId,
        DateOnly tarih,
        int? haricRandevuId = null)
    {
        var hizmet = await _context.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .FirstOrDefaultAsync(h => h.Id == hizmetId);

        if (hizmet == null)
        {
            return new List<UygunHekimSaatleriDto>();
        }

        var gun = tarih.DayOfWeek;
        var gunBaslangic = tarih.ToDateTime(TimeOnly.MinValue);
        var gunBitis = gunBaslangic.AddDays(1);

        var veterinerler = await _context.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.Musaitlikler)
            .Where(v =>
                v.VeterinerHizmetler.Any(vh => vh.HizmetId == hizmetId) &&
                v.Musaitlikler.Any(m => m.Gun == gun))
            .OrderBy(v => v.AdSoyad)
            .ToListAsync();

        var veterinerIdleri = veterinerler.Select(v => v.Id).ToList();

        // Reddedilen ve iptal edilen randevular saati bloklamaz
        var doluRandevular = await _context.Randevular
            .AsNoTracking()
            .Where(r =>
                veterinerIdleri.Contains(r.VeterinerId) &&
                r.BaslangicZamani >= gunBaslangic &&
                r.BaslangicZamani < gunBitis &&
                r.Durum != RandevuDurumu.Reddedildi &&
                r.Durum != RandevuDurumu.IptalEdildi &&
                (!haricRandevuId.HasValue || r.Id != haricRandevuId.Value))
            .ToListAsync();

        var sonuc = new List<UygunHekimSaatleriDto>();
        var simdi = DateTime.Now;
        const int adimDakika = 30;

        foreach (var vet in veterinerler)
        {
            var vetRandevulari = doluRandevular
                .Where(r => r.VeterinerId == vet.Id)
                .ToList();

            var gunMusaitlikleri = vet.Musaitlikler
                .Where(m => m.Gun == gun)
                .OrderBy(m => m.BaslangicSaati)
                .ToList();

            var uygunSaatler = new List<string>();

            foreach (var blok in gunMusaitlikleri)
            {
                var etkinBaslangic = vet.Klinik != null && blok.BaslangicSaati < vet.Klinik.AcilisSaati
                    ? vet.Klinik.AcilisSaati
                    : blok.BaslangicSaati;
                var etkinBitis = vet.Klinik != null && blok.BitisSaati > vet.Klinik.KapanisSaati
                    ? vet.Klinik.KapanisSaati
                    : blok.BitisSaati;

                var adayBaslangicDt = tarih.ToDateTime(etkinBaslangic);
                var blokBitisDt = tarih.ToDateTime(etkinBitis);

                while (adayBaslangicDt.AddMinutes(hizmet.SureDakika) <= blokBitisDt)
                {
                    var adayBitisDt = adayBaslangicDt.AddMinutes(hizmet.SureDakika);

                    if (adayBaslangicDt > simdi)
                    {
                        var cakisiyor = vetRandevulari.Any(r =>
                        {
                            var mevcutBaslangic = r.BaslangicZamani;
                            var mevcutBitis = r.BaslangicZamani.AddMinutes(r.SureDakika);
                            return adayBaslangicDt < mevcutBitis && adayBitisDt > mevcutBaslangic;
                        });

                        if (!cakisiyor)
                        {
                            var saatMetni = adayBaslangicDt.ToString("HH:mm");
                            if (!uygunSaatler.Contains(saatMetni))
                            {
                                uygunSaatler.Add(saatMetni);
                            }
                        }
                    }

                    adayBaslangicDt = adayBaslangicDt.AddMinutes(adimDakika);
                }
            }

            if (uygunSaatler.Any())
            {
                sonuc.Add(new UygunHekimSaatleriDto
                {
                    VeterinerId = vet.Id,
                    VeterinerAdSoyad = vet.AdSoyad,
                    UzmanlikAlanlari = vet.UzmanlikAlanlari,
                    KlinikAdi = vet.Klinik?.Ad ?? string.Empty,
                    UygunSaatler = uygunSaatler
                });
            }
        }

        return sonuc;
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz, Randevu? Randevu)> OlusturAsync(
        int evcilHayvanId,
        int hizmetId,
        int veterinerId,
        DateTime baslangicZamani,
        string aktifKullaniciId,
        bool adminMi)
    {
        var hayvan = await _context.EvcilHayvanlar.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evcilHayvanId);
        if (hayvan == null)
        {
            return (IslemSonucu.Hata("Seçilen evcil hayvan bulunamadı."), false, null);
        }

        if (!adminMi && hayvan.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Başka bir üyeye ait evcil hayvan için randevu oluşturamazsınız."), true, null);
        }

        var (uygunMu, hataMesaji, hizmet) = await RandevuUygunlugunuKontrolEtAsync(
            evcilHayvanId,
            hizmetId,
            veterinerId,
            baslangicZamani,
            haricRandevuId: null);

        if (!uygunMu || hizmet == null)
        {
            return (IslemSonucu.Hata(hataMesaji ?? "Randevu zamanı uygun değil."), false, null);
        }

        var randevu = new Randevu
        {
            EvcilHayvanId = evcilHayvanId,
            HizmetId = hizmetId,
            VeterinerId = veterinerId,
            BaslangicZamani = baslangicZamani,
            SureDakika = hizmet.SureDakika,
            Ucret = hizmet.Ucret,
            Durum = RandevuDurumu.Bekliyor,
            OlusturulmaTarihi = DateTime.Now
        };

        _context.Randevular.Add(randevu);
        await _context.SaveChangesAsync();

        return (IslemSonucu.Basari("Randevunuz 'Bekliyor' durumunda oluşturuldu. Veteriner veya Admin onayı sonrası kesinleşecektir."), false, randevu);
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz)> GuncelleAsync(
        int randevuId,
        int evcilHayvanId,
        int hizmetId,
        int veterinerId,
        DateTime baslangicZamani,
        string aktifKullaniciId,
        bool adminMi)
    {
        var mevcut = await _context.Randevular
            .Include(r => r.EvcilHayvan)
            .FirstOrDefaultAsync(r => r.Id == randevuId);

        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Güncellenecek randevu bulunamadı."), false);
        }

        if (!adminMi && mevcut.EvcilHayvan?.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Bu randevuyu güncelleme yetkiniz yok."), true);
        }

        if (!adminMi && mevcut.Durum != RandevuDurumu.Bekliyor)
        {
            return (IslemSonucu.Hata("Yalnızca 'Bekliyor' durumundaki randevular düzenlenebilir."), false);
        }

        var yeniHayvan = await _context.EvcilHayvanlar.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evcilHayvanId);
        if (yeniHayvan == null)
        {
            return (IslemSonucu.Hata("Seçilen evcil hayvan bulunamadı."), false);
        }

        if (!adminMi && yeniHayvan.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Başka bir üyeye ait evcil hayvan seçemezsiniz."), true);
        }

        var (uygunMu, hataMesaji, hizmet) = await RandevuUygunlugunuKontrolEtAsync(
            evcilHayvanId,
            hizmetId,
            veterinerId,
            baslangicZamani,
            haricRandevuId: randevuId);

        if (!uygunMu || hizmet == null)
        {
            return (IslemSonucu.Hata(hataMesaji ?? "Randevu zamanı uygun değil."), false);
        }

        mevcut.EvcilHayvanId = evcilHayvanId;
        mevcut.HizmetId = hizmetId;
        mevcut.VeterinerId = veterinerId;
        mevcut.BaslangicZamani = baslangicZamani;
        mevcut.SureDakika = hizmet.SureDakika;
        mevcut.Ucret = hizmet.Ucret;

        await _context.SaveChangesAsync();
        return (IslemSonucu.Basari("Randevu bilgileri güncellendi."), false);
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz)> DurumGuncelleAsync(
        int randevuId,
        RandevuDurumu yeniDurum,
        string aktifKullaniciId,
        bool adminMi,
        bool veterinerMi)
    {
        var mevcut = await _context.Randevular
            .Include(r => r.EvcilHayvan)
            .Include(r => r.Veteriner)
            .FirstOrDefaultAsync(r => r.Id == randevuId);

        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Randevu bulunamadı."), false);
        }

        var kendiVeterineriMi = veterinerMi && mevcut.Veteriner?.KullaniciId == aktifKullaniciId;
        var kendiSahibiMi = mevcut.EvcilHayvan?.SahipId == aktifKullaniciId;

        if (yeniDurum is RandevuDurumu.Onaylandi or RandevuDurumu.Reddedildi)
        {
            if (!adminMi && !kendiVeterineriMi)
            {
                return (IslemSonucu.Hata("Bu randevuyu onaylama veya reddetme yetkiniz yok."), true);
            }

            if (mevcut.Durum != RandevuDurumu.Bekliyor)
            {
                return (IslemSonucu.Hata("Yalnızca 'Bekliyor' durumundaki randevular onaylanabilir veya reddedilebilir."), false);
            }
        }
        else if (yeniDurum == RandevuDurumu.IptalEdildi)
        {
            if (!adminMi && !kendiVeterineriMi && !kendiSahibiMi)
            {
                return (IslemSonucu.Hata("Bu randevuyu iptal etme yetkiniz yok."), true);
            }

            if (mevcut.Durum is RandevuDurumu.Tamamlandi or RandevuDurumu.Reddedildi or RandevuDurumu.IptalEdildi)
            {
                return (IslemSonucu.Hata("Bu durumdaki randevu iptal edilemez."), false);
            }
        }

        mevcut.Durum = yeniDurum;
        await _context.SaveChangesAsync();

        var mesaj = yeniDurum switch
        {
            RandevuDurumu.Onaylandi => "Randevu onaylandı.",
            RandevuDurumu.Reddedildi => "Randevu reddedildi.",
            RandevuDurumu.IptalEdildi => "Randevu iptal edildi.",
            _ => "Randevu durumu güncellendi."
        };

        return (IslemSonucu.Basari(mesaj), false);
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz)> MuayeneNotuKaydetAsync(
        int randevuId,
        string muayeneNotu,
        string aktifKullaniciId,
        bool adminMi,
        bool veterinerMi)
    {
        var mevcut = await _context.Randevular
            .Include(r => r.Veteriner)
            .FirstOrDefaultAsync(r => r.Id == randevuId);

        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Randevu bulunamadı."), false);
        }

        var kendiVeterineriMi = veterinerMi && mevcut.Veteriner?.KullaniciId == aktifKullaniciId;
        if (!adminMi && !kendiVeterineriMi)
        {
            return (IslemSonucu.Hata("Bu randevu için muayene notu girme yetkiniz yok."), true);
        }

        if (mevcut.Durum is RandevuDurumu.Reddedildi or RandevuDurumu.IptalEdildi)
        {
            return (IslemSonucu.Hata("Reddedilmiş veya iptal edilmiş randevuya muayene notu girilemez."), false);
        }

        if (string.IsNullOrWhiteSpace(muayeneNotu))
        {
            return (IslemSonucu.Hata("Muayene notu boş bırakılamaz."), false);
        }

        mevcut.MuayeneNotu = muayeneNotu.Trim();
        mevcut.Durum = RandevuDurumu.Tamamlandi;

        await _context.SaveChangesAsync();
        return (IslemSonucu.Basari("Muayene notu kaydedildi ve randevu 'Tamamlandı' olarak işaretlendi."), false);
    }

    public async Task<(IslemSonucu Sonuc, bool Yetkisiz)> SilAsync(
        int id,
        string aktifKullaniciId,
        bool adminMi)
    {
        var mevcut = await _context.Randevular
            .Include(r => r.EvcilHayvan)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (mevcut == null)
        {
            return (IslemSonucu.Hata("Silinecek randevu bulunamadı."), false);
        }

        if (!adminMi && mevcut.EvcilHayvan?.SahipId != aktifKullaniciId)
        {
            return (IslemSonucu.Hata("Bu randevuyu silme yetkiniz yok."), true);
        }

        if (!adminMi && mevcut.Durum == RandevuDurumu.Tamamlandi)
        {
            return (IslemSonucu.Hata("Tamamlanmış muayene kaydı silinemez."), false);
        }

        _context.Randevular.Remove(mevcut);
        await _context.SaveChangesAsync();
        return (IslemSonucu.Basari("Randevu kaydı silindi."), false);
    }

    public async Task<List<MuayeneGecmisiDto>?> MuayeneGecmisiniGetirAsync(string hayvanAdi, string sahipId)
    {
        var aranan = hayvanAdi.Trim().ToLower();
        var hayvan = await _context.EvcilHayvanlar
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SahipId == sahipId && e.Ad.ToLower() == aranan);

        if (hayvan == null)
        {
            return null;
        }

        return await _context.Randevular
            .AsNoTracking()
            .Include(r => r.Hizmet)
            .Include(r => r.Veteriner)
            .Where(r => r.EvcilHayvanId == hayvan.Id && r.Durum == RandevuDurumu.Tamamlandi)
            .OrderByDescending(r => r.BaslangicZamani)
            .Select(r => new MuayeneGecmisiDto
            {
                RandevuId = r.Id,
                Tarih = r.BaslangicZamani,
                HizmetAdi = r.Hizmet != null ? r.Hizmet.Ad : string.Empty,
                VeterinerAdi = r.Veteriner != null ? r.Veteriner.AdSoyad : string.Empty,
                MuayeneNotu = r.MuayeneNotu
            })
            .ToListAsync();
    }

    private async Task<(bool Uygun, string? Mesaj, Hizmet? Hizmet)> RandevuUygunlugunuKontrolEtAsync(
        int evcilHayvanId,
        int hizmetId,
        int veterinerId,
        DateTime baslangicZamani,
        int? haricRandevuId)
    {
        if (baslangicZamani <= DateTime.Now)
        {
            return (false, "Geçmiş bir tarih veya saate randevu oluşturulamaz.", null);
        }

        var hizmet = await _context.Hizmetler
            .AsNoTracking()
            .Include(h => h.Klinik)
            .FirstOrDefaultAsync(h => h.Id == hizmetId);

        if (hizmet == null)
        {
            return (false, "Seçilen hizmet bulunamadı.", null);
        }

        var veteriner = await _context.Veterinerler
            .AsNoTracking()
            .Include(v => v.Klinik)
            .Include(v => v.VeterinerHizmetler)
            .Include(v => v.Musaitlikler)
            .FirstOrDefaultAsync(v => v.Id == veterinerId);

        if (veteriner == null)
        {
            return (false, "Seçilen veteriner hekim bulunamadı.", null);
        }

        if (!veteriner.VeterinerHizmetler.Any(vh => vh.HizmetId == hizmetId))
        {
            return (false, $"{veteriner.AdSoyad}, '{hizmet.Ad}' hizmetini vermemektedir.", null);
        }

        var bitisZamani = baslangicZamani.AddMinutes(hizmet.SureDakika);
        if (bitisZamani.Date != baslangicZamani.Date)
        {
            return (false, "Randevu bitiş saati gün sonunu aşamaz.", null);
        }

        var baslangicSaat = TimeOnly.FromDateTime(baslangicZamani);
        var bitisSaat = TimeOnly.FromDateTime(bitisZamani);

        if (veteriner.Klinik != null &&
            (baslangicSaat < veteriner.Klinik.AcilisSaati || bitisSaat > veteriner.Klinik.KapanisSaati))
        {
            return (false,
                $"Randevu ({baslangicSaat:HH:mm} – {bitisSaat:HH:mm}), kliniğin çalışma saatleri ({veteriner.Klinik.AcilisSaati:HH:mm} – {veteriner.Klinik.KapanisSaati:HH:mm}) dışındadır.",
                null);
        }

        var gun = baslangicZamani.DayOfWeek;
        var musaitlikUygun = veteriner.Musaitlikler.Any(m =>
            m.Gun == gun &&
            baslangicSaat >= m.BaslangicSaati &&
            bitisSaat <= m.BitisSaati);

        if (!musaitlikUygun)
        {
            return (false,
                $"{veteriner.AdSoyad}, {GunYardimcisi.TurkceAd(gun)} günü {baslangicSaat:HH:mm} – {bitisSaat:HH:mm} ({hizmet.SureDakika} dk) aralığında müsait değildir.",
                null);
        }

        var gunBaslangic = baslangicZamani.Date;
        var gunBitis = gunBaslangic.AddDays(1);

        // Reddedilen ve iptal edilen randevular hariç; arka arkaya randevular için katı < ve > kullanılır
        var vetGunRandevulari = await _context.Randevular
            .AsNoTracking()
            .Where(r =>
                r.VeterinerId == veterinerId &&
                r.BaslangicZamani >= gunBaslangic &&
                r.BaslangicZamani < gunBitis &&
                r.Durum != RandevuDurumu.Reddedildi &&
                r.Durum != RandevuDurumu.IptalEdildi &&
                (!haricRandevuId.HasValue || r.Id != haricRandevuId.Value))
            .ToListAsync();

        var cakisanVetRandevu = vetGunRandevulari.FirstOrDefault(r =>
        {
            var mevcutBaslangic = r.BaslangicZamani;
            var mevcutBitis = r.BaslangicZamani.AddMinutes(r.SureDakika);
            return baslangicZamani < mevcutBitis && bitisZamani > mevcutBaslangic;
        });

        if (cakisanVetRandevu != null)
        {
            var cakisBitis = cakisanVetRandevu.BaslangicZamani.AddMinutes(cakisanVetRandevu.SureDakika);
            return (false,
                $"{veteriner.AdSoyad} için {cakisanVetRandevu.BaslangicZamani:HH:mm} – {cakisBitis:HH:mm} saatleri arasında başka bir randevu bulunduğu için çakışma oluştu.",
                null);
        }

        var hayvanGunRandevulari = await _context.Randevular
            .AsNoTracking()
            .Where(r =>
                r.EvcilHayvanId == evcilHayvanId &&
                r.BaslangicZamani >= gunBaslangic &&
                r.BaslangicZamani < gunBitis &&
                r.Durum != RandevuDurumu.Reddedildi &&
                r.Durum != RandevuDurumu.IptalEdildi &&
                (!haricRandevuId.HasValue || r.Id != haricRandevuId.Value))
            .ToListAsync();

        var cakisanHayvanRandevu = hayvanGunRandevulari.FirstOrDefault(r =>
        {
            var mevcutBaslangic = r.BaslangicZamani;
            var mevcutBitis = r.BaslangicZamani.AddMinutes(r.SureDakika);
            return baslangicZamani < mevcutBitis && bitisZamani > mevcutBaslangic;
        });

        if (cakisanHayvanRandevu != null)
        {
            var cakisBitis = cakisanHayvanRandevu.BaslangicZamani.AddMinutes(cakisanHayvanRandevu.SureDakika);
            return (false,
                $"Seçilen evcil hayvanın {cakisanHayvanRandevu.BaslangicZamani:HH:mm} – {cakisBitis:HH:mm} aralığında zaten başka bir randevusu bulunmaktadır.",
                null);
        }

        return (true, null, hizmet);
    }

    private static bool YetkiliMi(Randevu randevu, string kullaniciId, bool adminMi, bool veterinerMi)
    {
        if (adminMi)
        {
            return true;
        }

        if (veterinerMi)
        {
            return randevu.Veteriner?.KullaniciId == kullaniciId;
        }

        return randevu.EvcilHayvan?.SahipId == kullaniciId;
    }
}
