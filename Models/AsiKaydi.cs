using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.Models;

public class AsiKaydi
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Aşı Adı")]
    public string AsiAdi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yapıldığı tarih zorunludur.")]
    [Display(Name = "Yapıldığı Tarih")]
    public DateOnly YapildigiTarih { get; set; }

    [Display(Name = "Sonraki Doz Tarihi")]
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(300)]
    [Display(Name = "Notlar")]
    public string? Notlar { get; set; }
}
