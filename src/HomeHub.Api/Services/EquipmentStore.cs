namespace HomeHub.Api.Services;

public record EquipmentSnapshot(
    string DeviceModel,
    decimal NominalPowerW,
    string PlantName,
    decimal EnergyMonthKwh
);

/// <summary>
/// Singleton that holds the latest equipment info from Growatt (not persisted to DB).
/// Updated by GrowattSyncService on each successful sync.
/// </summary>
public class EquipmentStore
{
    private volatile EquipmentSnapshot? _current;

    public EquipmentSnapshot? Current => _current;

    public void Update(EquipmentSnapshot snap) => _current = snap;
}
