namespace VeterinerKlinikSistemi.Models;

public static class GunYardimcisi
{
    public static readonly DayOfWeek[] HaftaninGunleri =
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    public static string TurkceAd(DayOfWeek gun) => gun switch
    {
        DayOfWeek.Monday => "Pazartesi",
        DayOfWeek.Tuesday => "Salı",
        DayOfWeek.Wednesday => "Çarşamba",
        DayOfWeek.Thursday => "Perşembe",
        DayOfWeek.Friday => "Cuma",
        DayOfWeek.Saturday => "Cumartesi",
        DayOfWeek.Sunday => "Pazar",
        _ => gun.ToString()
    };

    public static int SiraNo(DayOfWeek gun) => gun switch
    {
        DayOfWeek.Sunday => 7,
        _ => (int)gun
    };
}
