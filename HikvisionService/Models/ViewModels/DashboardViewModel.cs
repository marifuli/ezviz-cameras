namespace HikvisionService.Models.ViewModels;

public class DashboardViewModel
{
    // System summary
    public int TotalCameras { get; set; }
    public int OnlineCameras { get; set; }
    public int OfflineCameras { get; set; }
    public int ActiveDownloadJobs { get; set; }
    public int FailedDownloadJobs { get; set; }
    public int CompletedDownloadJobs { get; set; }
    
    // Offline cameras list
    public List<CameraStatusViewModel> OfflineCamerasList { get; set; } = new List<CameraStatusViewModel>();
    
    // Storage drives
    public List<StorageDriveViewModel> StorageDrives { get; set; } = new List<StorageDriveViewModel>();
    
    // Chart data
    public List<ChartDataPoint> DownloadsPerDay { get; set; } = new List<ChartDataPoint>();
    public List<ChartDataPoint> StorageUsageTrend { get; set; } = new List<ChartDataPoint>();
    public List<ChartDataPoint> CameraStatusHistory { get; set; } = new List<ChartDataPoint>();
    
    // Camera statistics
    public Dictionary<string, int> DownloadsPerCamera { get; set; } = new Dictionary<string, int>();
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

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Category { get; set; }
    public string? Color { get; set; }
}

public class FootageViewModel
{
    public List<Camera> Cameras { get; set; } = new List<Camera>();
    public List<FootageFileViewModel> Files { get; set; } = new List<FootageFileViewModel>();
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public long? SelectedCameraId { get; set; }
    public string FileType { get; set; } = "both"; // "video", "photo", "both"
}

public class FootageFileViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // "video", "photo"
    public long FileSize { get; set; }
    public DateTime FileStartTime { get; set; }
    public DateTime FileEndTime { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public long CameraId { get; set; }
    public string ThumbnailPath { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
}