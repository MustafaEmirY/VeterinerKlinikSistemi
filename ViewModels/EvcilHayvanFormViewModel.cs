using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VeterinerKlinikSistemi.ViewModels;

public class EvcilHayvanFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Evcil hayvan adı zorunludur.")]
    [StringLength(80)]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(50)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = string.Empty;

    [Required(ErrorMessage = "Irk zorunludur.")]
    [StringLength(80)]
    [Display(Name = "Irk")]
    public string Irk { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğum tarihi zorunludur.")]
    [Display(Name = "Doğum Tarihi")]
    public DateOnly DogumTarihi { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));

    [Required(ErrorMessage = "Kilo zorunludur.")]
    [Range(0.1, 200, ErrorMessage = "Kilo 0.1 ile 200 kg arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public decimal Kilo { get; set; }

    [Display(Name = "Fotoğraf (İsteğe Bağlı)")]
    public IFormFile? Fotograf { get; set; }

    public string? MevcutFotografYolu { get; set; }

    [Display(Name = "Mevcut Fotoğrafı Kaldır")]
    public bool FotografiKaldir { get; set; }

    [Display(Name = "Hayvan Sahibi")]
    public string? SahipId { get; set; }
}
