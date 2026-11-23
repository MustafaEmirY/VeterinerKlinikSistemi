using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.ViewModels;

public class VeterinerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Uzmanlık alanları zorunludur.")]
    [StringLength(200)]
    [Display(Name = "Uzmanlık Alanları")]
    public string UzmanlikAlanlari { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(150)]
    [Display(Name = "E-posta")]
    public string? Eposta { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Klinik seçimi zorunludur.")]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }

    public List<int> SeciliHizmetIdleri { get; set; } = new();

    [Display(Name = "Veteriner Giriş Hesabı Oluştur")]
    public bool HesapOlustur { get; set; } = true;

    [StringLength(100, MinimumLength = 3, ErrorMessage = "Şifre en az 3 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Giriş Şifresi")]
    public string? GirisSifresi { get; set; }

    public string? MevcutKullaniciEposta { get; set; }
}
