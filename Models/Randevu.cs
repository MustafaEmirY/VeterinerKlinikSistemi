using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinikSistemi.Models;

public class Randevu
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required]
    [Display(Name = "Veteriner Hekim")]
    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    [Required]
    [Display(Name = "Hizmet")]
    public int HizmetId { get; set; }
    public Hizmet? Hizmet { get; set; }

    [Required(ErrorMessage = "Randevu tarih ve saati zorunludur.")]
    [Display(Name = "Randevu Zamanı")]
    public DateTime BaslangicZamani { get; set; }

    [Range(5, 480)]
    [Display(Name = "Süre (Dakika)")]
    public int SureDakika { get; set; }

    [Range(0, 100000)]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Ücret (TL)")]
    public decimal Ucret { get; set; }

    [Display(Name = "Durum")]
    public RandevuDurumu Durum { get; set; } = RandevuDurumu.Bekliyor;

    [StringLength(1000)]
    [Display(Name = "Muayene Notu")]
    public string? MuayeneNotu { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
}
