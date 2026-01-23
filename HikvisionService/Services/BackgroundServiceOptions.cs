namespace HikvisionService.Services;

public class BackgroundServiceOptions
{
    public CameraHealthCheckOptions CameraHealthCheck { get; set; } = new();
    public StorageMonitoringOptions StorageMonitoring { get; set; } = new();
    public DownloadJobOptions DownloadJob { get; set; } = new();
}

public class CameraHealthCheckOptions
{
    public int IntervalMinutes { get; set; } = 5;
}

public class StorageMonitoringOptions
{
    public int IntervalMinutes { get; set; } = 15;
}

public class DownloadJobOptions
{
    public int IntervalSeconds { get; set; } = 60;
    public int MaxConcurrentDownloads { get; set; } = 2;
}