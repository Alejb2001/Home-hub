using HomeHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SolarReading> SolarReadings => Set<SolarReading>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

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
    }
}
