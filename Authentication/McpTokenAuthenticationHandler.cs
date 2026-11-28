using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Authentication;

public class McpTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "McpToken";

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public McpTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
        : base(options, logger, encoder)
    {
        _context = context;
        _userManager = userManager;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TokenCikar();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("MCP API anahtarı bulunamadı.");
        }

        var kullanici = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.McpApiAnahtari == token);

        if (kullanici == null)
        {
            return AuthenticateResult.Fail("Geçersiz MCP API anahtarı.");
        }

        var roller = await _userManager.GetRolesAsync(kullanici);

        var talepler = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, kullanici.Id),
            new(ClaimTypes.Name, kullanici.AdSoyad),
            new(ClaimTypes.Email, kullanici.Email ?? string.Empty)
        };

        foreach (var rol in roller)
        {
            talepler.Add(new Claim(ClaimTypes.Role, rol));
        }

        var kimlik = new ClaimsIdentity(talepler, SchemeName);
        var principal = new ClaimsPrincipal(kimlik);
        var bilet = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(bilet);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer realm=\"KaplanVeterinerMCP\"";
        Response.ContentType = "application/json; charset=utf-8";

        var govde = JsonSerializer.Serialize(new
        {
            hata = "MCP sunucusuna erişim için geçerli bir kişisel MCP API anahtarı gereklidir.",
            kullanim = "Authorization: Bearer <McpApiAnahtari> veya X-Api-Key: <McpApiAnahtari>"
        }, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await Response.WriteAsync(govde);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json; charset=utf-8";

        var govde = JsonSerializer.Serialize(new
        {
            hata = "Bu MCP işlemi için yetkiniz bulunmamaktadır."
        }, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await Response.WriteAsync(govde);
    }

    private string? TokenCikar()
    {
        if (Request.Headers.TryGetValue("Authorization", out var authBaslik))
        {
            var deger = authBaslik.ToString().Trim();
            if (deger.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return deger["Bearer ".Length..].Trim();
            }
        }

        if (Request.Headers.TryGetValue("X-Api-Key", out var apiKeyBaslik))
        {
            var deger = apiKeyBaslik.ToString().Trim();
            if (!string.IsNullOrEmpty(deger))
            {
                return deger;
            }
        }

        return null;
    }
}
