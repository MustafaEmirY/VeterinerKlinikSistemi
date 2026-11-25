namespace VeterinerKlinikSistemi.DTOs;

public class AsiKaydiDto
{
    public int Id { get; set; }
    public string AsiAdi { get; set; } = string.Empty;
    public DateOnly YapildigiTarih { get; set; }
    public DateOnly? SonrakiDozTarihi { get; set; }
    public string? Notlar { get; set; }
    public string Durum { get; set; } = string.Empty;
}

public class AsiTakvimiDto
{
    public int EvcilHayvanId { get; set; }
    public string HayvanAdi { get; set; } = string.Empty;
    public string Tur { get; set; } = string.Empty;
    public string Irk { get; set; } = string.Empty;
    public List<AsiKaydiDto> GecmisAsilar { get; set; } = new();
    public List<AsiKaydiDto> YaklasanAsilar { get; set; } = new();
}
