namespace HikvisionService.Models.ViewModels;

public class DashboardViewModel
{
    // System summary
    public int TotalCameras { get; set; }
    public int OnlineCameras { get; set; }
    public int OfflineCameras { get; set; }
    public int ActiveDownloadJobs { get; set; }
    public int FailedDownloadJobs { get; set; }
    
    // Offline cameras list
    public List<CameraStatusViewModel> OfflineCamerasList { get; set; } = new List<CameraStatusViewModel>();
    
    // Storage drives
    public List<StorageDriveViewModel> StorageDrives { get; set; } = new List<StorageDriveViewModel>();
}

public class CameraStatusViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }
    public DateTime? LastDownloadedAt { get; set; }
    public string Status { get; set; } = string.Empty; // "Idle", "Downloading", etc.
}

public class StorageDriveViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public long TotalSpace { get; set; }
    public long UsedSpace { get; set; }
    public long FreeSpace { get; set; }
    public string Status { get; set; } = string.Empty;
    public double UsagePercentage => TotalSpace > 0 ? (double)UsedSpace / TotalSpace * 100 : 0;
    public DateTime LastCheckedAt { get; set; }
}