using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VeterinerKlinikSistemi.Models;

public class Hizmet
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Hizmet Adı")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [Required]
    [Range(5, 480, ErrorMessage = "Süre 5 ile 480 dakika arasında olmalıdır.")]
    [Display(Name = "Süre (Dakika)")]
    public int SureDakika { get; set; }

    [Required]
    [Range(0, 100000, ErrorMessage = "Ücret 0 ile 100.000 TL arasında olmalıdır.")]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Ücret (TL)")]
    public decimal Ucret { get; set; }

    [Required]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    public ICollection<VeterinerHizmet> VeterinerHizmetler { get; set; } = new List<VeterinerHizmet>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}
