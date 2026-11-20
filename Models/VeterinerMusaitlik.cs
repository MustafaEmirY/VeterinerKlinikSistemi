using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.Models;

public class VeterinerMusaitlik
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Veteriner Hekim")]
    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    [Required]
    [Display(Name = "Gün")]
    public DayOfWeek Gun { get; set; }

    [Required]
    [Display(Name = "Başlangıç Saati")]
    public TimeOnly BaslangicSaati { get; set; }

    [Required]
    [Display(Name = "Bitiş Saati")]
    public TimeOnly BitisSaati { get; set; }
}
