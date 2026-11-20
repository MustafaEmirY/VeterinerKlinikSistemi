using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinikSistemi.Models;

public class EvcilHayvan
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
    public DateOnly DogumTarihi { get; set; }

    [Required]
    [Range(0.1, 200, ErrorMessage = "Kilo 0.1 ile 200 kg arasında olmalıdır.")]
    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Kilo (kg)")]
    public decimal Kilo { get; set; }

    [StringLength(300)]
    [Display(Name = "Fotoğraf")]
    public string? FotografYolu { get; set; }

    [Required]
    [StringLength(450)]
    public string SahipId { get; set; } = string.Empty;

    public ICollection<AsiKaydi> AsiKayitlari { get; set; } = new List<AsiKaydi>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}
