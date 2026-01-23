using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikvisionService.Models;

[Table("cameras")]
public class Camera
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("store_id")]
    public long? StoreId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("ip_address")]
    public string IpAddress { get; set; } = string.Empty;

    [Column("port")]
    public int Port { get; set; } = 554;

    [Column("username")]
    public string? Username { get; set; }

    [Column("password")]
    public string? Password { get; set; }

    [Column("server_port")]
    public int? ServerPort { get; set; }

    [Column("is_online")]
    public bool IsOnline { get; set; } = false;

    [Column("last_online_at")]
    public DateTime? LastOnlineAt { get; set; }

    [Column("last_downloaded_at")]
    public DateTime? LastDownloadedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Store? Store { get; set; }
    public virtual ICollection<FileDownloadJob> FileDownloadJobs { get; set; } = new List<FileDownloadJob>();
}