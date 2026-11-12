using HomeHub.Api.Models;

namespace HomeHub.Api.Services;

public record SolarSummary(
    decimal PowerW,
    decimal EnergyTodayKwh,
    decimal EnergyMonthKwh,
    decimal EnergyTotalKwh,
    short Status,
    DateTimeOffset LastUpdated,
    string DeviceModel,
    decimal NominalPowerW,
    string PlantName,
    decimal Co2Kg,
    decimal TreesCount
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
    Task<SolarSummary?> GetSummaryAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SolarReading>> GetHistoryAsync(long userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<SolarStats> GetStatsAsync(long userId, string period, CancellationToken cancellationToken = default);
}
