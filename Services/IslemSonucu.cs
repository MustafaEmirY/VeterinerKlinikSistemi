namespace VeterinerKlinikSistemi.Services;

public class IslemSonucu
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = string.Empty;

    public static IslemSonucu Basari(string mesaj = "") => new() { Basarili = true, Mesaj = mesaj };
    public static IslemSonucu Hata(string mesaj) => new() { Basarili = false, Mesaj = mesaj };
}
