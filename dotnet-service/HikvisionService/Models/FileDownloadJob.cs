using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikvisionService.Models;

[Table("file_download_jobs")]
public class FileDownloadJob
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("camera_id")]
    public long CameraId { get; set; }

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("file_type")]
    public string FileType { get; set; } = string.Empty; // video, photo

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("download_path")]
    public string DownloadPath { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "pending"; // pending, downloading, completed, failed

    [Column("progress")]
    public int Progress { get; set; } = 0;

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("file_start_time")]
    public DateTime FileStartTime { get; set; }

    [Column("file_end_time")]
    public DateTime FileEndTime { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Camera Camera { get; set; } = null!;
}