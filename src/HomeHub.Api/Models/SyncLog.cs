using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HomeHub.Api.Models;

[Table("sync_log")]
public class SyncLog
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("synced_at")]
    public DateTimeOffset SyncedAt { get; set; }

    [Column("success")]
    public bool Success { get; set; }

    [Column("records_saved")]
    public int RecordsSaved { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}
