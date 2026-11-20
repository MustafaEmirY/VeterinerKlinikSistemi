using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.Models;

public enum RandevuDurumu
{
    [Display(Name = "Bekliyor")]
    Bekliyor = 0,

    [Display(Name = "Onaylandı")]
    Onaylandi = 1,

    [Display(Name = "Reddedildi")]
    Reddedildi = 2,

    [Display(Name = "Tamamlandı")]
    Tamamlandi = 3,

    [Display(Name = "İptal Edildi")]
    IptalEdildi = 4
}
