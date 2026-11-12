using System.Collections.Concurrent;

namespace HomeHub.Api.Services;

public record EquipmentSnapshot(
    string DeviceModel,
    decimal NominalPowerW,
    string PlantName,
    decimal EnergyMonthKwh
);

/// <summary>
/// Singleton that holds the latest equipment info from Growatt per service config.
/// Updated by GrowattSyncService on each successful sync.
/// </summary>
public class EquipmentStore
{
    private readonly ConcurrentDictionary<long, EquipmentSnapshot> _snapshots = new();

    public EquipmentSnapshot? Get(long serviceConfigId) =>
        _snapshots.TryGetValue(serviceConfigId, out var snap) ? snap : null;

    public void Update(long serviceConfigId, EquipmentSnapshot snap) =>
        _snapshots[serviceConfigId] = snap;
}
