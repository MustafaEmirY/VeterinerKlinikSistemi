using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using ModelContextProtocol.Server;
using VeterinerKlinikSistemi.Services;

namespace VeterinerKlinikSistemi.Mcp;

[McpServerToolType]
public class VeterinerMcpTools
{
    private static readonly JsonSerializerOptions JsonAyarlari = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RandevuService _randevuService;
    private readonly AsiService _asiService;
    private readonly EvcilHayvanService _evcilHayvanService;
    private readonly HizmetService _hizmetService;

    public VeterinerMcpTools(
        IHttpContextAccessor httpContextAccessor,
        RandevuService randevuService,
        AsiService asiService,
        EvcilHayvanService evcilHayvanService,
        HizmetService hizmetService)
    {
        _httpContextAccessor = httpContextAccessor;
        _randevuService = randevuService;
        _asiService = asiService;
        _evcilHayvanService = evcilHayvanService;
        _hizmetService = hizmetService;
    }

    [McpServerTool(Name = "uygun_saatler")]
    [Description("Seçilen hizmet ve tarih için (yyyy-MM-dd formatında) uygun veteriner hekimleri ve boş randevu saatlerini listeler.")]
    public async Task<string> UygunSaatlerAsync(
        [Description("Hizmet numarası (örnek: 1 = Genel Muayene, 2 = Aşı Uygulaması, 3 = Diş Bakımı)")] int hizmetId,
        [Description("Randevu tarihi (yyyy-MM-dd formatında, örnek: 2026-12-01)")] string tarih)
    {
        var kullaniciId = AktifKullaniciIdGetir();
        if (kullaniciId == null)
        {
            return JsonYaz(new { basarili = false, mesaj = "Kimlik doğrulaması gerekli." });
        }

        if (!TarihCozumle(tarih, out var parsedTarih))
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = "Geçersiz tarih formatı. Lütfen yyyy-MM-dd formatında bir tarih girin (örnek: 2026-12-01)."
            });
        }

        var hizmet = await _hizmetService.IdIleGetirAsync(hizmetId);
        if (hizmet == null)
        {
            var mevcutHizmetler = await _hizmetService.TumHizmetleriGetirAsync();
            return JsonYaz(new
            {
                basarili = false,
                mesaj = $"{hizmetId} numaralı hizmet bulunamadı.",
                mevcutHizmetler = mevcutHizmetler.Select(h => new
                {
                    h.Id,
                    h.Ad,
                    h.SureDakika,
                    h.Ucret
                })
            });
        }

        var uygunHekimler = await _randevuService.UygunSaatleriGetirAsync(hizmetId, parsedTarih);

        if (uygunHekimler.Count == 0)
        {
            return JsonYaz(new
            {
                basarili = true,
                hizmet = new { hizmet.Id, hizmet.Ad, hizmet.SureDakika, hizmet.Ucret },
                tarih = parsedTarih.ToString("yyyy-MM-dd"),
                uygunHekimler = Array.Empty<object>(),
                mesaj = "Seçilen tarih için uygun veteriner hekim veya boş saat bulunamadı."
            });
        }

        return JsonYaz(new
        {
            basarili = true,
            hizmet = new { hizmet.Id, hizmet.Ad, hizmet.SureDakika, hizmet.Ucret },
            tarih = parsedTarih.ToString("yyyy-MM-dd"),
            uygunHekimler
        });
    }

    [McpServerTool(Name = "asi_takvimi")]
    [Description("Yalnızca giriş yapan kullanıcıya ait belirtilen evcil hayvanın geçmiş aşılarını ve yaklaşan doz tarihlerini döndürür.")]
    public async Task<string> AsiTakvimiAsync(
        [Description("Evcil hayvanın adı (örnek: Pamuk, Zeytin, Limon, Badem)")] string hayvanAdi)
    {
        var kullaniciId = AktifKullaniciIdGetir();
        if (kullaniciId == null)
        {
            return JsonYaz(new { basarili = false, mesaj = "Kimlik doğrulaması gerekli." });
        }

        if (string.IsNullOrWhiteSpace(hayvanAdi))
        {
            return JsonYaz(new { basarili = false, mesaj = "Lütfen bir evcil hayvan adı belirtin." });
        }

        var takvim = await _asiService.AdaGoreAsiTakvimiGetirAsync(hayvanAdi, kullaniciId);
        if (takvim == null)
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = $"Size ait '{hayvanAdi.Trim()}' adlı bir evcil hayvan bulunamadı."
            });
        }

        return JsonYaz(new
        {
            basarili = true,
            takvim
        });
    }

    [McpServerTool(Name = "randevu_olustur")]
    [Description("Giriş yapan kullanıcının kendi evcil hayvanı için çakışma ve müsaitlik kontrolü yaparak 'Bekliyor' durumunda yeni randevu oluşturur.")]
    public async Task<string> RandevuOlusturAsync(
        [Description("Kullanıcıya ait evcil hayvanın adı (örnek: Pamuk)")] string hayvanAdi,
        [Description("Alınacak hizmetin numarası (örnek: 1)")] int hizmetId,
        [Description("Veteriner hekim numarası (örnek: 1)")] int veterinerId,
        [Description("Randevu tarihi (yyyy-MM-dd formatında, örnek: 2026-12-01)")] string tarih,
        [Description("Randevu başlangıç saati (HH:mm formatında, örnek: 10:00)")] string saat)
    {
        var kullaniciId = AktifKullaniciIdGetir();
        if (kullaniciId == null)
        {
            return JsonYaz(new { basarili = false, mesaj = "Kimlik doğrulaması gerekli." });
        }

        if (string.IsNullOrWhiteSpace(hayvanAdi))
        {
            return JsonYaz(new { basarili = false, mesaj = "Lütfen bir evcil hayvan adı belirtin." });
        }

        if (!TarihCozumle(tarih, out var parsedTarih))
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = "Geçersiz tarih formatı. Lütfen yyyy-MM-dd formatında bir tarih girin (örnek: 2026-12-01)."
            });
        }

        if (!TimeOnly.TryParseExact(saat.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedSaat))
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = "Geçersiz saat formatı. Lütfen HH:mm formatında bir saat girin (örnek: 10:00)."
            });
        }

        var hayvan = await _evcilHayvanService.AdaGoreSahipHayvaniGetirAsync(hayvanAdi, kullaniciId);
        if (hayvan == null)
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = $"Size ait '{hayvanAdi.Trim()}' adlı bir evcil hayvan bulunamadı."
            });
        }

        var baslangicZamani = parsedTarih.ToDateTime(parsedSaat);

        // MCP üzerinden oluşturulan randevularda yalnızca kullanıcının kendi hayvanına izin verilir
        var (sonuc, _, olusturulan) = await _randevuService.OlusturAsync(
            hayvan.Id,
            hizmetId,
            veterinerId,
            baslangicZamani,
            kullaniciId,
            adminMi: false);

        if (!sonuc.Basarili || olusturulan == null)
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = sonuc.Mesaj
            });
        }

        return JsonYaz(new
        {
            basarili = true,
            mesaj = sonuc.Mesaj,
            randevu = new
            {
                olusturulan.Id,
                evcilHayvan = hayvan.Ad,
                olusturulan.HizmetId,
                olusturulan.VeterinerId,
                baslangicZamani = olusturulan.BaslangicZamani.ToString("yyyy-MM-dd HH:mm"),
                olusturulan.SureDakika,
                olusturulan.Ucret,
                durum = RandevuService.DurumMetni(olusturulan.Durum)
            }
        });
    }

    [McpServerTool(Name = "muayene_gecmisi")]
    [Description("Yalnızca giriş yapan kullanıcıya ait belirtilen evcil hayvanın tamamlanmış muayene kayıtlarını ve veteriner muayene notlarını döndürür.")]
    public async Task<string> MuayeneGecmisiAsync(
        [Description("Evcil hayvanın adı (örnek: Pamuk)")] string hayvanAdi)
    {
        var kullaniciId = AktifKullaniciIdGetir();
        if (kullaniciId == null)
        {
            return JsonYaz(new { basarili = false, mesaj = "Kimlik doğrulaması gerekli." });
        }

        if (string.IsNullOrWhiteSpace(hayvanAdi))
        {
            return JsonYaz(new { basarili = false, mesaj = "Lütfen bir evcil hayvan adı belirtin." });
        }

        var gecmis = await _randevuService.MuayeneGecmisiniGetirAsync(hayvanAdi, kullaniciId);
        if (gecmis == null)
        {
            return JsonYaz(new
            {
                basarili = false,
                mesaj = $"Size ait '{hayvanAdi.Trim()}' adlı bir evcil hayvan bulunamadı."
            });
        }

        if (gecmis.Count == 0)
        {
            return JsonYaz(new
            {
                basarili = true,
                hayvanAdi = hayvanAdi.Trim(),
                muayeneler = Array.Empty<object>(),
                mesaj = $"'{hayvanAdi.Trim()}' için henüz tamamlanmış bir muayene kaydı bulunmamaktadır."
            });
        }

        return JsonYaz(new
        {
            basarili = true,
            hayvanAdi = hayvanAdi.Trim(),
            muayeneSayisi = gecmis.Count,
            muayeneler = gecmis.Select(m => new
            {
                m.RandevuId,
                tarih = m.Tarih.ToString("yyyy-MM-dd HH:mm"),
                m.HizmetAdi,
                m.VeterinerAdi,
                muayeneNotu = m.MuayeneNotu ?? "Not girilmemiş."
            })
        });
    }

    private string? AktifKullaniciIdGetir()
    {
        return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static bool TarihCozumle(string tarih, out DateOnly sonuc)
    {
        return DateOnly.TryParseExact(
            tarih.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out sonuc);
    }

    private static string JsonYaz(object veri)
    {
        return JsonSerializer.Serialize(veri, JsonAyarlari);
    }
}
