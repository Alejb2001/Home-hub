using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeHub.Api.Models;

[Table("solar_readings")]
public class SolarReading
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("plant_id")]
    [MaxLength(50)]
    public required string PlantId { get; set; }

    [Column("inverter_sn")]
    [MaxLength(50)]
    public required string InverterSn { get; set; }

    [Column("timestamp_utc")]
    public DateTimeOffset TimestampUtc { get; set; }

    [Column("power_w")]
    public decimal PowerW { get; set; }

    [Column("energy_today_kwh")]
    public decimal EnergyTodayKwh { get; set; }

    [Column("energy_total_kwh")]
    public decimal EnergyTotalKwh { get; set; }

    /// <summary>1 = online/normal, 0 = offline/fault</summary>
    [Column("status")]
    public short Status { get; set; }

    // Not persisted — populated by GrowattClient in memory, used by EquipmentStore
    [NotMapped] public decimal? EnergyMonthKwh { get; set; }
    [NotMapped] public string? DeviceModel { get; set; }
    [NotMapped] public decimal? NominalPowerW { get; set; }
    [NotMapped] public string? PlantName { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
