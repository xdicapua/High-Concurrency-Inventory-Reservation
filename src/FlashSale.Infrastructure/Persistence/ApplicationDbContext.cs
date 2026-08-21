using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(64);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(256);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        // Configuración de Reservation
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.CreatedAtUtc).IsRequired();
            entity.Property(r => r.ExpiresAtUtc).IsRequired();

            // Índice para consultas rápidas por usuario y expiración
            entity.HasIndex(r => new { r.UserId, r.Status });
        });
    }
}