using HomeHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SolarReading> SolarReadings => Set<SolarReading>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<ServiceConfig> ServiceConfigs => Set<ServiceConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SolarReading>(entity =>
        {
            entity.HasIndex(r => new { r.PlantId, r.TimestampUtc })
                .HasDatabaseName("ix_solar_readings_plant_timestamp")
                .IsDescending(false, true);

            entity.HasIndex(r => r.TimestampUtc)
                .HasDatabaseName("ix_solar_readings_timestamp")
                .IsDescending(true);

            entity.Property(r => r.PowerW).HasPrecision(10, 2);
            entity.Property(r => r.EnergyTodayKwh).HasPrecision(10, 3);
            entity.Property(r => r.EnergyTotalKwh).HasPrecision(12, 3);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ix_users_email");
        });

        modelBuilder.Entity<Home>(entity =>
        {
            entity.HasOne(h => h.User)
                .WithOne(u => u.Home)
                .HasForeignKey<Home>(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServiceConfig>(entity =>
        {
            entity.HasOne(s => s.Home)
                .WithMany(h => h.Services)
                .HasForeignKey(s => s.HomeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
