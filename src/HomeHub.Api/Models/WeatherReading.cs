using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHub.Api.Models;

[Table("weather_readings")]
public class WeatherReading
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("service_config_id")]
    public long ServiceConfigId { get; set; }

    [Column("timestamp_utc")]
    public DateTimeOffset TimestampUtc { get; set; }

    [Column("temp_c")]
    public decimal TempC { get; set; }

    [Column("clouds_pct")]
    public int CloudsPct { get; set; }

    /// <summary>OWM weather condition code. 800 = clear, 801-804 = clouds, 5xx = rain, etc.</summary>
    [Column("weather_id")]
    public int WeatherId { get; set; }

    [Column("description")]
    [MaxLength(100)]
    public string Description { get; set; } = "";

    [Column("humidity_pct")]
    public int HumidityPct { get; set; }

    [Column("wind_speed_ms")]
    public decimal WindSpeedMs { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
