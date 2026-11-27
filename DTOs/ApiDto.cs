using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinikSistemi.DTOs;

public class VeterinerApiDto
{
    public int Id { get; set; }
    public string AdSoyad { get; set; } = string.Empty;
    public string UzmanlikAlanlari { get; set; } = string.Empty;
    public string KlinikAdi { get; set; } = string.Empty;
    public List<string> Hizmetler { get; set; } = new();
    public List<string> MusaitGunler { get; set; } = new();
}

public class HizmetCiroRaporuDto
{
    public int HizmetId { get; set; }
    public string HizmetAdi { get; set; } = string.Empty;
    public string KlinikAdi { get; set; } = string.Empty;
    public int ToplamRandevuSayisi { get; set; }
    public int OnayliVeyaTamamlananSayisi { get; set; }
    public int BekleyenSayisi { get; set; }
    public decimal ToplamCiro { get; set; }
}

public class CiroRaporuOzetDto
{
    public DateOnly? BaslangicTarihi { get; set; }
    public DateOnly? BitisTarihi { get; set; }
    public int GenelRandevuSayisi { get; set; }
    public decimal GenelCiro { get; set; }
    public List<HizmetCiroRaporuDto> HizmetBazliRapor { get; set; } = new();
}

public class RandevuOlusturApiDto
{
    [Required(ErrorMessage = "EvcilHayvanId zorunludur.")]
    public int EvcilHayvanId { get; set; }

    [Required(ErrorMessage = "HizmetId zorunludur.")]
    public int HizmetId { get; set; }

    [Required(ErrorMessage = "VeterinerId zorunludur.")]
    public int VeterinerId { get; set; }

    [Required(ErrorMessage = "BaslangicZamani zorunludur.")]
    public DateTime BaslangicZamani { get; set; }
}

public class RandevuApiDto
{
    public int Id { get; set; }
    public int EvcilHayvanId { get; set; }
    public string EvcilHayvanAdi { get; set; } = string.Empty;
    public int HizmetId { get; set; }
    public string HizmetAdi { get; set; } = string.Empty;
    public int VeterinerId { get; set; }
    public string VeterinerAdi { get; set; } = string.Empty;
    public DateTime BaslangicZamani { get; set; }
    public int SureDakika { get; set; }
    public decimal Ucret { get; set; }
    public string Durum { get; set; } = string.Empty;
    public string? MuayeneNotu { get; set; }
}
