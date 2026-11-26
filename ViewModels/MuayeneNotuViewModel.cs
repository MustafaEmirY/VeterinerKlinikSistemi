using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.ViewModels;

public class MuayeneNotuViewModel
{
    public int RandevuId { get; set; }

    [Required(ErrorMessage = "Muayene notu zorunludur.")]
    [StringLength(1000, ErrorMessage = "Muayene notu en fazla 1000 karakter olabilir.")]
    [Display(Name = "Muayene Notu")]
    public string MuayeneNotu { get; set; } = string.Empty;

    public string? HayvanAdi { get; set; }
    public string? HizmetAdi { get; set; }
    public string? VeterinerAdi { get; set; }
    public DateTime BaslangicZamani { get; set; }
}
