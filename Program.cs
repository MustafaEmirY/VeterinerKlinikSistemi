using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Data;
using VeterinerKlinikSistemi.Models;
using VeterinerKlinikSistemi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Admin şifresi ödev gereği "sau" olduğu için parola kuralları buna uyumlu yapılandırıldı
    options.Password.RequiredLength = 3;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // REST API isteklerinde HTML login sayfasına 302 yönlendirmek yerine doğru HTTP 401/403 durum kodlarını döndür
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<KlinikService>();
builder.Services.AddScoped<HizmetService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<VeterinerService>();
builder.Services.AddScoped<EvcilHayvanService>();
builder.Services.AddScoped<AsiService>();
builder.Services.AddScoped<RandevuService>();
builder.Services.AddScoped<RaporService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var trCulture = new CultureInfo("tr-TR");
trCulture.NumberFormat.NumberDecimalSeparator = ".";
trCulture.NumberFormat.CurrencyDecimalSeparator = ".";
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(trCulture),
    SupportedCultures = new List<CultureInfo> { trCulture },
    SupportedUICultures = new List<CultureInfo> { trCulture }
};
app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

if (!EF.IsDesignTime)
{
    await DbInitializer.SeedAsync(app.Services);
}

app.Run();
