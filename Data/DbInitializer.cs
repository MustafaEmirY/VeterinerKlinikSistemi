using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roller = { Roller.Admin, Roller.Veteriner, Roller.Uye };
        foreach (var rol in roller)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
            }
        }

        var adminUser = await KullaniciYoksaOlusturAsync(
            userManager,
            "b251210353@sakarya.edu.tr",
            "Mustafa Emir Yılmaz",
            "sau",
            Roller.Admin);

        var vet1User = await KullaniciYoksaOlusturAsync(
            userManager,
            "dr.selin@kaplanvet.com",
            "Vet. Hek. Selin Arslan",
            "vet123",
            Roller.Veteriner);

        var vet2User = await KullaniciYoksaOlusturAsync(
            userManager,
            "dr.kerem@kaplanvet.com",
            "Vet. Hek. Kerem Demir",
            "vet123",
            Roller.Veteriner);

        var uyeUser = await KullaniciYoksaOlusturAsync(
            userManager,
            "ayse@ornek.com",
            "Ayşe Kaya",
            "uye123",
            Roller.Uye);

        if (!await context.Klinikler.AnyAsync())
        {
            var klinik = new Klinik
            {
                Ad = "Kaplan Veteriner Kliniği",
                Adres = "Kemalpaşa Mah. Üniversite Cad. No:42, Serdivan / Sakarya",
                Telefon = "0264 211 45 60",
                AcilisSaati = new TimeOnly(9, 0),
                KapanisSaati = new TimeOnly(19, 0)
            };
            context.Klinikler.Add(klinik);
            await context.SaveChangesAsync();

            var muayene = new Hizmet
            {
                Ad = "Genel Muayene",
                Aciklama = "Detaylı fiziksel kontrol, ateş ölçümü ve genel sağlık değerlendirmesi",
                SureDakika = 30,
                Ucret = 450m,
                KlinikId = klinik.Id
            };
            var asi = new Hizmet
            {
                Ad = "Aşı Uygulaması",
                Aciklama = "Karma, kuduz ve koruyucu aşı uygulamaları ile aşı karnesi takibi",
                SureDakika = 20,
                Ucret = 350m,
                KlinikId = klinik.Id
            };
            var kisirlastirma = new Hizmet
            {
                Ad = "Kısırlaştırma",
                Aciklama = "Steril ameliyathane ortamında kedi ve köpek kısırlaştırma operasyonu",
                SureDakika = 60,
                Ucret = 2500m,
                KlinikId = klinik.Id
            };
            var disTemizligi = new Hizmet
            {
                Ad = "Diş Temizliği",
                Aciklama = "Ultrasonik diş taşı temizliği ve ağız sağlığı bakımı",
                SureDakika = 45,
                Ucret = 1200m,
                KlinikId = klinik.Id
            };
            var tirasBakim = new Hizmet
            {
                Ad = "Tıraş ve Bakım",
                Aciklama = "Tüy kesimi, banyo, kulak ve tırnak bakımı",
                SureDakika = 45,
                Ucret = 600m,
                KlinikId = klinik.Id
            };

            context.Hizmetler.AddRange(muayene, asi, kisirlastirma, disTemizligi, tirasBakim);
            await context.SaveChangesAsync();

            var vet1 = new Veteriner
            {
                AdSoyad = "Vet. Hek. Selin Arslan",
                UzmanlikAlanlari = "Küçük Hayvan, Cerrahi",
                Eposta = "dr.selin@kaplanvet.com",
                Telefon = "0532 410 11 22",
                KlinikId = klinik.Id,
                KullaniciId = vet1User.Id
            };
            var vet2 = new Veteriner
            {
                AdSoyad = "Vet. Hek. Kerem Demir",
                UzmanlikAlanlari = "Egzotik Hayvan, Dermatoloji, Dahiliye",
                Eposta = "dr.kerem@kaplanvet.com",
                Telefon = "0533 520 33 44",
                KlinikId = klinik.Id,
                KullaniciId = vet2User.Id
            };

            context.Veterinerler.AddRange(vet1, vet2);
            await context.SaveChangesAsync();

            context.VeterinerHizmetler.AddRange(
                new VeterinerHizmet { VeterinerId = vet1.Id, HizmetId = muayene.Id },
                new VeterinerHizmet { VeterinerId = vet1.Id, HizmetId = asi.Id },
                new VeterinerHizmet { VeterinerId = vet1.Id, HizmetId = kisirlastirma.Id },
                new VeterinerHizmet { VeterinerId = vet1.Id, HizmetId = disTemizligi.Id },
                new VeterinerHizmet { VeterinerId = vet2.Id, HizmetId = muayene.Id },
                new VeterinerHizmet { VeterinerId = vet2.Id, HizmetId = asi.Id },
                new VeterinerHizmet { VeterinerId = vet2.Id, HizmetId = disTemizligi.Id },
                new VeterinerHizmet { VeterinerId = vet2.Id, HizmetId = tirasBakim.Id }
            );

            for (int gun = (int)DayOfWeek.Monday; gun <= (int)DayOfWeek.Friday; gun++)
            {
                context.VeterinerMusaitlikler.Add(new VeterinerMusaitlik
                {
                    VeterinerId = vet1.Id,
                    Gun = (DayOfWeek)gun,
                    BaslangicSaati = new TimeOnly(9, 0),
                    BitisSaati = new TimeOnly(17, 0)
                });
            }

            for (int gun = (int)DayOfWeek.Monday; gun <= (int)DayOfWeek.Saturday; gun++)
            {
                context.VeterinerMusaitlikler.Add(new VeterinerMusaitlik
                {
                    VeterinerId = vet2.Id,
                    Gun = (DayOfWeek)gun,
                    BaslangicSaati = new TimeOnly(10, 0),
                    BitisSaati = new TimeOnly(19, 0)
                });
            }

            var pamuk = new EvcilHayvan
            {
                Ad = "Pamuk",
                Tur = "Kedi",
                Irk = "Tekir",
                DogumTarihi = new DateOnly(2023, 5, 14),
                Kilo = 4.20m,
                SahipId = uyeUser.Id
            };
            var zeytin = new EvcilHayvan
            {
                Ad = "Zeytin",
                Tur = "Köpek",
                Irk = "Golden Retriever",
                DogumTarihi = new DateOnly(2022, 3, 10),
                Kilo = 28.50m,
                SahipId = uyeUser.Id
            };
            var limon = new EvcilHayvan
            {
                Ad = "Limon",
                Tur = "Egzotik (Kuş)",
                Irk = "Sultan Papağanı",
                DogumTarihi = new DateOnly(2024, 8, 20),
                Kilo = 0.11m,
                SahipId = uyeUser.Id
            };
            var badem = new EvcilHayvan
            {
                Ad = "Badem",
                Tur = "Egzotik (Tavşan)",
                Irk = "Hollanda Lop",
                DogumTarihi = new DateOnly(2024, 1, 12),
                Kilo = 1.85m,
                SahipId = uyeUser.Id
            };

            context.EvcilHayvanlar.AddRange(pamuk, zeytin, limon, badem);
            await context.SaveChangesAsync();

            context.AsiKayitlari.AddRange(
                new AsiKaydi
                {
                    EvcilHayvanId = pamuk.Id,
                    AsiAdi = "Karma Aşı",
                    YapildigiTarih = new DateOnly(2026, 6, 15),
                    SonrakiDozTarihi = new DateOnly(2026, 12, 15),
                    Notlar = "İlk doz sorunsuz tamamlandı."
                },
                new AsiKaydi
                {
                    EvcilHayvanId = pamuk.Id,
                    AsiAdi = "Kuduz Aşısı",
                    YapildigiTarih = new DateOnly(2026, 2, 10),
                    SonrakiDozTarihi = new DateOnly(2027, 2, 10)
                },
                new AsiKaydi
                {
                    EvcilHayvanId = zeytin.Id,
                    AsiAdi = "Kuduz Aşısı",
                    YapildigiTarih = new DateOnly(2026, 4, 5),
                    SonrakiDozTarihi = new DateOnly(2027, 4, 5)
                },
                new AsiKaydi
                {
                    EvcilHayvanId = badem.Id,
                    AsiAdi = "RHDV Karma Aşı",
                    YapildigiTarih = new DateOnly(2026, 5, 20),
                    SonrakiDozTarihi = new DateOnly(2026, 11, 30),
                    Notlar = "Yıllık koruyucu aşı tekrarı yaklaşmaktadır."
                }
            );

            context.Randevular.Add(new Randevu
            {
                EvcilHayvanId = pamuk.Id,
                VeterinerId = vet1.Id,
                HizmetId = muayene.Id,
                BaslangicZamani = new DateTime(2026, 10, 15, 10, 0, 0),
                SureDakika = muayene.SureDakika,
                Ucret = muayene.Ucret,
                Durum = RandevuDurumu.Tamamlandi,
                MuayeneNotu = "Genel sağlık durumu iyi, vücut ısısı normal, kulak ve diş muayenesi temiz. Aralık ayında karma aşı tekrarı önerildi.",
                OlusturulmaTarihi = new DateTime(2026, 10, 14, 15, 30, 0)
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> KullaniciYoksaOlusturAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string adSoyad,
        string sifre,
        string rol)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                AdSoyad = adSoyad,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, sifre);
            if (!result.Succeeded)
            {
                var hatalar = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Başlangıç kullanıcısı oluşturulamadı ({email}): {hatalar}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, rol))
        {
            await userManager.AddToRoleAsync(user, rol);
        }

        return user;
    }
}
