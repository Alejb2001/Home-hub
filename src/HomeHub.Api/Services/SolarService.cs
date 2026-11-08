using HomeHub.Api.Data;
using HomeHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Services;

public class SolarService(AppDbContext db) : ISolarService
{
    public async Task<SolarSummary?> GetSummaryAsync(CancellationToken ct = default)
    {
        var latest = await db.SolarReadings
            .OrderByDescending(r => r.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return null;

        return new SolarSummary(
            latest.PowerW,
            latest.EnergyTodayKwh,
            latest.EnergyTotalKwh,
            latest.Status,
            latest.TimestampUtc
        );
    }

    public async Task<IReadOnlyList<SolarReading>> GetHistoryAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        return await db.SolarReadings
            .Where(r => r.TimestampUtc >= from && r.TimestampUtc <= to)
            .OrderBy(r => r.TimestampUtc)
            .ToListAsync(ct);
    }

    public async Task<SolarStats> GetStatsAsync(string period, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var from = period switch
        {
            "week" => now.AddDays(-7),
            "month" => now.AddMonths(-1),
            "year" => now.AddYears(-1),
            _ => now.Date // "day" default
        };

        var readings = await db.SolarReadings
            .Where(r => r.TimestampUtc >= from && r.TimestampUtc <= now)
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
