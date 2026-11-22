using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.ViewModels;

namespace VeterinerKlinikSistemi.Services;

public class AdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> OzetGetirAsync()
    {
        return new AdminDashboardViewModel
        {
            KlinikSayisi = await _context.Klinikler.CountAsync(),
            HizmetSayisi = await _context.Hizmetler.CountAsync(),
            VeterinerSayisi = await _context.Veterinerler.CountAsync(),
            EvcilHayvanSayisi = await _context.EvcilHayvanlar.CountAsync(),
            AsiKaydiSayisi = await _context.AsiKayitlari.CountAsync(),
            ToplamRandevuSayisi = await _context.Randevular.CountAsync(),
            BekleyenRandevuSayisi = await _context.Randevular.CountAsync(r => r.Durum == RandevuDurumu.Bekliyor),
            KullaniciSayisi = await _context.Users.CountAsync()
        };
    }
}
