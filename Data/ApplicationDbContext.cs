using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinikSistemi.Models;

namespace VeterinerKlinikSistemi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Klinik> Klinikler { get; set; } = default!;
    public DbSet<Hizmet> Hizmetler { get; set; } = default!;
    public DbSet<Veteriner> Veterinerler { get; set; } = default!;
    public DbSet<VeterinerHizmet> VeterinerHizmetler { get; set; } = default!;
    public DbSet<VeterinerMusaitlik> VeterinerMusaitlikler { get; set; } = default!;
    public DbSet<EvcilHayvan> EvcilHayvanlar { get; set; } = default!;
    public DbSet<AsiKaydi> AsiKayitlari { get; set; } = default!;
    public DbSet<Randevu> Randevular { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VeterinerHizmet>()
            .HasKey(vh => new { vh.VeterinerId, vh.HizmetId });

        modelBuilder.Entity<VeterinerHizmet>()
            .HasOne(vh => vh.Veteriner)
            .WithMany(v => v.VeterinerHizmetler)
            .HasForeignKey(vh => vh.VeterinerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VeterinerHizmet>()
            .HasOne(vh => vh.Hizmet)
            .WithMany(h => h.VeterinerHizmetler)
            .HasForeignKey(vh => vh.HizmetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Hizmet>()
            .HasOne(h => h.Klinik)
            .WithMany(k => k.Hizmetler)
            .HasForeignKey(h => h.KlinikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Veteriner>()
            .HasOne(v => v.Klinik)
            .WithMany(k => k.Veterinerler)
            .HasForeignKey(v => v.KlinikId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VeterinerMusaitlik>()
            .HasOne(vm => vm.Veteriner)
            .WithMany(v => v.Musaitlikler)
            .HasForeignKey(vm => vm.VeterinerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AsiKaydi>()
            .HasOne(a => a.EvcilHayvan)
            .WithMany(e => e.AsiKayitlari)
            .HasForeignKey(a => a.EvcilHayvanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Randevu>()
            .HasOne(r => r.EvcilHayvan)
            .WithMany(e => e.Randevular)
            .HasForeignKey(r => r.EvcilHayvanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Randevu>()
            .HasOne(r => r.Veteriner)
            .WithMany(v => v.Randevular)
            .HasForeignKey(r => r.VeterinerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Randevu>()
            .HasOne(r => r.Hizmet)
            .WithMany(h => h.Randevular)
            .HasForeignKey(r => r.HizmetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.McpApiAnahtari)
            .IsUnique();

        modelBuilder.Entity<EvcilHayvan>()
            .HasOne(e => e.Sahip)
            .WithMany(u => u.EvcilHayvanlar)
            .HasForeignKey(e => e.SahipId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Veteriner>()
            .HasOne(v => v.Kullanici)
            .WithMany()
            .HasForeignKey(v => v.KullaniciId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
