using HomeHub.Api.Models;

namespace HomeHub.Api.Services;

public interface IGrowattClient
{
    Task<GrowattPlantInfo> GetPlantInfoAsync(CancellationToken cancellationToken = default);
    Task<SolarReading> GetCurrentReadingAsync(string plantId, string inverterSn, CancellationToken cancellationToken = default);
}

public record GrowattPlantInfo(string PlantId, string PlantName, string InverterSn);
