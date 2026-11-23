using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.ViewModels;

public class MusaitlikEkleViewModel
{
    public int VeterinerId { get; set; }

    [Required(ErrorMessage = "Gün seçimi zorunludur.")]
    [Display(Name = "Gün")]
    public DayOfWeek Gun { get; set; } = DayOfWeek.Monday;

    [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
    [Display(Name = "Başlangıç Saati")]
    public TimeOnly BaslangicSaati { get; set; } = new TimeOnly(9, 0);

    [Required(ErrorMessage = "Bitiş saati zorunludur.")]
    [Display(Name = "Bitiş Saati")]
    public TimeOnly BitisSaati { get; set; } = new TimeOnly(17, 0);
}
