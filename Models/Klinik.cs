using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.Models;

public class Klinik
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Klinik adı zorunludur.")]
    [StringLength(120)]
    [Display(Name = "Klinik Adı")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres zorunludur.")]
    [StringLength(250)]
    [Display(Name = "Adres")]
    public string Adres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [StringLength(20)]
    [Display(Name = "Telefon")]
    public string Telefon { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Açılış Saati")]
    public TimeOnly AcilisSaati { get; set; } = new TimeOnly(9, 0);

    [Required]
    [Display(Name = "Kapanış Saati")]
    public TimeOnly KapanisSaati { get; set; } = new TimeOnly(19, 0);

    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
}
