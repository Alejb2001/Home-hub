using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHub.Api.Models;

[Table("service_configs")]
public class ServiceConfig
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("home_id")]
    public long HomeId { get; set; }

    [Column("type")]
    [MaxLength(50)]
    public required string Type { get; set; }

    [Column("display_name")]
    [MaxLength(100)]
    public required string DisplayName { get; set; }

    [Column("credentials_json")]
    public required string CredentialsJson { get; set; }

    [Column("plant_id")]
    [MaxLength(50)]
    public string? PlantId { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Home Home { get; set; } = null!;
}
