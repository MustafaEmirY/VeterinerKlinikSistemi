using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VeterinerKlinikSistemi.Models;

public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    [Display(Name = "MCP API Anahtarı")]
    public string McpApiAnahtari { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<EvcilHayvan> EvcilHayvanlar { get; set; } = new List<EvcilHayvan>();
}
