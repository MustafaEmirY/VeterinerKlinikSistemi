using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.Models;

public class Veteriner
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

    [Required]
    [Display(Name = "Klinik")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    [StringLength(450)]
    public string? KullaniciId { get; set; }

    public ICollection<VeterinerHizmet> VeterinerHizmetler { get; set; } = new List<VeterinerHizmet>();
    public ICollection<VeterinerMusaitlik> Musaitlikler { get; set; } = new List<VeterinerMusaitlik>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
}
