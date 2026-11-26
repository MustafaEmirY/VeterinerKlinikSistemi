using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.ViewModels;

public class RandevuFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Evcil hayvan seçimi zorunludur.")]
    [Display(Name = "Evcil Hayvan")]
    public int EvcilHayvanId { get; set; }

    [Required(ErrorMessage = "Hizmet seçimi zorunludur.")]
    [Display(Name = "Hizmet")]
    public int HizmetId { get; set; }

    [Required(ErrorMessage = "Veteriner hekim seçimi zorunludur.")]
    [Display(Name = "Veteriner Hekim")]
    public int VeterinerId { get; set; }

    [Required(ErrorMessage = "Tarih zorunludur.")]
    [Display(Name = "Tarih")]
    public DateOnly Tarih { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Required(ErrorMessage = "Saat zorunludur.")]
    [Display(Name = "Saat")]
    public TimeOnly Saat { get; set; } = new TimeOnly(10, 0);
}
