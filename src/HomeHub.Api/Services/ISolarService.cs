using HomeHub.Api.Models;

namespace HomeHub.Api.Services;

public record SolarSummary(
    decimal PowerW,
    decimal EnergyTodayKwh,
    decimal EnergyTotalKwh,
    short Status,
    DateTimeOffset LastUpdated
);

public record SolarStats(
    string Period,
    decimal TotalEnergyKwh,
    decimal MaxPowerW,
    DateTimeOffset From,
    DateTimeOffset To
);

public interface ISolarService
{
    Task<SolarSummary?> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SolarReading>> GetHistoryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<SolarStats> GetStatsAsync(string period, CancellationToken cancellationToken = default);
}
