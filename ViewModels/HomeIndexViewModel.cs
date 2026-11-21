using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.ViewModels;

public class HomeIndexViewModel
{
    public Klinik? Klinik { get; set; }
    public List<Hizmet> Hizmetler { get; set; } = new();
    public List<Veteriner> Veterinerler { get; set; } = new();
}
