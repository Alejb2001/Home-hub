using HomeHub.Api.Data;
using HomeHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Services;

public class SolarService(AppDbContext db, EquipmentStore equipmentStore) : ISolarService
{
    private async Task<ServiceConfig?> GetGrowattServiceAsync(long userId, CancellationToken ct)
    {
        return await db.ServiceConfigs
            .Include(s => s.Home)
            .FirstOrDefaultAsync(
                s => s.Home.UserId == userId && s.Type == "growatt" && s.Enabled,
                ct);
    }

    public async Task<SolarSummary?> GetSummaryAsync(long userId, CancellationToken ct = default)
    {
        var svc = await GetGrowattServiceAsync(userId, ct);
        if (svc?.PlantId is null) return null;

        var latest = await db.SolarReadings
            .Where(r => r.PlantId == svc.PlantId)
            .OrderByDescending(r => r.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return null;

        var now = DateTimeOffset.UtcNow;
        var firstOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthReadings = await db.SolarReadings
            .Where(r => r.PlantId == svc.PlantId && r.TimestampUtc >= firstOfMonth)
            .ToListAsync(ct);
        var energyMonth = monthReadings
            .GroupBy(r => r.TimestampUtc.Date)
            .Sum(g => g.Max(r => r.EnergyTodayKwh));

        var eq = equipmentStore.Get(svc.Id);

        return new SolarSummary(
            latest.PowerW,
            latest.EnergyTodayKwh,
            energyMonth,
            latest.EnergyTotalKwh,
            latest.Status,
            latest.TimestampUtc,
            eq?.DeviceModel ?? "",
            eq?.NominalPowerW ?? 0,
            eq?.PlantName ?? "",
            Math.Round(latest.EnergyTotalKwh * 0.997m, 1),
            Math.Round(latest.EnergyTotalKwh * 0.055m, 0)
        );
    }

    public async Task<IReadOnlyList<SolarReading>> GetHistoryAsync(
        long userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var svc = await GetGrowattServiceAsync(userId, ct);
        if (svc?.PlantId is null) return [];

        return await db.SolarReadings
            .Where(r => r.PlantId == svc.PlantId && r.TimestampUtc >= from && r.TimestampUtc <= to)
            .OrderBy(r => r.TimestampUtc)
            .ToListAsync(ct);
    }

    public async Task<SolarStats> GetStatsAsync(long userId, string period, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var from = period switch
        {
            "week" => now.AddDays(-7),
            "month" => now.AddMonths(-1),
            "year" => now.AddYears(-1),
            _ => now.Date
        };

        var svc = await GetGrowattServiceAsync(userId, ct);
        if (svc?.PlantId is null)
            return new SolarStats(period, 0, 0, from, now);

        var readings = await db.SolarReadings
            .Where(r => r.PlantId == svc.PlantId && r.TimestampUtc >= from && r.TimestampUtc <= now)
            .ToListAsync(ct);

        return new SolarStats(
            period,
            readings.Count > 0 ? readings.Max(r => r.EnergyTodayKwh) : 0,
            readings.Count > 0 ? readings.Max(r => r.PowerW) : 0,
            from,
            now
        );
    }
}
