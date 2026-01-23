using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikvisionService.Models;

[Table("storage_drives")]
public class StorageDrive
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("root_path")]
    public string RootPath { get; set; } = string.Empty;

    [Column("total_space")]
    public long TotalSpace { get; set; } = 0;

    [Column("used_space")]
    public long UsedSpace { get; set; } = 0;

    [Column("free_space")]
    public long FreeSpace { get; set; } = 0;

    [Column("last_checked_at")]
    public DateTime LastCheckedAt { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Normal"; // Normal, Warning, Critical, Full

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}