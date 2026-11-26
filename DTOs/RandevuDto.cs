namespace VeterinerKlinikSistemi.DTOs;

public class UygunHekimSaatleriDto
{
    public int VeterinerId { get; set; }
    public string VeterinerAdSoyad { get; set; } = string.Empty;
    public string UzmanlikAlanlari { get; set; } = string.Empty;
    public string KlinikAdi { get; set; } = string.Empty;
    public List<string> UygunSaatler { get; set; } = new();
}

public class MuayeneGecmisiDto
{
    public int RandevuId { get; set; }
    public DateTime Tarih { get; set; }
    public string HizmetAdi { get; set; } = string.Empty;
    public string VeterinerAdi { get; set; } = string.Empty;
    public string? MuayeneNotu { get; set; }
}
